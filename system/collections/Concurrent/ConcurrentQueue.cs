#pragma warning disable 0420

// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcurrentQueue.cs
//
// <OWNER>[....]</OWNER>
//
// A lock-free, concurrent queue primitive, and its associated debugger view type.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{

    /// <summary>
    /// 表示线程安全的先进先出 (FIFO) 集合。
    /// </summary>
    /// <typeparam name="T"> 队列中包含的元素的类型。</typeparam>
    /// <remarks>
    /// All public  and protected members of <see cref="ConcurrentQueue{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<>))]
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    [Serializable]
    public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        //fields of ConcurrentQueue
        [NonSerialized]
        private volatile Segment m_head;

        [NonSerialized]
        private volatile Segment m_tail;

        private T[] m_serializationArray; // Used for custom serialization.

        private const int SEGMENT_SIZE = 32;

        //number of snapshot takers, GetEnumerator(), ToList() and ToArray() operations take snapshot.
        [NonSerialized]
        internal volatile int m_numSnapshotTakers = 0;

        /// <summary>
        /// 初始化一个新的ConcurrentQueue类
        /// </summary>
        public ConcurrentQueue()
        {
            m_head = m_tail = new Segment(0, this);
        }

        /// <summary>
        /// 从一个存在的集合中初始化队列内容 （迭代器和数组）
        /// </summary>
        /// <param name="collection">从其复制元素的集合</param>
        private void InitializeFromCollection(IEnumerable<T> collection)
        {
            // 使用本地变量去避免额外的读和写，这是安全的，因为他只被构造器调用
            Segment localTail = new Segment(0, this);
            m_head = localTail; 

            int index = 0;
            foreach (T element in collection)//将集合元素遍历，并插入到segment中
            {
                Contract.Assert(index >= 0 && index < SEGMENT_SIZE);
                localTail.UnsafeAdd(element);//将元素插入到localTail(segment)中
                index++;//将索引加一

                if (index >= SEGMENT_SIZE)//再次判断segment是否已经满了
                {
                    localTail = localTail.UnsafeGrow();//如果满了，将更新localTail，即创建一个新的segment附加到当前的segment后。
                    index = 0;//更新索引
                }
            }

            m_tail = localTail;//更新当前队列的m_tail
        }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentQueue<T> 类的新实例，该类包含从指定集合中复制的元素
        /// </summary>
        /// <param name="collection">其元素被复制到新的 System.Collections.Concurrent.ConcurrentQueue<T> 中的集合。</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection"/>collection 参数为 null。</exception>
        public ConcurrentQueue(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// 获取数据数组序列化
        /// </summary>
        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            // save the data into the serialization array to be saved
            m_serializationArray = ToArray();
        }

        /// <summary>
        /// 从一个序列化对象中构造队列
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Contract.Assert(m_serializationArray != null);
            InitializeFromCollection(m_serializationArray);
            m_serializationArray = null;
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see
        /// cref="T:System.Array"/>, starting at a particular
        /// <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="T:System.Collections.Concurrent.ConcurrentBag"/>. The <see
        /// cref="T:System.Array">Array</see> must have zero-based indexing.</param>
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

            // We must be careful not to corrupt the array, so we will first accumulate an
            // internal list of elements that we will then copy to the array. This requires
            // some extra allocation, but is necessary since we don't know up front whether
            // the array is sufficiently large to hold the stack's contents.
            ((ICollection)ToList()).CopyTo(array, index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// with the SyncRoot; otherwise, false. For <see cref="ConcurrentQueue{T}"/>, this property always
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
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>
        /// Attempts to add an object to the <see
        /// cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see
        /// cref="T:System.Collections.Concurrent.IProducerConsumerCollection{T}"/>. The value can be a null
        /// reference (Nothing in Visual Basic) for reference types.
        /// </param>
        /// <returns>true if the object was added successfully; otherwise, false.</returns>
        /// <remarks>For <see cref="ConcurrentQueue{T}"/>, this operation will always add the object to the
        /// end of the <see cref="ConcurrentQueue{T}"/>
        /// and return true.</remarks>
        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Enqueue(item);
            return true;
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
        /// <remarks>For <see cref="ConcurrentQueue{T}"/>, this operation will attempt to remove the object
        /// from the beginning of the <see cref="ConcurrentQueue{T}"/>.
        /// </remarks>
        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            return TryDequeue(out item);
        }

        /// <summary>
        /// 获取一个指示 System.Collections.Concurrent.ConcurrentQueue<T> 是否为空的值。
        /// </summary>
        /// <value>如果 System.Collections.Concurrent.ConcurrentQueue<T> 为空，则为 true；否则为 false。</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of this property is recommended
        /// rather than retrieving the number of items from the <see cref="Count"/> property and comparing it
        /// to 0.  However, as this collection is intended to be accessed concurrently, it may be the case
        /// that another thread will modify the collection after <see cref="IsEmpty"/> returns, thus invalidating
        /// the result.
        /// </remarks>
        public bool IsEmpty
        {
            get
            {
                Segment head = m_head;
                if (!head.IsEmpty)
                    //fast route 1:
                    //如果当前head不为空，则表示队列不为空
                    return false;
                else if (head.Next == null)
                    //fast route 2:
                    //如果当前head是空并且最后一个segment也是空，则表示队列为空
                    return true;
                else
                //slow route:
                //当前head是空，但是最后一个segment不为空，他意味着其他线程正在增长新的segment
                {
                    SpinWait spin = new SpinWait();
                    while (head.IsEmpty)//循环等待判断
                    {
                        if (head.Next == null)
                            return true;

                        spin.SpinOnce();
                        head = m_head;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// 将 System.Collections.Concurrent.ConcurrentQueue<T> 中存储的元素复制到新数组中。
        /// </summary>
        /// <returns>一个新数组，其中包含从 System.Collections.Concurrent.ConcurrentQueue<T> 复制的元素的快照。</returns>
        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        /// <summary>
        /// 复制ConcurrentQueue{T}元素到一个新的List{T}
        /// </summary>
        /// <returns>A new <see cref="T:System.Collections.Generic.List{T}"/> containing a snapshot of
        /// elements copied from the <see cref="ConcurrentQueue{T}"/>.</returns>
        private List<T> ToList()
        {
            // 增加行为快照捕获的数目，在快照发生之前增加必须发生。与此同时，在list复制完毕后，自减必须发生。
            // 只有通过这种方法，在segment.TryRemove()的时候，检查m_numSnapshotTakers是否等于0，才能消除竞争条件，
            Interlocked.Increment(ref m_numSnapshotTakers);

            List<T> list = new List<T>();
            try
            {
                //在缓存中存储head 和 tail 位置
                Segment head, tail;
                int headLow, tailHigh;
                GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);

                if (head == tail)//表示head 和 tail segment是相同的，即只有一个segment
                {
                    head.AddToList(list, headLow, tailHigh);
                }
                else// 有多个segment
                {
                    head.AddToList(list, headLow, SEGMENT_SIZE - 1);// 先将第一segment填满
                    Segment curr = head.Next;// 获取下一个segment
                    while (curr != tail)//进行循环插入list 直到尾部segment
                    {
                        curr.AddToList(list, 0, SEGMENT_SIZE - 1);
                        curr = curr.Next;
                    }
                    //添加尾segment
                    tail.AddToList(list, 0, tailHigh);
                }
            }
            finally
            {
                // 在复制完毕后自减必须发生
                Interlocked.Decrement(ref m_numSnapshotTakers);
            }
            return list;
        }

        /// <summary>
        /// 存储当前head和tail位置
        /// </summary>
        /// <param name="head">return the head segment</param>
        /// <param name="tail">return the tail segment</param>
        /// <param name="headLow">return the head offset, value range [0, SEGMENT_SIZE]在head segment中索引</param>
        /// <param name="tailHigh">return the tail offset, value range [-1, SEGMENT_SIZE-1]在tail segment中的索引</param>
        private void GetHeadTailPositions(out Segment head, out Segment tail,
            out int headLow, out int tailHigh)
        {
            head = m_head;
            tail = m_tail;
            headLow = head.Low;
            tailHigh = tail.High;
            SpinWait spin = new SpinWait();

            //直到被观察的值是稳定的和明显的，我们将循环
            //这保证通过其他方法排序更新是默认的
            while (
                //if head and tail changed, retry
                head != m_head || tail != m_tail
                //if low and high pointers, retry
                || headLow != head.Low || tailHigh != tail.High
                //if head jumps ahead of tail because of concurrent grow and dequeue, retry
                || head.m_index > tail.m_index)
            {
                spin.SpinOnce();
                head = m_head;
                tail = m_tail;
                headLow = head.Low;
                tailHigh = tail.High;
            }
        }


        /// <summary>
        /// 获取 System.Collections.Concurrent.ConcurrentQueue<T> 中包含的元素数。
        /// </summary>
        /// <value>System.Collections.Concurrent.ConcurrentQueue<T> 中包含的元素个数。</value>
        /// <remarks>
        /// For determining whether the collection contains any items, use of the <see cref="IsEmpty"/>
        /// property is recommended rather than retrieving the number of items from the <see cref="Count"/>
        /// property and comparing it to 0.
        /// </remarks>
        public int Count
        {
            get
            {
                //store head and tail positions in buffer, 
                Segment head, tail;
                int headLow, tailHigh;
                GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);

                if (head == tail)//只有一个segment
                {
                    return tailHigh - headLow + 1;
                }

                //head segment 数量
                int count = SEGMENT_SIZE - headLow;

                //中间segment，所有的都是满的
                //We don't deal with overflow to be consistent with the behavior of generic types in CLR.
                count += SEGMENT_SIZE * ((int)(tail.m_index - head.m_index - 1));

                //tail segment 数量
                count += tailHigh + 1;

                return count;
            }
        }


        /// <summary>
        /// Copies the <see cref="ConcurrentQueue{T}"/> elements to an existing one-dimensional <see
        /// cref="T:System.Array">Array</see>, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array">Array</see> that is the
        /// destination of the elements copied from the
        /// <see cref="ConcurrentQueue{T}"/>. The <see cref="T:System.Array">Array</see> must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is a null reference (Nothing in
        /// Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// zero.</exception>
        /// <exception cref="ArgumentException"><paramref name="index"/> is equal to or greater than the
        /// length of the <paramref name="array"/>
        /// -or- The number of elements in the source <see cref="ConcurrentQueue{T}"/> is greater than the
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
            ToList().CopyTo(array, index);
        }


        /// <summary>
        /// Returns an enumerator that iterates through the <see
        /// cref="ConcurrentQueue{T}"/>.
        /// </summary>
        /// <returns>An enumerator for the contents of the <see
        /// cref="ConcurrentQueue{T}"/>.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents
        /// of the queue.  It does not reflect any updates to the collection after 
        /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        /// concurrently with reads from and writes to the queue.
        /// </remarks>
        public IEnumerator<T> GetEnumerator()
        {
            // Increments the number of active snapshot takers. This increment must happen before the snapshot is 
            // taken. At the same time, Decrement must happen after the enumeration is over. Only in this way, can it
            // eliminate race condition when Segment.TryRemove() checks whether m_numSnapshotTakers == 0. 
            Interlocked.Increment(ref m_numSnapshotTakers);

            // Takes a snapshot of the queue. 
            // A design flaw here: if a Thread.Abort() happens, we cannot decrement m_numSnapshotTakers. But we cannot 
            // wrap the following with a try/finally block, otherwise the decrement will happen before the yield return 
            // statements in the GetEnumerator (head, tail, headLow, tailHigh) method.           
            Segment head, tail;
            int headLow, tailHigh;
            GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);

            //If we put yield-return here, the iterator will be lazily evaluated. As a result a snapshot of
            // the queue is not taken when GetEnumerator is initialized but when MoveNext() is first called.
            // This is inconsistent with existing generic collections. In order to prevent it, we capture the 
            // value of m_head in a buffer and call out to a helper method.
            //The old way of doing this was to return the ToList().GetEnumerator(), but ToList() was an 
            // unnecessary perfomance hit.
            return GetEnumerator(head, tail, headLow, tailHigh);
        }

        /// <summary>
        /// Helper method of GetEnumerator to seperate out yield return statement, and prevent lazy evaluation. 
        /// </summary>
        private IEnumerator<T> GetEnumerator(Segment head, Segment tail, int headLow, int tailHigh)
        {
            try
            {
                SpinWait spin = new SpinWait();

                if (head == tail)
                {
                    for (int i = headLow; i <= tailHigh; i++)
                    {
                        // 如果位置通过添加操作被保留，但是值却未被写入，在值有效之前一直自旋
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return head.m_array[i];//迭代返回数组内容
                    }
                }
                else
                {
                    //在head segment 中的迭代器
                    for (int i = headLow; i < SEGMENT_SIZE; i++)
                    {
                        // If the position is reserved by an Enqueue operation, but the value is not written into,
                        // spin until the value is available.
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return head.m_array[i];
                    }
                    //在中间segment的
                    Segment curr = head.Next;
                    while (curr != tail)
                    {
                        for (int i = 0; i < SEGMENT_SIZE; i++)
                        {
                            // If the position is reserved by an Enqueue operation, but the value is not written into,
                            // spin until the value is available.
                            spin.Reset();
                            while (!curr.m_state[i].m_value)
                            {
                                spin.SpinOnce();
                            }
                            yield return curr.m_array[i];
                        }
                        curr = curr.Next;
                    }

                    //在tail segment中的迭代器
                    for (int i = 0; i <= tailHigh; i++)
                    {
                        // If the position is reserved by an Enqueue operation, but the value is not written into,
                        // spin until the value is available.
                        spin.Reset();
                        while (!tail.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }
                        yield return tail.m_array[i];
                    }
                }
            }
            finally
            {
                // 减操作一定发生在迭代完毕后
                Interlocked.Decrement(ref m_numSnapshotTakers);
            }
        }

        /// <summary>
        /// 将对象添加到 System.Collections.Concurrent.ConcurrentQueue<T> 的结尾处。
        /// </summary>
        /// <param name="item">要添加到 System.Collections.Concurrent.ConcurrentQueue<T> 的结尾处的对象。
        /// 该值对于引用类型可以是空引用
        /// </param>
        public void Enqueue(T item)
        {
            SpinWait spin = new SpinWait();
            while (true)//等待其他线程处理，调用尾segment.TryAppend()进行添加
            {
                Segment tail = m_tail;
                if (tail.TryAppend(item))
                    return;
                spin.SpinOnce();
            }
        }


        /// <summary>
        /// 尝试移除并返回位于并发队列开头处的对象。
        /// </summary>
        /// <param name="result">
        /// 此方法返回时，如果操作成功，则 result 包含所移除的对象。 如果没有可供移除的对象，则不指定该值。
        /// </param>
        /// <returns>如果成功在 System.Collections.Concurrent.ConcurrentQueue<T> 开头处移除并返回了元素，则为 true；否则为false.</returns>
        public bool TryDequeue(out T result)
        {
            while (!IsEmpty)//判断队列是否为空
            {
                Segment head = m_head;
                if (head.TryRemove(out result))//移除元素
                    return true;
                //在IsEmpty中实现了自旋方法，我们不需要在while循环自旋
            }
            result = default(T);
            return false;
        }

        /// <summary>
        /// 尝试返回 System.Collections.Concurrent.ConcurrentQueue<T> 开头处的对象但不将其移除。.
        /// </summary>
        /// <param name="result">此方法返回时，result 包含 System.Collections.Concurrent.ConcurrentQueue<T> 开始处的对象；
        /// 如果操作失败，则包含未指定的值。</param>
        /// <returns>如果成功返回了对象，则为 true；否则为 false。</returns>
        public bool TryPeek(out T result)
        {
            Interlocked.Increment(ref m_numSnapshotTakers);

            while (!IsEmpty)
            {
                Segment head = m_head;
                if (head.TryPeek(out result))
                {
                    Interlocked.Decrement(ref m_numSnapshotTakers);
                    return true;
                }
                //在IsEmpty中实现了自旋方法，我们不需要在while循环自旋
            }
            result = default(T);
            Interlocked.Decrement(ref m_numSnapshotTakers);
            return false;
        }


        /// <summary>
        /// 为ConcurrentQueue的私有类
        /// 一个队列是一个小数组的连接列表，每个节点被叫做一个segment。
        /// 一个segment包含一个数组，一个指向下一个segment，和m_low，m_high 目录节点
        /// 在数组中第一个和最后一个元素
        /// </summary>
        private class Segment
        {
            //we define two volatile arrays: m_array and m_state. Note that the accesses to the array items 
            //do not get volatile treatment. But we don't need to worry about loading adjacent elements or 
            //store/load on adjacent elements would suffer reordering. 
            // - Two stores:  these are at risk, but CLRv2 memory model guarantees store-release hence we are safe.
            // - Two loads: because one item from two volatile arrays are accessed, the loads of the array references
            //          are sufficient to prevent reordering of the loads of the elements.
            internal volatile T[] m_array;// 内部数组

            // 为了m_arrray中的每一个条目，在m_state中相应的条目无论这个位置是否包含一个有效值，m_state都将最初初始化为false
            internal volatile VolatileBool[] m_state;

            //pointer to the next segment. null if the current segment is the last segment
            // 指向下一个segment的指针，如果当前segment是最后一个segment，那么他就是空
            private volatile Segment m_next;

            //We use this zero based index to track how many segments have been created for the queue, and
            //to compute how many active segments are there currently. 
            // * The number of currently active segments is : m_tail.m_index - m_head.m_index + 1;
            // * m_index is incremented with every Segment.Grow operation. We use Int64 type, and we can safely 
            //   assume that it never overflows. To overflow, we need to do 2^63 increments, even at a rate of 4 
            //   billion (2^32) increments per second, it takes 2^31 seconds, which is about 64 years.
            internal readonly long m_index;

            //indices of where the first and last valid values
            // - m_low points to the position of the next element to pop from this segment, range [0, infinity)
            //      m_low >= SEGMENT_SIZE implies the segment is disposable
            // - m_high points to the position of the latest pushed element, range [-1, infinity)
            //      m_high == -1 implies the segment is new and empty
            //      m_high >= SEGMENT_SIZE-1 means this segment is ready to grow. 
            //        and the thread who sets m_high to SEGMENT_SIZE-1 is responsible to grow the segment
            // - Math.Min(m_low, SEGMENT_SIZE) > Math.Min(m_high, SEGMENT_SIZE-1) implies segment is empty
            // - initially m_low =0 and m_high=-1;
            private volatile int m_low;
            private volatile int m_high;

            private volatile ConcurrentQueue<T> m_source;

            /// <summary>
            /// 通过规定的索引，创建和初始化一个segment
            /// </summary>
            internal Segment(long index, ConcurrentQueue<T> source)
            {
                m_array = new T[SEGMENT_SIZE];
                m_state = new VolatileBool[SEGMENT_SIZE]; //全部初始化为false
                m_high = -1;
                Contract.Assert(index >= 0);
                m_index = index;
                m_source = source;
            }

            /// <summary>
            /// 返回下一个segment
            /// </summary>
            internal Segment Next
            {
                get { return m_next; }
            }


            /// <summary>
            /// 如果当前segment是空（没有任何有效元素去添加），则返回true
            /// 否则返回false
            /// </summary>
            internal bool IsEmpty
            {
                get { return (Low > High); }
            }

            /// <summary>
            /// 添加一个元素到当前segment的尾部，专门被ConcurrentQueue.InitializedFromCollection调用
            /// InitializedFromCollection 是保证没有索引越界，和没有争论（不安全地）
            /// </summary>
            /// <param name="value"></param>
            internal void UnsafeAdd(T value)
            {
                Contract.Assert(m_high < SEGMENT_SIZE - 1);//判断是否越界
                m_high++;
                m_array[m_high] = value;
                m_state[m_high].m_value = true;
            }

            /// <summary>
            /// 创建一个新的segment并且附加到当前segment上，但是不更新m_tail节点
            /// 专门被ConcurrentQueue.InitializedFromCollection调用
            /// InitializedFromCollection 是保证没有索引越界，和没有争论（不安全地）
            /// </summary>
            /// <returns>返回新segment的引用</returns>
            internal Segment UnsafeGrow()
            {
                Contract.Assert(m_high >= SEGMENT_SIZE - 1);
                Segment newSegment = new Segment(m_index + 1, m_source); //m_index是int64即long，我们不需要担心越界
                m_next = newSegment;//将当前segment的next设置为初始化的segment，但是不更新m_tail
                return newSegment;
            }

            /// <summary>
            /// 创建一个新的segment并且附加到当前segment上，并更新m_tail指针
            /// 当无竞争时，方法被调用
            /// </summary>
            internal void Grow()
            {
                //no CAS is needed, since there is no contention (other threads are blocked, busy waiting)
                Segment newSegment = new Segment(m_index + 1, m_source);  //m_index is Int64, we don't need to worry about overflow
                m_next = newSegment;
                Contract.Assert(m_source.m_tail == this);//判断当前segment是否是尾节点
                m_source.m_tail = m_next;
            }


            /// <summary>
            /// 尝试将一个元素附加到这个segment的尾部
            /// </summary>
            /// <param name="value">附加的元素</param>
            /// <param name="tail">The tail.</param>
            /// <returns>如果元素附加成功，则返回true，如果当前segment慢的，则返回false</returns>
            /// <remarks>if appending the specified element succeeds, and after which the segment is full, 
            /// then grow the segment</remarks>
            internal bool TryAppend(T value)
            {
                //快速检查m_high是否越界，如果越界，则跳出
                if (m_high >= SEGMENT_SIZE - 1)
                {
                    return false;
                }

                //现在我们将使用一个CAS去增加m_high，并且存储结构在newhigh中。
                //依赖于在这个segment中有多少个空余节点以及有多少线程同时在做插入，返回的"newhigh"可能
                // 1) < SEGMENT_SIZE - 1 : 我们在这个segment中插入一个节点，并且不是最后一个，
                // 2) == SEGMENT_SIZE - 1 : 我们将处理最后一个节点，插入值并且增长segment
                // 3) > SEGMENT_SIZE - 1 : 我们失败于在这个segment中存储一个节点，我们将给Queue.Enqueue方法失败，并告诉他在下一个segment中尝试

                int newhigh = SEGMENT_SIZE; //初始化值去防止越界

                //We need do Interlocked.Increment and value/state update in a finally block to ensure that they run
                //without interuption. This is to prevent anything from happening between them, and another dequeue
                //thread maybe spinning forever to wait for m_state[] to be true;
                try
                { }
                finally
                {
                    newhigh = Interlocked.Increment(ref m_high);
                    if (newhigh <= SEGMENT_SIZE - 1)
                    {
                        m_array[newhigh] = value;
                        m_state[newhigh].m_value = true;
                    }

                    //if this thread takes up the last slot in the segment, then this thread is responsible
                    //to grow a new segment. Calling Grow must be in the finally block too for reliability reason:
                    //if thread abort during Grow, other threads will be left busy spinning forever.
                    if (newhigh == SEGMENT_SIZE - 1)
                    {
                        Grow();
                    }
                }

                //如果 newhigh <= SEGMENT_SIZE-1, 他表示当前线程成功插入一点
                return newhigh <= SEGMENT_SIZE - 1;
            }


            /// <summary>
            /// 尝试从当前segment的首部移除一个元素
            /// </summary>
            /// <param name="result">移除结果</param>
            /// <param name="head">The head.</param>
            /// <returns>如果当前segment为空，则返回为false</returns>
            internal bool TryRemove(out T result)
            {
                SpinWait spin = new SpinWait();
                int lowLocal = Low, highLocal = High;
                while (lowLocal <= highLocal)
                {
                    //尝试更新m_low，将m_low向后移动一位
                    if (Interlocked.CompareExchange(ref m_low, lowLocal + 1, lowLocal) == lowLocal)
                    {
                        //if the specified value is not available (this spot is taken by a push operation,
                        // but the value is not written into yet), then spin
                        SpinWait spinLocal = new SpinWait();
                        while (!m_state[lowLocal].m_value)// 如果当前位置的m_state为false，则进行自旋一次，直到为true，但是一般不会进入这个循环
                        {
                            spinLocal.SpinOnce();
                        }
                        result = m_array[lowLocal];//进行结果复制

                        // If there is no other thread taking snapshot (GetEnumerator(), ToList(), etc), reset the deleted entry to null.
                        // It is ok if after this conditional check m_numSnapshotTakers becomes > 0, because new snapshots won't include 
                        // the deleted entry at m_array[lowLocal]. 
                        if (m_source.m_numSnapshotTakers <= 0)
                        {
                            m_array[lowLocal] = default(T); //release the reference to the object. 
                        }

                        //如果当前线程设置m_low对于SEGMENT_SIZE,那表示当前segment已经被消耗完了，
                        //然后这个线程将有责任去注销这个segment，然后重置m_head
                        if (lowLocal + 1 >= SEGMENT_SIZE)
                        {
                            //  Invariant: we only dispose the current m_head, not any other segment
                            //  In usual situation, disposing a segment is simply seting m_head to m_head.m_next
                            //  But there is one special case, where m_head and m_tail points to the same and ONLY
                            //segment of the queue: Another thread A is doing Enqueue and finds that it needs to grow,
                            //while the *current* thread is doing *this* Dequeue operation, and finds that it needs to 
                            //dispose the current (and ONLY) segment. Then we need to wait till thread A finishes its 
                            //Grow operation, this is the reason of having the following while loop
                            spinLocal = new SpinWait();//多线程操作的处理
                            while (m_next == null)
                            {
                                spinLocal.SpinOnce();
                            }
                            Contract.Assert(m_source.m_head == this);
                            m_source.m_head = m_next;
                        }
                        return true;
                    }
                    else
                    {
                        //CAS由于竞争失败：短暂自旋和重置
                        spin.SpinOnce();
                        lowLocal = Low; highLocal = High;
                    }
                }//end of while
                result = default(T);//segment为空，构造一个默认对象或值，返回false
                return false;
            }

            /// <summary>
            /// 尝试peek当前的segment
            /// </summary>
            /// <param name="result">holds the return value of the element at the head position, 
            /// value set to default(T) if there is no such an element</param>
            /// <returns>true if there are elements in the current segment, false otherwise</returns>
            internal bool TryPeek(out T result)
            {
                result = default(T);
                int lowLocal = Low;
                if (lowLocal > High)
                    return false;
                SpinWait spin = new SpinWait();
                while (!m_state[lowLocal].m_value)
                {
                    spin.SpinOnce();
                }
                result = m_array[lowLocal];
                return true;
            }

            /// <summary>
            /// 将当前segment的部分或者全部添加到一个List中
            /// </summary>
            /// <param name="list">the list to which to add</param>
            /// <param name="start">the start position</param>
            /// <param name="end">the end position</param>
            internal void AddToList(List<T> list, int start, int end)
            {
                for (int i = start; i <= end; i++)
                {
                    SpinWait spin = new SpinWait();
                    while (!m_state[i].m_value)
                    {
                        spin.SpinOnce();
                    }
                    list.Add(m_array[i]);
                }
            }

            /// <summary>
            /// 返回当前segment的首位置，值的范围是[0,SEGMENT_SIZE],
            /// 如果他是SEGMENT_SIZE,他意味着这个segment是耗尽的，也就是空的
            /// </summary>
            internal int Low
            {
                get
                {
                    return Math.Min(m_low, SEGMENT_SIZE);
                }
            }

            /// <summary>
            /// 返回当前segment尾部的合理位置，值的范围是[-1,SEGMENT-1].
            /// 当他是-1时，他意味着这是一个新的segment，并且还没有元素
            /// </summary>
            internal int High
            {
                get
                {
                    //如果m_high > SEGMENT_SIZE,他表示他已经越过范围，我们应该返回SEGMENT_SIZE-1作为合理的位置
                    return Math.Min(m_high, SEGMENT_SIZE - 1);
                }
            }

        }
    }//end of class Segment

    /// <summary>
    /// 一个为了包装volatile(不稳定的) bool的结构体，请注意结构体他自己复制将不是volatile
    /// 为了这个例子将不包括不稳定的操作volatileBool1 = volatileBool2 jit将复制结构体并且将忽略volatile
    /// </summary>
    struct VolatileBool
    {
        public VolatileBool(bool value)
        {
            m_value = value;
        }
        public volatile bool m_value;
    }
}
