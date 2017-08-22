// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IProducerConsumerCollection.cs
//
// <OWNER>[....]</OWNER>
//
// A common interface for all concurrent collections.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{

    /// <summary>
    /// 定义供制造者/使用者用来操作线程安全集合的方法。。。。。。。...
    /// </summary>
    /// <typeparam name="T">指定集合中的元素的类型。</typeparam>
    /// <remarks>
    /// 此接口提供一个统一的表示（为生产者/消费者集合），从而更高级别抽象如System.Collections.Concurrent.BlockingCollection<T>
    /// 可以集合作为基础的存储机制。
    /// </remarks>
    public interface IProducerConsumerCollection<T> : IEnumerable<T>, ICollection
    {

        /// <summary>
        /// 从指定的索引位置开始，将 System.Collections.Concurrent.IProducerConsumerCollection<T>
        /// 的元素复制到 System.Array 中。
        /// </summary>
        /// <param name="array">一维<see cref="T:System.Array"/> that is the destination of
        /// the elements copied from the <see cref="IProducerConsumerCollection{T}"/>.
        /// The array must have zero-based indexing.</param>
        /// <param name="index">必须从零开始</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/>是一个空引用</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>小于零</exception>
        /// <exception cref="ArgumentException">index 等于或大于 array 的长度 - 或 - 集合中的元素数大于从 index 到目标 array 结尾的可用空间。
        /// </exception>
        void CopyTo(T[] array, int index);

        /// <summary>
        /// 尝试将一个对象添加到 System.Collections.Concurrent.IProducerConsumerCollection<T> 中。
        /// </summary>
        /// <param name="item">要添加到<see
        /// cref="IProducerConsumerCollection{T}"/>中的对象</param>
        /// <returns>如果成功添加了对象，则为 true；否则为 false。</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="item"/>对此集合无效</exception>
        bool TryAdd(T item);

        /// <summary>
        ///  尝试从<see cref="IProducerConsumerCollection{T}"/>移除并返回一个对象。
        /// </summary>
        /// <param name="item">
        /// 此方法返回时，如果成功移除并返回了对象，则 item 包含所移除的对象。 如果没有可供移除的对象，则不指定该值。
        /// </param>
        /// <returns>如果成功移除并返回了对象，则为 true；否则为 false。</returns>
        bool TryTake(out T item);

        /// <summary>
        /// 将<see cref="IProducerConsumerCollection{T}"/>中包含的元素复制到新数组中
        /// </summary>
        /// <returns>一个新数组，其中包含从 System.Collections.Concurrent.IProducerConsumerCollection<T>复制的元素。</returns>
        T[] ToArray();

    }


    /// <summary>
    /// 一个IProducerConsumerCollection的调试信息，那可以更加容易浏览一个时间点内的内容信息
    /// </summary>
    /// <typeparam name="T">The type of elements stored within.</typeparam>
    internal sealed class SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<T>
    {
        private IProducerConsumerCollection<T> m_collection; // The collection being viewed.

        /// <summary>
        /// Constructs a new debugger view object for the provided collection object.
        /// </summary>
        /// <param name="collection">A collection to browse in the debugger.</param>
        public SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView(IProducerConsumerCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            m_collection = collection;
        }

        /// <summary>
        /// Returns a snapshot of the underlying collection's elements.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get { return m_collection.ToArray(); }
        }

    }
}
