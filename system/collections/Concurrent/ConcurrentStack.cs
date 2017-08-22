#pragma warning disable 0420

// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcurrentStack.cs
//
// <OWNER>[....]</OWNER>
//
// A lock-free, concurrent stack primitive, and its associated debugger view type.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
    // A stack that uses CAS operations internally to maintain thread-safety in a lock-free
    // manner. Attempting to push or pop concurrently from the stack will not trigger waiting,
    // although some optimistic concurrency and retry is used, possibly leading to lack of
    // fairness and/or livelock. The stack uses spinning and backoff to add some randomization,
    // in hopes of statistically decreasing the possibility of livelock.
    // 
    // Note that we currently allocate a new node on every push. This avoids having to worry
    // about potential ABA issues, since the CLR GC ensures that a memory address cannot be
    // reused before all references to it have died.

    /// <summary>
    /// 表示线程安全的后进先出 (LIFO) 集合。
    /// </summary>
    /// <typeparam name="T">堆栈中包含的元素的类型。</typeparam>
    /// <remarks>
    /// <see cref="ConcurrentStack{T}"/> 中的公开和保护的成员都是线程安全并且可能会使用在多线程并发中
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<>))]
    [HostProtection(Synchronization = true, ExternalThreading = true)]
#if !FEATURE_CORECLR
    [Serializable]
#endif //!FEATURE_CORECLR
    public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        /// <summary>
        /// 一个简单的节点格式被用于存储并发元素的栈和队列
        /// </summary>
        private class Node
        {
            internal readonly T m_value; // Value of the node.
            internal Node m_next; // Next pointer.

            /// <summary>
            /// 通过指定的值和无下一个节点构造一个新节点
            /// </summary>
            /// <param name="value">节点值</param>
            internal Node(T value)
            {
                m_value = value;
                m_next = null;
            }
        }

#if !FEATURE_CORECLR
        [NonSerialized]
#endif //!FEATURE_CORECLR
        private volatile Node m_head; // The stack is a singly linked list, and only remembers the head.

#if !FEATURE_CORECLR
        private T[] m_serializationArray; // 被用于消费者序列化
#endif //!FEATURE_CORECLR

        private const int BACKOFF_MAX_YIELDS = 8; // Arbitrary number to cap backoff.

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentStack{T}"/>
        /// class.
        /// </summary>
        public ConcurrentStack()
        {
        }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentStack<T> 类的新实例，该类包含从指定集合中复制的元素
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new <see
        /// cref="ConcurrentStack{T}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/>参数为空</exception>
        public ConcurrentStack(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            InitializeFromCollection(collection);
        }

        /// <summary>
        /// Initializes the contents of the stack from an existing collection.
        /// </summary>
        /// <param name="collection">A collection from which to copy elements.</param>
        private void InitializeFromCollection(IEnumerable<T> collection)
        {
            // 我们将集合内容复制到栈中
            Node lastNode = null;
            foreach (T element in collection)
            {
                Node newNode = new Node(element);
                newNode.m_next = lastNode;
                lastNode = newNode;
            }

            m_head = lastNode;
        }

#if !FEATURE_CORECLR
        /// <summary>
        /// 获取数字数据用于序列化
        /// </summary>
        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            // save the data into the serialization array to be saved
            m_serializationArray = ToArray();
        }

        /// <summary>
        /// 从一个预先的序列化构造一个栈
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Contract.Assert(m_serializationArray != null);
            Node prevNode = null;
            Node head = null;
            for (int i = 0; i < m_serializationArray.Length; i++)
            {
                Node currNode = new Node(m_serializationArray[i]);

                if (prevNode == null)
                {
                    head = currNode;
                }
                else
                {
                    prevNode.m_next = currNode;
                }

                prevNode = currNode;
            }

            m_head = head;
            m_serializationArray = null;//清空序列化数组
        }
#endif //!FEATURE_CORECLR


        /// <summary>
        /// 获取一个值指示<see cref="ConcurrentStack{T}"/>是否为空的值
        /// </summary>
        /// <value>true if the <see cref="ConcurrentStack{T}"/> is empty; otherwise, false.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of this property is recommended
        /// rather than retrieving the number of items from the <see cref="Count"/> property and comparing it
        /// to 0.  However, as this collection is intended to be accessed concurrently, it may be the case
        /// that another thread will modify the collection after <see cref="IsEmpty"/> returns, thus invalidating
        /// the result.
        /// </remarks>
        public bool IsEmpty
        {
            // Checks whether the stack is empty. Clearly the answer may be out of date even prior to
            // the function returning (i.e. if another thread concurrently adds to the stack). It does
            // guarantee, however, that, if another thread does not mutate the stack, a subsequent call
            // to TryPop will return true -- i.e. it will also read the stack as non-empty.
            get { return m_head == null; }
        }

        /// <summary>
        ///  获取 System.Collections.Concurrent.ConcurrentStack<T> 中包含的元素数。
        /// </summary>
        /// <value>The number of elements contained in the <see cref="ConcurrentStack{T}"/>.</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
        /// property and comparing it to 0.
        /// </remarks>
        public int Count
        {
            // Counts the number of entries in the stack. This is an O(n) operation. The answer may be out
            // of date before returning, but guarantees to return a count that was once valid. Conceptually,
            // the implementation snaps a copy of the list and then counts the entries, though physically
            // this is not what actually happens.
            get
            {
                int count = 0;

                // Just whip through the list and tally up the number of nodes. We rely on the fact that
                // node next pointers are immutable after being enqueued for the first time, even as
                // they are being dequeued. If we ever changed this (e.g. to pool nodes somehow),
                // we'd need to revisit this implementation.

                for (Node curr = m_head; curr != null; curr = curr.m_next)
                {
                    count++; //we don't handle overflow, to be consistent with existing generic collection types in CLR
                }

                return count;
            }
        }


        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentStack{T}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            // Gets a value indicating whether access to this collection is synchronized. Always returns
            // false. The reason is subtle. While access is in face thread safe, it's not the case that
            // locking on the SyncRoot would have prevented concurrent pushes and pops, as this property
            // would typically indicate; that's because we internally use CAS operations vs. true locks.
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="T:System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
            }
        }

        /// <summary>
        /// 移除<see cref="ConcurrentStack{T}"/>中所有对象
        /// </summary>
        public void Clear()
        {
            // Clear the list by setting the head to null. We don't need to use an atomic
            // operation for this: anybody who is mutating the head by pushing or popping
            // will need to use an atomic operation to guarantee they serialize and don't
            // overwrite our setting of the head to null.
            // 通过将head设置为null清空list，我们不需要使用一个原子操作：
            // 任何通过push或pop变化head的人将需要使用一个原子操作去保证序列化并且不需要重写我们将head设置为null
            m_head = null;
        }

        /// <summary>
        /// 将<see cref="T:System.Collections.ICollection"/>内容元素复制给一个<see
        /// cref="T:System.Array"/>,在数组的开始索引处
        /// （显示调用ICollection.CopyTo）
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="T:System.Array"/> must
        /// have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/> is multidimensional. -or-
        /// <paramref name="array"/> does not have zero-based indexing. -or-
        /// <paramref name="index"/> is equal to or greater than the length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is
        /// greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>. -or- The type of the source <see
        /// cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the
        /// destination <paramref name="array"/>.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            // Validate arguments.
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // 我们必须谨慎防止恶化array，所以我们将首先积攒一个内部元素list以至于我们向array复制
            // 这需要额外的分配，但是是必须的但是我们不知道数组是否是足够大的对于容纳栈的内容
            ((ICollection)ToList()).CopyTo(array, index);//调用ICollection中的CopyTo
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentStack{T}"/> elements to an existing one-dimensional <see
        /// cref="T:System.Array"/>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the
        /// <see cref="ConcurrentStack{T}"/>. The <see cref="T:System.Array"/> must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ConcurrentStack{T}"/> is greater than the
        /// available space from <paramref name="index"/> to the end of the destination <paramref
        /// name="array"/>.
        /// </exception>
        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            // We must be careful not to corrupt the array, so we will first accumulate an
            // internal list of elements that we will then copy to the array. This requires
            // some extra allocation, but is necessary since we don't know up front whether
            // the array is sufficiently large to hold the stack's contents.
            ToList().CopyTo(array, index);//调用List中的CopyTo
        }


        /// <summary>
        /// 将对象插入<see cref="ConcurrentStack{T}"/>顶部
        /// </summary>
        /// <param name="item">
        /// 要推入到 System.Collections.Concurrent.ConcurrentStack<T> 中的对象。 该值对于引用类型可以是空引用
        /// </param>
        public void Push(T item)
        {
            // Pushes a node onto the front of the stack thread-safely. Internally（内部地）, this simply
            // swaps（交换） the current head pointer using a (thread safe) CAS operation to accomplish（完成）
            // lock freedom. If the CAS fails, we add some back off to statistically（统计） decrease（减少）
            // contention（争论） at the head, and then go back around and retry.

            Node newNode = new Node(item);
            newNode.m_next = m_head;
            if (Interlocked.CompareExchange(ref m_head, newNode, newNode.m_next) == newNode.m_next)
            {
                return;
            }

            // 如果我们失败了，沿着路径和循环插入，直到我们成功。
            PushCore(newNode, newNode);
        }

        /// <summary>
        /// 自动将多个对象插入 System.Collections.Concurrent.ConcurrentStack<T> 的顶部。
        /// </summary>
        /// <param name="items">要推入到 System.Collections.Concurrent.ConcurrentStack<T> 中的对象。</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// When adding multiple items to the stack, using PushRange is a more efficient
        /// mechanism than using <see cref="Push"/> one item at a time.  Additionally, PushRange
        /// guarantees that all of the elements will be added atomically, meaning that no other threads will
        /// be able to inject elements between the elements being pushed.  Items at lower indices in
        /// the <paramref name="items"/> array will be pushed before items at higher indices.
        /// </remarks>
        public void PushRange(T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            PushRange(items, 0, items.Length);
        }

        /// <summary>
        /// 自动将多个对象插入 System.Collections.Concurrent.ConcurrentStack<T> 的顶部。
        /// </summary>
        /// <param name="items">要推入到 System.Collections.Concurrent.ConcurrentStack<T> 中的对象。</param>
        /// <param name="startIndex">items 中从零开始的偏移量，从此处开始将元素插入到 
        /// System.Collections.Concurrent.ConcurrentStack<T>的顶部。</param>
        /// <param name="count">要插入到 System.Collections.Concurrent.ConcurrentStack<T> 的顶部的元素数。</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 为负。 或 startIndex 大于或等于 items 的长度。</exception>
        /// <exception cref="ArgumentException"><paramref name="startIndex"/> + <paramref name="count"/> 大于<paramref name="items"/>的长度</exception>
        /// <remarks>
        /// When adding multiple items to the stack, using PushRange is a more efficient
        /// mechanism than using <see cref="Push"/> one item at a time. Additionally, PushRange
        /// guarantees that all of the elements will be added atomically, meaning that no other threads will
        /// be able to inject elements between the elements being pushed. Items at lower indices in the
        /// <paramref name="items"/> array will be pushed before items at higher indices.
        /// </remarks>
        public void PushRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);

            // No op if the count is zero
            if (count == 0)
                return;

            // 将items数组进行封装转换为Nodes
            Node head, tail;
            head = tail = new Node(items[startIndex]);
            for (int i = startIndex + 1; i < startIndex + count; i++)
            {
                Node node = new Node(items[i]);
                node.m_next = head;
                head = node;
            }

            tail.m_next = m_head;
            if (Interlocked.CompareExchange(ref m_head, head, tail.m_next) == tail.m_next)
            {
                return;
            }

            // If we failed, go to the slow path and loop around until we succeed.
            PushCore(head, tail);

        }


        /// <summary>
        /// 将一个或许多节点压人栈中，如果头和尾节点是相同的则将一个节点压人栈中，否则将位于头和尾节点中的列表压人栈中
        /// </summary>
        /// <param name="head">新列表的头节点</param>
        /// <param name="tail">新列表的尾节点</param>
        private void PushCore(Node head, Node tail)
        {
            SpinWait spin = new SpinWait();

            // Keep trying to CAS the exising head with the new node until we succeed.
            // 将已有栈的head节点赋值给要添加栈的尾节点的m_next，再将head节点赋值个已有栈的head节点，
            // 在判断原m_head中的值是否与tail.m_next相等
            do
            {
                spin.SpinOnce();
                // 重复读取头和连接新的节点
                tail.m_next = m_head;
            }
            while (Interlocked.CompareExchange(
                ref m_head, head, tail.m_next) != tail.m_next);

#if !FEATURE_PAL && !FEATURE_CORECLR
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPushFailed(spin.Count);
            }
#endif //!FEATURE_PAL && !FEATURE_CORECLR
        }

        /// <summary>
        /// 本地帮助方法去验证pop push 范围的方法输入校验
        /// </summary>
        private void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange"));
            }
            int length = items.Length;
            if (startIndex >= length || startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange"));
            }
            if (length - count < startIndex) //instead of (startIndex + count > items.Length) to prevent overflow
            {
                throw new ArgumentException(Environment.GetResourceString("ConcurrentStack_PushPopRange_InvalidCount"));
            }
        }

        /// <summary>
        /// 尝试将一个对象添加到 System.Collections.Concurrent.IProducerConsumerCollection<T> 中。
        /// </summary>
        /// <param name="item">The object to add to the <see
        /// cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>. The value can be a null
        /// reference (Nothing in Visual Basic) for reference types.
        /// </param>
        /// <returns>true if the object was added successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentStack{T}"/>, this operation
        /// will always insert the object onto the top of the <see cref="ConcurrentStack{T}"/>
        /// and return true.</remarks>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Push(item);
            return true;
        }

        /// <summary>
        /// 尝试返回 System.Collections.Concurrent.ConcurrentStack<T> 顶部的对象但不将其移除。
        /// </summary>
        /// <param name="result">此方法返回时，result 包含 System.Collections.Concurrent.ConcurrentStack<T> 顶部的对象；
        /// 如果操作失败，则包含未指定的值。</param>
        /// <returns>如果成功返回了对象，则为 true；否则为 false。</returns>
        public bool TryPeek(out T result)
        {
            Node head = m_head;

            // If the stack is empty, return false; else return the element and true.
            if (head == null)
            {
                result = default(T);
                return false;
            }
            else
            {
                result = head.m_value;
                return true;
            }
        }

        /// <summary>
        /// 尝试弹出并返回 System.Collections.Concurrent.ConcurrentStack<T> 顶部的对象。
        /// </summary>
        /// <param name="result">
        /// 此方法返回时，如果操作成功，则 result 包含所移除的对象。 如果没有可供移除的对象，则不指定该值。
        /// </param>
        /// <returns>如果成功移除并返回了 System.Collections.Concurrent.ConcurrentStack<T> 顶部的元素，则为 true；
        /// 否则为false。</returns>
        public bool TryPop(out T result)
        {
            Node head = m_head;
            //stack is empty
            if (head == null)
            {
                result = default(T);
                return false;
            }
            if (Interlocked.CompareExchange(ref m_head, head.m_next, head) == head)//将head.m_next替换为head
            {
                result = head.m_value;
                return true;
            }

            // Fall through to the slow path.
            return TryPopCore(out result);
        }

        /// <summary>
        /// 尝试自动弹出并返回 System.Collections.Concurrent.ConcurrentStack<T> 顶部的多个对象。
        /// </summary>
        /// <param name="items">
        /// 要将从 System.Collections.Concurrent.ConcurrentStack<T> 顶部弹出的对象添加到的 System.Array。
        /// </param>
        /// <returns>已成功从 System.Collections.Concurrent.ConcurrentStack<T> 
        /// 顶部弹出并插入到 items 中的对象数。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null argument (Nothing
        /// in Visual Basic).</exception>
        /// <remarks>
        /// When popping multiple items, if there is little contention on the stack, using
        /// TryPopRange can be more efficient than using <see cref="TryPop"/>
        /// once per item to be removed.  Nodes fill the <paramref name="items"/>
        /// with the first node to be popped at the startIndex, the second node to be popped
        /// at startIndex + 1, and so on.
        /// </remarks>
        public int TryPopRange(T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return TryPopRange(items, 0, items.Length);
        }

        /// <summary>
        /// 尝试自动弹出并返回 System.Collections.Concurrent.ConcurrentStack<T> 顶部的多个对象。
        /// </summary>
        /// <param name="items">
        /// 要将从 System.Collections.Concurrent.ConcurrentStack<T> 顶部弹出的对象添加到的 System.Array。
        /// </param>
        /// <param name="startIndex">items 中从零开始的偏移量，
        /// 从此处开始插入 System.Collections.Concurrent.ConcurrentStack<T>顶部的元素。</param>
        /// <param name="count">T将从 System.Collections.Concurrent.ConcurrentStack<T>
        /// 顶部弹出并插入到 items 中的元素数。</param>
        /// <returns>已成功从堆栈顶部弹出并插入到 items 中的对象数。</returns>        
        /// <exception cref="ArgumentNullException">items 是 null 引用（在 Visual Basic 中为 Nothing）。</exception>
        /// <exception cref="ArgumentOutOfRangeException">startIndex 或 count 为负。
        /// 或 startIndex 大于或等于 items 的长度。</exception>
        /// <exception cref="ArgumentException">startIndex + count 大于 items 的长度。</exception>
        /// <remarks>
        /// When popping multiple items, if there is little contention on the stack, using
        /// TryPopRange can be more efficient than using <see cref="TryPop"/>
        /// once per item to be removed.  Nodes fill the <paramref name="items"/>
        /// with the first node to be popped at the startIndex, the second node to be popped
        /// at startIndex + 1, and so on.
        /// </remarks>
        public int TryPopRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);

            // No op if the count is zero
            if (count == 0)
                return 0;

            Node poppedHead;
            int nodesCount = TryPopCore(count, out poppedHead);
            if (nodesCount > 0)
            {
                CopyRemovedItems(poppedHead, items, startIndex, nodesCount);

            }
            return nodesCount;

        }

        /// <summary>
        /// Local helper function to Pop an item from the stack, slow path
        /// </summary>
        /// <param name="result">The popped item</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private bool TryPopCore(out T result)
        {
            Node poppedNode;

            if (TryPopCore(1, out poppedNode) == 1)
            {
                result = poppedNode.m_value;
                return true;
            }

            result = default(T);
            return false;

        }

        /// <summary>
        /// Slow path helper for TryPop. This method assumes an initial attempt to pop an element
        /// has already occurred and failed, so it begins spinning right away.
        /// </summary>
        /// <param name="count">The number of items to pop.</param>
        /// <param name="poppedHead">
        /// When this method returns, if the pop succeeded, contains the removed object. If no object was
        /// available to be removed, the value is unspecified. This parameter is passed uninitialized.
        /// </param>
        /// <returns>True if an element was removed and returned; otherwise, false.</returns>
        private int TryPopCore(int count, out Node poppedHead)
        {
            SpinWait spin = new SpinWait();

            // Try to CAS the head with its current next.  We stop when we succeed or
            // when we notice that the stack is empty, whichever comes first.
            Node head;
            Node next;
            int backoff = 1;
            Random r = new Random(Environment.TickCount & Int32.MaxValue); // avoid the case where TickCount could return Int32.MinValue
            while (true)
            {
                head = m_head;
                // 如果为空，则返回空指针
                if (head == null)
                {
#if !FEATURE_PAL && !FEATURE_CORECLR
                    if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
                    {
                        CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spin.Count);
                    }
#endif //!FEATURE_PAL && !FEATURE_CORECLR
                    poppedHead = null;
                    return 0;
                }

                // 用于遍历出要pop出节点集合的数目和尾巴
                next = head;
                int nodesCount = 1;
                for (; nodesCount < count && next.m_next != null; nodesCount++)
                {
                    next = next.m_next;
                }

                // Try to swap the new head.  If we succeed, break out of the loop.
                // 尝试替换新的head，如果我们成功，跳出循环
                if (Interlocked.CompareExchange(ref m_head, next.m_next, head) == head)
                {
#if !FEATURE_PAL && !FEATURE_CORECLR
                    if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
                    {
                        CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spin.Count);
                    }
#endif //!FEATURE_PAL && !FEATURE_CORECLR
                    // Return the popped Node.
                    poppedHead = head;
                    return nodesCount;
                }

                // We failed to CAS the new head.  Spin briefly and retry.
                // 下面的代码不知道
                for (int i = 0; i < backoff; i++)
                {
                    spin.SpinOnce();
                }

                backoff = spin.NextSpinWillYield ? r.Next(1, BACKOFF_MAX_YIELDS) : backoff * 2;
            }
        }


        /// <summary>
        /// 本地帮助函数去复制pop出的元素给一个集合
        /// </summary>
        /// <param name="head">被用于复制的列表头</param>
        /// <param name="collection">存放pop出元素的集合</param>
        /// <param name="startIndex">存放pop元素的开始索引</param>
        /// <param name="nodesCount">节点数目</param>
        private void CopyRemovedItems(Node head, T[] collection, int startIndex, int nodesCount)
        {
            Node current = head;
            for (int i = startIndex; i < startIndex + nodesCount; i++)
            {
                collection[i] = current.m_value;
                current = current.m_next;
            }

        }

        /// <summary>
        /// Attempts to remove and return an object from the <see
        /// cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, <paramref name="item"/> contains the
        /// object removed. If no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>true if an element was removed and returned succesfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentStack{T}"/>, this operation will attempt to pope the object at
        /// the top of the <see cref="ConcurrentStack{T}"/>.
        /// </remarks>
        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            return TryPop(out item);
        }

        /// <summary>
        /// Copies the items stored in the <see cref="ConcurrentStack{T}"/> to a new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of elements copied from the <see
        /// cref="ConcurrentStack{T}"/>.</returns>
        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        /// <summary>
        /// 返回一个包含list内容快照的数组，使用目标列表节点作新列表范围的头
        /// </summary>
        /// <returns>一个list内容的数组</returns>
        private List<T> ToList()
        {
            List<T> list = new List<T>();

            Node curr = m_head;
            while (curr != null)
            {
                list.Add(curr.m_value);
                curr = curr.m_next;
            }
            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the <see cref="ConcurrentStack{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the stack.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the stack.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // Returns an enumerator for the stack. This effectively takes a snapshot
            // of the stack's contents at the time of the call, i.e. subsequent modifications
            // (pushes or pops) will not be reflected in the enumerator's contents.

            //If we put yield-return here, the iterator will be lazily evaluated. As a result a snapshot of
            //the stack is not taken when GetEnumerator is initialized but when MoveNext() is first called.
            //This is inconsistent with existing generic collections. In order to prevent it, we capture the 
            //value of m_head in a buffer and call out to a helper method
            return GetEnumerator(m_head);
        }

        private IEnumerator<T> GetEnumerator(Node head)
        {
            Node current = head;
            while (current != null)
            {
                yield return current.m_value;
                current = current.m_next;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through
        /// the collection.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the stack. It does not
        /// reflect any updates to the collection after
        /// <see cref="GetEnumerator"/> was called. The enumerator is safe to use concurrently with reads
        /// from and writes to the stack.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
