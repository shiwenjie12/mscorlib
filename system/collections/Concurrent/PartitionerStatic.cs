#pragma warning disable 0420
// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionerStatic.cs
//
// <OWNER>[....]</OWNER>
//
// A class of default partitioners for Partitioner<TSource>
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// 用默认行为集创建 Out-of-the-box 分区。
    /// 例如，默认情况下，缓冲区的某种形式和多级组块将用于完成 IEnumerable<T> 实现是快速、非阻塞常见方案的最佳性能。
    /// 使用 Partitioner.Create<TSource> 方法，这些行为可由此枚举重写。
    /// </summary>
    [Flags]
#if !FEATURE_CORECLR
    [Serializable]
#endif
    public enum EnumerablePartitionerOptions
    {
        /// <summary>
        /// 使用默认行为，就是使用缓冲实现最佳性能。
        /// </summary>
        None = 0x0,

        /// <summary>
        /// 创建分区程序，该程序从源可枚举项中一次提取项而不使用中间存储，多个线程可以更有效地访问该存储。
        /// 此选项支持低延迟（一从源获得项目就对其进行处理）且部分支持项目间的依赖项（线程无法死锁等待线程自身负责处理的项目）。
        /// </summary>
        NoBuffering = 0x1
    }

    // The static class Partitioners implements 3 default partitioning strategies:
    // 1. dynamic load balance partitioning for indexable data source (IList and arrays)
    // 2. static partitioning for indexable data source (IList and arrays)
    // 3. dynamic load balance partitioning for enumerables. Enumerables have indexes, which are the natural order
    //    of elements, but enuemrators are not indexable 
    // - data source of type IList/arrays have both dynamic and static partitioning, as 1 and 3.
    //   We assume that the source data of IList/Array is not changing concurrently.
    // - data source of type IEnumerable can only be partitioned dynamically (load-balance)
    // - Dynamic partitioning methods 1 and 3 are same, both being dynamic and load-balance. But the 
    //   implementation is different for data source of IList/Array vs. IEnumerable:
    //   * When the source collection is IList/Arrays, we use Interlocked on the shared index; 
    //   * When the source collection is IEnumerable, we use Monitor to wrap around the access to the source 
    //     enumerator.

    /// <summary>
    /// 提供针对数组、列表和可枚举项的常见分区策略。
    /// </summary>
    /// <remarks>
    /// <para>
    /// The static methods on <see cref="Partitioner"/> are all thread-safe and may be used concurrently
    /// from multiple threads. However, while a created partitioner is in use, the underlying data source
    /// should not be modified, whether from the same thread that's using a partitioner or from a separate
    /// thread.
    /// </para>
    /// </remarks>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public static class Partitioner
    {
        /// <summary>
        /// 从 IList<T> 实例创建一个可排序分区程序。
        /// </summary>
        /// <typeparam name="TSource">源列表中的元素的类型。</typeparam>
        /// <param name="list">要进行分区的列表。</param>
        /// <param name="loadBalance">
        /// 一个布尔值，该值指示创建的分区程序是否应在各分区之间保持动态负载平衡，而不是静态负载平衡。
        /// </param>
        /// <returns>
        /// 基于输入列表的可排序分区程序。
        /// </returns>
        public static OrderablePartitioner<TSource> Create<TSource>(IList<TSource> list, bool loadBalance)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            if (loadBalance)
            {
                return (new DynamicPartitionerForIList<TSource>(list));
            }
            else
            {
                return (new StaticIndexRangePartitionerForIList<TSource>(list));
            }
        }

        /// <summary>
        /// 从 Array 实例创建一个可排序分区程序。
        /// </summary>
        /// <typeparam name="TSource">源数组中的元素类型</typeparam>
        /// <param name="array">要进行分区的数组</param>
        /// <param name="loadBalance">
        /// 一个布尔值，该值指示创建的分区程序是否应在各分区之间保持动态负载平衡，而不是静态负载平衡。
        /// </param>
        /// <returns>
        /// 基于输入数组的可排序分区程序。
        /// </returns>
        public static OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance)
        {
            // This implementation uses 'ldelem' instructions for element retrieval, rather than using a
            // method call.

            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (loadBalance)
            {
                return (new DynamicPartitionerForArray<TSource>(array));
            }
            else
            {
                return (new StaticIndexRangePartitionerForArray<TSource>(array));
            }
        }

        /// <summary>
        /// 从 IEnumerable<T> 实例创建一个可排序分区程序。
        /// </summary>
        /// <typeparam name="TSource">源可枚举项中的元素的类型。</typeparam>
        /// <param name="source">要进行分区的可枚举项。</param>
        /// <returns>
        /// 基于输入数组的可排序分区程序。
        /// </returns>
        /// <remarks>
        /// 命令用于创建的分区是由元素的自然顺序从源可列举的检索
        /// </remarks>
        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source)
        {
            return Create<TSource>(source, EnumerablePartitionerOptions.None);
        }

        /// <summary>
        /// 从 IEnumerable<T> 实例创建一个可排序分区程序。
        /// </summary>
        /// <typeparam name="TSource">源可枚举项中的元素的类型。</typeparam>
        /// <param name="source">要进行分区的可枚举项。</param>
        /// <param name="partitionerOptions">控制分区缓冲行为的选项。</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// partitionerOptions 参数为 EnumerablePartitionerOptions 指定无效值
        /// </exception>
        /// <returns>
        /// An orderable partitioner based on the input array.
        /// 基于数组的可排列分区程序
        /// </returns>
        /// <remarks>
        /// The ordering used in the created partitioner is determined by the natural order of the elements 
        /// as retrieved from the source enumerable.
        /// </remarks>
        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if ((partitionerOptions & (~EnumerablePartitionerOptions.NoBuffering)) != 0)
                throw new ArgumentOutOfRangeException("partitionerOptions");

            return (new DynamicPartitionerForIEnumerable<TSource>(source, partitionerOptions));
        }

#if !PFX_LEGACY_3_5
        /// <summary>创建一个按用户指定的范围划分区块的分区程序。</summary>
        /// <param name="fromInclusive">范围下限（含）</param>
        /// <param name="toExclusive">范围上限（不含）</param>
        /// <returns>一个分区程序。</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">toExclusive 参数小于或等于 fromInclusive 参数。</exception>
        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive)
        {
            //我们想要多少块范围划分为?如果这是1,那么答案是“一块核心”。
            //一般来说,虽然,你会取得更好的负载平衡在一个繁忙的系统中,如果你使它高于1。
            int coreOversubscriptionRate = 3;//子范围大小

            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException("toExclusive");
            long rangeSize = (toExclusive - fromInclusive) /
                (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0) rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        /// <summary>创建一个按用户指定的范围划分区块的分区程序。</summary>
        /// <param name="fromInclusive">范围下限（含）。</param>
        /// <param name="toExclusive">范围上限（不含）。</param>
        /// <param name="rangeSize">每个子范围的大小。</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">toExclusive 参数小于或等于 fromInclusive 参数。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">rangeSize 参数小于或等于 0。</exception>
        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize)
        {
            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException("toExclusive");
            if (rangeSize <= 0) throw new ArgumentOutOfRangeException("rangeSize");
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        /// <summary>
        /// 包裹范围元组。
        /// </summary>
        /// <param name="fromInclusive">范围下限（含）</param>
        /// <param name="toExclusive">范围上限（不含）</param>
        /// <param name="rangeSize">每个子范围大小</param>
        /// <returns>返回一个二元组集合枚举</returns>
        private static IEnumerable<Tuple<long, long>> CreateRanges(long fromInclusive, long toExclusive, long rangeSize)
        {
            // Enumerate all of the ranges
            long from, to;
            bool shouldQuit = false;//是否应该退出

            for (long i = fromInclusive; (i < toExclusive) && !shouldQuit; i += rangeSize)
            {
                from = i;
                try { checked { to = i + rangeSize; } }//检查to是否越界
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }
                if (to > toExclusive) to = toExclusive;
                yield return new Tuple<long, long>(from, to);//迭代返回二元组
            }
        }

        /// <summary>创建一个按用户指定的范围划分区块的分区程序。</summary>
        /// <param name="fromInclusive">范围下限（含）。</param>
        /// <param name="toExclusive">范围上限（不含）。</param>
        /// <returns>一个分区程序。</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">toExclusive 参数小于或等于 fromInclusive 参数。</exception>
        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive)
        {
            // How many chunks do we want to divide the range into?  If this is 1, then the
            // answer is "one chunk per core".  Generally, though, you'll achieve better
            // load balancing on a busy system if you make it higher than 1.
            int coreOversubscriptionRate = 3;

            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException("toExclusive");
            int rangeSize = (toExclusive - fromInclusive) /
                (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0) rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        /// <summary>Creates a partitioner that chunks the user-specified range.</summary>
        /// <param name="fromInclusive">The lower, inclusive bound of the range.</param>
        /// <param name="toExclusive">The upper, exclusive bound of the range.</param>
        /// <param name="rangeSize">The size of each subrange.</param>
        /// <returns>A partitioner.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="toExclusive"/> argument is 
        /// less than or equal to the <paramref name="fromInclusive"/> argument.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> The <paramref name="rangeSize"/> argument is 
        /// less than or equal to 0.</exception>
        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize)
        {
            if (toExclusive <= fromInclusive) throw new ArgumentOutOfRangeException("toExclusive");
            if (rangeSize <= 0) throw new ArgumentOutOfRangeException("rangeSize");
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering); // chunk one range at a time
        }

        // Private method to parcel out range tuples.
        private static IEnumerable<Tuple<int, int>> CreateRanges(int fromInclusive, int toExclusive, int rangeSize)
        {
            // Enumerate all of the ranges
            int from, to;
            bool shouldQuit = false;

            for (int i = fromInclusive; (i < toExclusive) && !shouldQuit; i += rangeSize)
            {
                from = i;
                try { checked { to = i + rangeSize; } }
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }
                if (to > toExclusive) to = toExclusive;
                yield return new Tuple<int, int>(from, to);
            }
        }
#endif

        #region DynamicPartitionEnumerator_Abstract class
        /// <summary>
        /// DynamicPartitionEnumerator_Abstract定义枚举器为每个分区动态负载平衡分区算法。
        /// - KeyValuePairs分区是一个枚举器,每个对应一个项目的数据来源:关键是索引源集合中,价值是商品本身。
        /// - 一组这样的分区共享一个读者在数据源。TSourceReader指定的读者的类型。
        /// - 每个分区请求一个连续的块元素的一次从源数据。最初的块大小是1,双打每一次,直到达到最大的块大小。 
        ///   GrabNextChunk的实现()方法有两个版本:一个用于数据源IndexRange类型(IList和数组),一个用于IEnumerable的数据来源。 
        /// - “重置”方法不支持任何分区算法。
        /// - MoveNext()方法的实现是所有dynanmic分割者,所以我们提供它在这个抽象类。
        /// </summary>
        /// <typeparam name="TSource">类型的数据源中的元素</typeparam>
        /// <typeparam name="TSourceReader">类型的数据源上的读者</typeparam>
        //TSourceReader是 
        //  - IList<TSource>, when source data is IList<TSource>, the shared reader is source data itself
        //  - TSource[], when source data is TSource[], the shared reader is source data itself
        //  - IEnumerator<TSource>, when source data is IEnumerable<TSource>, and the shared reader is an 
        //    enumerator of the source data
        // IList < TSource >,当源数据IList < TSource >、共享的读者源数据本身TSource[],当源数据TSource[],共享读者源数据本身IEnumerator < TSource >,当源数据IEnumerable < TSource >,和共享的读者是一个枚举器的源数据
        private abstract class DynamicPartitionEnumerator_Abstract<TSource, TSourceReader> : IEnumerator<KeyValuePair<long, TSource>>
        {
            //----------------- common fields and constructor for all dynamic partitioners -----------------
            //-----为所有的动态分区共同字段和构造函数
            //--- shared by all dervied class with souce data type: IList, Array, and IEnumerator
            protected readonly TSourceReader m_sharedReader;

            /// <summary>
            /// 根据TSource的格式获取默认的最大块size
            /// </summary>
            protected static int s_defaultMaxChunkSize = GetDefaultChunkSize<TSource>();

            //deferred allocating in MoveNext() with initial value 0, to avoid false sharing
            //we also use the fact that: (m_currentChunkSize==null) means MoveNext is never called on this enumerator 
            /// <summary>
            /// 当前块的大小
            /// </summary>
            protected SharedInt m_currentChunkSize;

            //deferring allocation in MoveNext() with initial value -1, to avoid false sharing
            protected SharedInt m_localOffset;

            private const int CHUNK_DOUBLING_RATE = 3; // Double the chunk size every this many grabs
            private int m_doublingCountdown; // Number of grabs remaining until chunk size doubles
            protected readonly int m_maxChunkSize; // s_defaultMaxChunkSize unless single-chunking is requested by the caller

            // m_sharedIndex shared by this set of partitions, and particularly when m_sharedReader is IEnuerable
            // m_sharedIndex通过划分设置分享，特别是m_shanredReader是一个枚举
            // it serves as tracking of the natual order of elements in m_sharedReaderthe value of this field is passed in from outside (already initialized) by the constructor, 
            protected readonly SharedLong m_sharedIndex;

            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
                : this(sharedReader, sharedIndex, false)
            {
            }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="sharedReader">分享reader</param>
            /// <param name="sharedIndex">分享索引</param>
            /// <param name="useSingleChunking">是否使用默认最大块大小</param>
            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex, bool useSingleChunking)
            {
                m_sharedReader = sharedReader;
                m_sharedIndex = sharedIndex;
                m_maxChunkSize = useSingleChunking ? 1 : s_defaultMaxChunkSize;
            }

            // ---------------- abstract method declarations --------------

            /// <summary>
            /// Abstract method to request a contiguous chunk of elements from the source collection
            /// 虚方法去请求一个连续的元素从源集合中
            /// </summary>
            /// <param name="requestedChunkSize">请求的指定元素数</param>
            /// <returns>
            /// true if we successfully reserved at least one element
            ///     如果我们在最后一个元素中成功的保留 (up to #=requestedChunkSize) 
            /// false if all elements in the source collection have been reserved.
            ///     如果在元集合集合中的所有元素都被保留
            /// </returns>
            //GrabNextChunk does the following: 
            //  - grab # of requestedChunkSize elements from source data through shared reader, 
            //  - at the time of function returns, m_currentChunkSize is updated with the number of 
            //    elements actually got ----gined (<=requestedChunkSize). 
            //  - GrabNextChunk returns true if at least one element is assigned to this partition; 
            //    false if the shared reader already hits the last element of the source data before 
            //    we call GrabNextChunk
            protected abstract bool GrabNextChunk(int requestedChunkSize);

            /// <summary>
            /// Abstract property, returns whether or not the shared reader has already read the last 
            /// element of the source data 
            /// </summary>
            protected abstract bool HasNoElementsLeft { get; set; }

            /// <summary>
            /// Get the current element in the current partition. Property required by IEnumerator interface
            /// This property is abstract because the implementation is different depending on the type
            /// of the source data: IList, Array or IEnumerable
            /// </summary>
            public abstract KeyValuePair<long, TSource> Current { get; }

            /// <summary>
            /// Dispose is abstract, and depends on the type of the source data:
            /// - For source data type IList and Array, the type of the shared reader is just the dataitself.
            ///   We don't do anything in Dispose method for IList and Array. 
            /// - For source data type IEnumerable, the type of the shared reader is an enumerator we created.
            ///   Thus we need to dispose this shared reader enumerator, when there is no more active partitions
            ///   left.
            /// </summary>
            public abstract void Dispose();

            /// <summary>
            /// Reset on partitions is not supported
            /// </summary>
            public void Reset()
            {
                throw new NotSupportedException();
            }


            /// <summary>
            /// Get the current element in the current partition. Property required by IEnumerator interface
            /// </summary>
            Object IEnumerator.Current
            {
                get
                {
                    return ((DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>)this).Current;
                }
            }

            /// <summary>
            /// Moves to the next element if any.
            /// Try current chunk first, if the current chunk do not have any elements left, then we 
            /// attempt to grab a chunk from the source collection.
            /// </summary>
            /// <returns>
            /// true if successfully moving to the next position;
            /// false otherwise(如果), if and only if there is no more elements left in the current chunk 
            /// AND the source collection is exhausted（耗尽）. 
            /// </returns>
            public bool MoveNext()
            {
                // 执行延迟分配的局部变量
                if (m_localOffset == null)
                {
                    Contract.Assert(m_currentChunkSize == null);
                    m_localOffset = new SharedInt(-1);
                    m_currentChunkSize = new SharedInt(0);
                    m_doublingCountdown = CHUNK_DOUBLING_RATE;
                }

                if (m_localOffset.Value < m_currentChunkSize.Value - 1)
                //尝试从当前块去获取下一个元素
                {
                    m_localOffset.Value++;
                    return true;
                }
                else
                //otherwise it means we exhausted the local chunk
                //否则他意味着我们耗尽了当前块
                //grab a new chunk from the source enumerator
                //从源枚举中获取一个新的块
                {
                    // The second part of the || condition is necessary to handle the case when MoveNext() is called
                    // after a previous MoveNext call returned false.
                    Contract.Assert(m_localOffset.Value == m_currentChunkSize.Value - 1 || m_currentChunkSize.Value == 0);

                    //set the requested chunk size to a proper value
                    int requestedChunkSize;
                    if (m_currentChunkSize.Value == 0) //first time grabbing from source enumerator
                    {
                        requestedChunkSize = 1;
                    }
                    else if (m_doublingCountdown > 0)
                    {
                        requestedChunkSize = m_currentChunkSize.Value;
                    }
                    else
                    {
                        requestedChunkSize = Math.Min(m_currentChunkSize.Value * 2, m_maxChunkSize);//设置请求块大小
                        m_doublingCountdown = CHUNK_DOUBLING_RATE; // 将加倍数目重置
                    }

                    // 减去你的加倍数目
                    m_doublingCountdown--;

                    Contract.Assert(requestedChunkSize > 0 && requestedChunkSize <= m_maxChunkSize);
                    //GrabNextChunk will update the value of m_currentChunkSize
                    if (GrabNextChunk(requestedChunkSize))
                    {
                        Contract.Assert(m_currentChunkSize.Value <= requestedChunkSize && m_currentChunkSize.Value > 0);
                        m_localOffset.Value = 0;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        #endregion

        #region type为数据源进行动态分区的枚举格式
        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForIEnumerator defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForIEnumerable<TSource> : OrderablePartitioner<TSource>
        {
            IEnumerable<TSource> m_source;
            readonly bool m_useSingleChunking;

            //constructor
            internal DynamicPartitionerForIEnumerable(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
                : base(true, false, true)
            {
                m_source = source;
                m_useSingleChunking = ((partitionerOptions & EnumerablePartitionerOptions.NoBuffering) != 0);
            }

            /// <summary>
            /// 重载 OrderablePartitioner.GetOrderablePartitions.
            /// Partitions the underlying collection into the given number of orderable partitions.
            /// </summary>
            /// <param name="partitionCount">number of partitions requested</param>
            /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }
                IEnumerator<KeyValuePair<long, TSource>>[] partitions
                    = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];

                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = new InternalPartitionEnumerable(m_source.GetEnumerator(), m_useSingleChunking, true);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }
                return partitions;
            }

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderableDyanmicPartitions
            /// </summary>
            /// <returns>a enumerable collection of orderable partitions</returns>
            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return new InternalPartitionEnumerable(m_source.GetEnumerator(), m_useSingleChunking, false);
            }

            /// <summary>
            /// Whether additional partitions can be created dynamically.
            /// </summary>
            override public bool SupportsDynamicPartitions
            {
                get { return true; }
            }

            #region Internal classes:  InternalPartitionEnumerable, InternalPartitionEnumerator
            /// <summary>
            /// Provides customized implementation for source data of IEnumerable
            /// Different from the counterpart for IList/Array, this enumerable maintains several additional fields
            /// shared by the partitions it owns, including a boolean "m_hasNoElementsLef", a shared lock, and a 
            /// shared count "m_activePartitionCount" used to track active partitions when they were created statically
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IDisposable
            {
                //reader through which we access the source data
                private readonly IEnumerator<TSource> m_sharedReader;
                private SharedLong m_sharedIndex;//initial value -1

                private volatile KeyValuePair<long, TSource>[] m_FillBuffer;  // intermediate buffer to reduce locking
                private volatile int m_FillBufferSize;               // actual number of elements in m_FillBuffer. Will start
                                                                    // at m_FillBuffer.Length, and might be reduced during the last refill
                private volatile int m_FillBufferCurrentPosition;    //shared value to be accessed by Interlock.Increment only
                private volatile int m_activeCopiers;               //number of active copiers

                //fields shared by all partitions that this Enumerable owns, their allocation is deferred
                private SharedBool m_hasNoElementsLeft; // no elements left at all.
                private SharedBool m_sourceDepleted;    // no elements left in the enumerator, but there may be elements in the Fill Buffer

                //shared synchronization lock, created by this Enumerable
                private object m_sharedLock;//deferring allocation by enumerator

                private bool m_disposed;

                // If dynamic partitioning, then m_activePartitionCount == null
                // If static partitioning, then it keeps track of active partition count
                private SharedInt m_activePartitionCount;

                // records whether or not the user has requested single-chunking behavior
                private readonly bool m_useSingleChunking;

                internal InternalPartitionEnumerable(IEnumerator<TSource> sharedReader, bool useSingleChunking, bool isStaticPartitioning)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                    m_hasNoElementsLeft = new SharedBool(false);
                    m_sourceDepleted = new SharedBool(false);
                    m_sharedLock = new object();
                    m_useSingleChunking = useSingleChunking;

                    // Only allocate the fill-buffer if single-chunking is not in effect
                    if (!m_useSingleChunking)
                    {
                        // Time to allocate the fill buffer which is used to reduce the contention on the shared lock.
                        // First pick the buffer size multiplier. We use 4 for when there are more than 4 cores, and just 1 for below. This is based on empirical evidence.
                        int fillBufferMultiplier = (PlatformHelper.ProcessorCount > 4) ? 4 : 1;

                        // and allocate the fill buffer using these two numbers
                        m_FillBuffer = new KeyValuePair<long, TSource>[fillBufferMultiplier * Partitioner.GetDefaultChunkSize<TSource>()];
                    }

                    if (isStaticPartitioning)
                    {
                        // If this object is created for static partitioning (ie. via GetPartitions(int partitionCount), 
                        // GetOrderablePartitions(int partitionCount)), we track the active partitions, in order to dispose 
                        // this object when all the partitions have been disposed.
                        m_activePartitionCount = new SharedInt(0);
                    }
                    else
                    {
                        // Otherwise this object is created for dynamic partitioning (ie, via GetDynamicPartitions(),
                        // GetOrderableDynamicPartitions()), we do not need tracking. This object must be disposed
                        // explicitly
                        m_activePartitionCount = null;
                    }
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    if (m_disposed)
                    {
                        throw new ObjectDisposedException(Environment.GetResourceString("PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed"));
                    }
                    else
                    {
                        return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex,
                            m_hasNoElementsLeft, m_sharedLock, m_activePartitionCount, this, m_useSingleChunking);
                    }
                }


                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }


                ///////////////////
                //
                // Used by GrabChunk_Buffered()
                private void TryCopyFromFillBuffer(KeyValuePair<long, TSource>[] destArray, 
                                                  int requestedChunkSize, 
                                                  ref int actualNumElementsGrabbed)
                {                    
                    actualNumElementsGrabbed = 0;


                    // making a local defensive copy of the fill buffer reference, just in case it gets nulled out
                    KeyValuePair<long, TSource>[] fillBufferLocalRef = m_FillBuffer;
                    if (fillBufferLocalRef == null) return;

                    // first do a quick check, and give up if the current position is at the end
                    // so that we don't do an unncessary pair of Interlocked.Increment / Decrement calls
                    if (m_FillBufferCurrentPosition >= m_FillBufferSize)
                    {                        
                        return; // no elements in the buffer to copy from
                    }

                    // We might have a chance to grab elements from the buffer. We will know for sure 
                    // when we do the Interlocked.Add below. 
                    // But first we must register as a potential copier in order to make sure 
                    // the elements we're copying from don't get overwritten by another thread 
                    // that starts refilling the buffer right after our Interlocked.Add.
                    Interlocked.Increment(ref m_activeCopiers);

                    int endPos = Interlocked.Add(ref m_FillBufferCurrentPosition, requestedChunkSize);
                    int beginPos = endPos - requestedChunkSize;

                    if (beginPos < m_FillBufferSize)
                    {
                        // adjust index and do the actual copy
                        actualNumElementsGrabbed = (endPos < m_FillBufferSize) ? endPos : m_FillBufferSize - beginPos;
                        Array.Copy(fillBufferLocalRef, beginPos, destArray, 0, actualNumElementsGrabbed);
                    }

                    // let the record show we are no longer accessing the buffer
                    Interlocked.Decrement(ref m_activeCopiers);
                }

                /// <summary>
                /// This is the common entry point for consuming items from the source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved at least one element 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    actualNumElementsGrabbed = 0;

                    if (m_hasNoElementsLeft.Value)
                    {
                        return false;
                    }

                    if (m_useSingleChunking)
                    {
                        return GrabChunk_Single(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                    else
                    {
                        return GrabChunk_Buffered(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                }

                /// <summary>
                /// Version of GrabChunk that grabs a single element at a time from the source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved an element 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk_Single(KeyValuePair<long,TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Contract.Assert(m_useSingleChunking, "Expected m_useSingleChecking to be true");
                    Contract.Assert(requestedChunkSize == 1, "Got requested chunk size of " + requestedChunkSize + " when single-chunking was on");
                    Contract.Assert(actualNumElementsGrabbed == 0, "Expected actualNumElementsGrabbed == 0, instead it is " + actualNumElementsGrabbed);
                    Contract.Assert(destArray.Length == 1, "Expected destArray to be of length 1, instead its length is " + destArray.Length);

                    lock (m_sharedLock)
                    {
                        if (m_hasNoElementsLeft.Value) return false;

                        try
                        {
                            if (m_sharedReader.MoveNext())
                            {
                                m_sharedIndex.Value = checked(m_sharedIndex.Value + 1);
                                destArray[0]
                                    = new KeyValuePair<long, TSource>(m_sharedIndex.Value,
                                                                        m_sharedReader.Current);
                                actualNumElementsGrabbed = 1;
                                return true;
                            }
                            else
                            {
                                //if MoveNext() return false, we set the flag to inform other partitions
                                m_sourceDepleted.Value = true;
                                m_hasNoElementsLeft.Value = true;
                                return false;
                            }
                        }
                        catch
                        {
                            // On an exception, make sure that no additional items are hereafter enumerated
                            m_sourceDepleted.Value = true;
                            m_hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }
                }



                /// <summary>
                /// Version of GrabChunk that uses buffering scheme to grab items out of source enumerable
                /// </summary>
                /// <returns>
                /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                internal bool GrabChunk_Buffered(KeyValuePair<long,TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Contract.Assert(requestedChunkSize > 0);
                    Contract.Assert(!m_useSingleChunking, "Did not expect to be in single-chunking mode");

                    TryCopyFromFillBuffer(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    
                    if (actualNumElementsGrabbed == requestedChunkSize)
                    {
                        // that was easy.
                        return true;
                    }
                    else if (m_sourceDepleted.Value)
                    {
                        // looks like we both reached the end of the fill buffer, and the source was depleted previously
                        // this means no more work to do for any other worker
                        m_hasNoElementsLeft.Value = true;
                        m_FillBuffer = null;
                        return (actualNumElementsGrabbed > 0);
                    }


                    //
                    //  now's the time to take the shared lock and enumerate
                    //
                    lock (m_sharedLock)
                    {
                        if (m_sourceDepleted.Value)
                        {
                            return (actualNumElementsGrabbed > 0);
                        }
                        
                        try
                        {
                            // we need to make sure all array copiers are finished
                            if (m_activeCopiers > 0)
                            {                                    
                                SpinWait sw = new SpinWait();
                                while( m_activeCopiers > 0) sw.SpinOnce();
                            }

                            Contract.Assert(m_sharedIndex != null); //already been allocated in MoveNext() before calling GrabNextChunk

                            // Now's the time to actually enumerate the source

                            // We first fill up the requested # of elements in the caller's array
                            // continue from the where TryCopyFromFillBuffer() left off
                            for (; actualNumElementsGrabbed < requestedChunkSize; actualNumElementsGrabbed++)
                            {
                                if (m_sharedReader.MoveNext())
                                {
                                    m_sharedIndex.Value = checked(m_sharedIndex.Value + 1);
                                    destArray[actualNumElementsGrabbed]
                                        = new KeyValuePair<long, TSource>(m_sharedIndex.Value,
                                                                          m_sharedReader.Current);
                                }
                                else
                                {
                                    //if MoveNext() return false, we set the flag to inform other partitions
                                    m_sourceDepleted.Value = true;
                                    break;
                                }
                            }

                            // taking a local snapshot of m_FillBuffer in case some other thread decides to null out m_FillBuffer 
                            // in the entry of this method after observing m_sourceCompleted = true
                            var localFillBufferRef = m_FillBuffer;

                            // If the big buffer seems to be depleted, we will also fill that up while we are under the lock
                            // Note that this is the only place that m_FillBufferCurrentPosition can be reset
                            if (m_sourceDepleted.Value == false && localFillBufferRef != null && 
                                m_FillBufferCurrentPosition >= localFillBufferRef.Length)
                            {
                                for (int i = 0; i < localFillBufferRef.Length; i++)
                                {
                                    if( m_sharedReader.MoveNext())
                                    {
                                        m_sharedIndex.Value = checked(m_sharedIndex.Value + 1);
                                        localFillBufferRef[i]
                                            = new KeyValuePair<long, TSource>(m_sharedIndex.Value,
                                                                              m_sharedReader.Current);
                                    }
                                    else
                                    {
                                        // No more elements left in the enumerator.
                                        // Record this, so that the next request can skip the lock
                                        m_sourceDepleted.Value = true;

                                        // also record the current count in m_FillBufferSize
                                        m_FillBufferSize = i;

                                        // and exit the for loop so that we don't keep incrementing m_FillBufferSize
                                        break;
                                    }

                                }

                                m_FillBufferCurrentPosition = 0;
                            }


                        }
                        catch
                        {
                            // If an exception occurs, don't let the other enumerators try to enumerate.
                            // NOTE: this could instead throw an InvalidOperationException, but that would be unexpected 
                            // and not helpful to the end user.  We know the root cause is being communicated already.)
                            m_sourceDepleted.Value = true;
                            m_hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }

                    return (actualNumElementsGrabbed > 0);
                }

                public void Dispose()
                {
                    if (!m_disposed)
                    {
                        m_disposed = true;
                        m_sharedReader.Dispose();
                    }
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumerator_Abstract directly
            /// Provides customized implementation for: GrabNextChunk, HasNoElementsLeft, Current, Dispose
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumerator_Abstract<TSource, IEnumerator<TSource>>
            {
                //---- fields ----
                //cached local copy of the current chunk
                private KeyValuePair<long, TSource>[] m_localList; //defer allocating to avoid false sharing

                // the values of the following two fields are passed in from
                // outside(already initialized) by the constructor, 
                private readonly SharedBool m_hasNoElementsLeft;
                private readonly object m_sharedLock;
                private readonly SharedInt m_activePartitionCount;
                private InternalPartitionEnumerable m_enumerable;

                //constructor
                internal InternalPartitionEnumerator(
                    IEnumerator<TSource> sharedReader,
                    SharedLong sharedIndex,
                    SharedBool hasNoElementsLeft,
                    object sharedLock,
                    SharedInt activePartitionCount,
                    InternalPartitionEnumerable enumerable,
                    bool useSingleChunking)
                    : base(sharedReader, sharedIndex, useSingleChunking)
                {
                    m_hasNoElementsLeft = hasNoElementsLeft;
                    m_sharedLock = sharedLock;
                    m_enumerable = enumerable;
                    m_activePartitionCount = activePartitionCount;

                    if (m_activePartitionCount != null)
                    {
                        // If static partitioning, we need to increase the active partition count.
                        Interlocked.Increment(ref m_activePartitionCount.Value);
                    }
                }

                //overriding methods

                /// <summary>
                /// Reserves a contiguous range of elements from source data
                /// </summary>
                /// <param name="requestedChunkSize">specified number of elements requested</param>
                /// <returns>
                /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
                /// false if all elements in the source collection have been reserved.
                /// </returns>
                override protected bool GrabNextChunk(int requestedChunkSize)
                {
                    Contract.Assert(requestedChunkSize > 0);

                    if (HasNoElementsLeft)
                    {
                        return false;
                    }

                    // defer allocation to avoid false sharing
                    if (m_localList == null)
                    {
                        m_localList = new KeyValuePair<long, TSource>[m_maxChunkSize];
                    }

                    // make the actual call to the enumerable that grabs a chunk
                    return m_enumerable.GrabChunk(m_localList, requestedChunkSize, ref m_currentChunkSize.Value);
                }

                /// <summary>
                /// Returns whether or not the shared reader has already read the last 
                /// element of the source data 
                /// </summary>
                /// <remarks>
                /// We cannot call m_sharedReader.MoveNext(), to see if it hits the last element
                /// or not, because we can't undo MoveNext(). Thus we need to maintain a shared 
                /// boolean value m_hasNoElementsLeft across all partitions
                /// </remarks>
                override protected bool HasNoElementsLeft
                {
                    get { return m_hasNoElementsLeft.Value; }
                    set
                    {
                        //we only set it from false to true once
                        //we should never set it back in any circumstances
                        Contract.Assert(value);
                        Contract.Assert(!m_hasNoElementsLeft.Value);
                        m_hasNoElementsLeft.Value = true;
                    }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }
                        Contract.Assert(m_localList != null);
                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return (m_localList[m_localOffset.Value]);
                    }
                }

                override public void Dispose()
                {
                    // If this is static partitioning, ie. m_activePartitionCount != null, since the current partition 
                    // is disposed, we decrement the number of active partitions for the shared reader. 
                    if (m_activePartitionCount != null && Interlocked.Decrement(ref m_activePartitionCount.Value) == 0)
                    {
                        // If the number of active partitions becomes 0, we need to dispose the shared 
                        // reader we created in the m_enumerable object.
                        m_enumerable.Dispose();
                    }
                    // If this is dynamic partitioning, ie. m_activePartitionCount != null, then m_enumerable needs to
                    // be disposed explicitly by the user, and we do not need to anything here
                }
            }
            #endregion

        }
        #endregion

        #region Dynamic Partitioner for source data of IndexRange types (IList<> and Array<>)
        /// <summary>
        /// Dynamic load-balance partitioner. This class is abstract and to be derived from by 
        /// the customized partitioner classes for IList, Array, and IEnumerable
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the source data</typeparam>
        /// <typeparam name="TCollection"> Type of the source data collection</typeparam>
        private abstract class DynamicPartitionerForIndexRange_Abstract<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            // TCollection can be: IList<TSource>, TSource[] and IEnumerable<TSource>
            // Derived classes specify TCollection, and implement the abstract method GetOrderableDynamicPartitions_Factory accordingly
            TCollection m_data;

            /// <summary>
            /// Constructs a new orderable partitioner 
            /// </summary>
            /// <param name="data">source data collection</param>
            protected DynamicPartitionerForIndexRange_Abstract(TCollection data)
                : base(true, false, true)
            {
                m_data = data;
            }

            /// <summary>
            /// Partition the source data and create an enumerable over the resulting partitions. 
            /// </summary>
            /// <param name="data">the source data collection</param>
            /// <returns>an enumerable of partitions of </returns>
            protected abstract IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TCollection data);

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderablePartitions.
            /// Partitions the underlying collection into the given number of orderable partitions.
            /// </summary>
            /// <param name="partitionCount">number of partitions requested</param>
            /// <returns>A list containing <paramref name="partitionCount"/> enumerators.</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }
                IEnumerator<KeyValuePair<long, TSource>>[] partitions
                    = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = GetOrderableDynamicPartitions_Factory(m_data);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }
                return partitions;
            }

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderableDyanmicPartitions
            /// </summary>
            /// <returns>a enumerable collection of orderable partitions</returns>
            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return GetOrderableDynamicPartitions_Factory(m_data);
            }

            /// <summary>
            /// Whether additional partitions can be created dynamically.
            /// </summary>
            override public bool SupportsDynamicPartitions
            {
                get { return true; }
            }

        }

        /// <summary>
        /// Defines dynamic partition for source data of IList and Array. 
        /// This class inherits DynamicPartitionEnumerator_Abstract
        ///   - implements GrabNextChunk, HasNoElementsLeft, and Dispose methods for IList and Array
        ///   - Current property still remains abstract, implementation is different for IList and Array
        ///   - introduces another abstract method SourceCount, which returns the number of elements in
        ///     the source data. Implementation differs for IList and Array
        /// </summary>
        /// <typeparam name="TSource">Type of the elements in the data source</typeparam>
        /// <typeparam name="TSourceReader">Type of the reader on the source data</typeparam>
        private abstract class DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSourceReader> : DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>
        {
            //fields
            protected int m_startIndex; //initially zero

            //constructor
            protected DynamicPartitionEnumeratorForIndexRange_Abstract(TSourceReader sharedReader, SharedLong sharedIndex)
                : base(sharedReader, sharedIndex)
            {
            }

            //abstract methods
            //the Current property is still abstract, and will be implemented by derived classes
            //we add another abstract method SourceCount to get the number of elements from the source reader

            /// <summary>
            /// Get the number of elements from the source reader.
            /// It calls IList.Count or Array.Length
            /// </summary>
            protected abstract int SourceCount { get; }

            //overriding methods

            /// <summary>
            /// Reserves a contiguous range of elements from source data
            /// </summary>
            /// <param name="requestedChunkSize">specified number of elements requested</param>
            /// <returns>
            /// true if we successfully reserved at least one element (up to #=requestedChunkSize) 
            /// false if all elements in the source collection have been reserved.
            /// </returns>
            override protected bool GrabNextChunk(int requestedChunkSize)
            {
                Contract.Assert(requestedChunkSize > 0);

                while (!HasNoElementsLeft)
                {
                    Contract.Assert(m_sharedIndex != null);
                    // use the new Volatile.Read method because it is cheaper than Interlocked.Read on AMD64 architecture
                    long oldSharedIndex = Volatile.Read(ref m_sharedIndex.Value);

                    if (HasNoElementsLeft)
                    {
                        //HasNoElementsLeft situation changed from false to true immediately
                        //and oldSharedIndex becomes stale
                        return false;
                    }

                    //there won't be overflow, because the index of IList/array is int, and we 
                    //have casted it to long. 
                    long newSharedIndex = Math.Min(SourceCount - 1, oldSharedIndex + requestedChunkSize);


                    //the following CAS, if successful, reserves a chunk of elements [oldSharedIndex+1, newSharedIndex] 
                    //inclusive in the source collection
                    if (Interlocked.CompareExchange(ref m_sharedIndex.Value, newSharedIndex, oldSharedIndex)
                        == oldSharedIndex)
                    {
                        //set up local indexes.
                        //m_currentChunkSize is always set to requestedChunkSize when source data had 
                        //enough elements of what we requested
                        m_currentChunkSize.Value = (int)(newSharedIndex - oldSharedIndex);
                        m_localOffset.Value = -1;
                        m_startIndex = (int)(oldSharedIndex + 1);
                        return true;
                    }
                }
                //didn't get any element, return false;
                return false;
            }

            /// <summary>
            /// Returns whether or not the shared reader has already read the last 
            /// element of the source data 
            /// </summary>
            override protected bool HasNoElementsLeft
            {
                get
                {
                    Contract.Assert(m_sharedIndex != null);
                    // use the new Volatile.Read method because it is cheaper than Interlocked.Read on AMD64 architecture
                    return Volatile.Read(ref m_sharedIndex.Value) >= SourceCount - 1;
                }
                set
                {
                    Contract.Assert(false);
                }
            }

            /// <summary>
            /// For source data type IList and Array, the type of the shared reader is just the data itself.
            /// We don't do anything in Dispose method for IList and Array. 
            /// </summary>
            override public void Dispose()
            { }
        }


        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForIList defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForIList<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, IList<TSource>>
        {
            //constructor
            internal DynamicPartitionerForIList(IList<TSource> source)
                : base(source)
            { }

            //override methods
            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(IList<TSource> m_data)
            {
                //m_data itself serves as shared reader
                return new InternalPartitionEnumerable(m_data);
            }

            /// <summary>
            /// Inherits from PartitionList_Abstract 
            /// Provides customized implementation for source data of IList
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                //reader through which we access the source data
                private readonly IList<TSource> m_sharedReader;
                private SharedLong m_sharedIndex;

                internal InternalPartitionEnumerable(IList<TSource> sharedReader)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumeratorForIndexRange_Abstract
            /// Provides customized implementation of SourceCount property and Current property for IList
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, IList<TSource>>
            {
                //constructor
                internal InternalPartitionEnumerator(IList<TSource> sharedReader, SharedLong sharedIndex)
                    : base(sharedReader, sharedIndex)
                { }

                //overriding methods
                override protected int SourceCount
                {
                    get { return m_sharedReader.Count; }
                }
                /// <summary>
                /// return a KeyValuePair of the current element and its key 
                /// </summary>
                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }

                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(m_startIndex + m_localOffset.Value,
                            m_sharedReader[m_startIndex + m_localOffset.Value]);
                    }
                }
            }
        }



        /// <summary>
        /// Inherits from DynamicPartitioners
        /// Provides customized implementation of GetOrderableDynamicPartitions_Factory method, to return an instance
        /// of EnumerableOfPartitionsForArray defined internally
        /// </summary>
        /// <typeparam name="TSource">Type of elements in the source data</typeparam>
        private class DynamicPartitionerForArray<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, TSource[]>
        {
            //constructor
            internal DynamicPartitionerForArray(TSource[] source)
                : base(source)
            { }

            //override methods
            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TSource[] m_data)
            {
                return new InternalPartitionEnumerable(m_data);
            }

            /// <summary>
            /// Inherits from PartitionList_Abstract 
            /// Provides customized implementation for source data of Array
            /// </summary>
            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                //reader through which we access the source data
                private readonly TSource[] m_sharedReader;
                private SharedLong m_sharedIndex;

                internal InternalPartitionEnumerable(TSource[] sharedReader)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }


                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex);
                }
            }

            /// <summary>
            /// Inherits from DynamicPartitionEnumeratorForIndexRange_Abstract
            /// Provides customized implementation of SourceCount property and Current property for Array
            /// </summary>
            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSource[]>
            {
                //constructor
                internal InternalPartitionEnumerator(TSource[] sharedReader, SharedLong sharedIndex)
                    : base(sharedReader, sharedIndex)
                { }

                //overriding methods
                override protected int SourceCount
                {
                    get { return m_sharedReader.Length; }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        //verify that MoveNext is at least called once before Current is called 
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }

                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(m_startIndex + m_localOffset.Value,
                            m_sharedReader[m_startIndex + m_localOffset.Value]);
                    }
                }
            }
        }
        #endregion


        #region Static partitioning for IList and Array, abstract classes
        /// <summary>
        /// Static partitioning over IList. 
        /// - dynamic and load-balance
        /// - Keys are ordered within each partition
        /// - Keys are ordered across partitions
        /// - Keys are normalized
        /// - Number of partitions is fixed once specified, and the elements of the source data are 
        /// distributed to each partition as evenly as possible. 
        /// </summary>
        /// <typeparam name="TSource">type of the elements</typeparam>        
        /// <typeparam name="TCollection">Type of the source data collection</typeparam>
        private abstract class StaticIndexRangePartitioner<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            protected StaticIndexRangePartitioner()
                : base(true, true, true)
            { }

            /// <summary>
            /// Abstract method to return the number of elements in the source data
            /// </summary>
            protected abstract int SourceCount { get; }

            /// <summary>
            /// Abstract method to create a partition that covers a range over source data, 
            /// starting from "startIndex", ending at "endIndex"
            /// </summary>
            /// <param name="startIndex">start index of the current partition on the source data</param>
            /// <param name="endIndex">end index of the current partition on the source data</param>
            /// <returns>a partition enumerator over the specified range</returns>
            // The partitioning algorithm is implemented in GetOrderablePartitions method
            // This method delegates according to source data type IList/Array
            protected abstract IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex);

            /// <summary>
            /// Overrides OrderablePartitioner.GetOrderablePartitions
            /// Return a list of partitions, each of which enumerate a fixed part of the source data
            /// The elements of the source data are distributed to each partition as evenly as possible. 
            /// Specifically, if the total number of elements is N, and number of partitions is x, and N = a*x +b, 
            /// where a is the quotient, and b is the remainder. Then the first b partitions each has a + 1 elements,
            /// and the last x-b partitions each has a elements.
            /// For example, if N=10, x =3, then 
            ///    partition 0 ranges [0,3],
            ///    partition 1 ranges [4,6],
            ///    partition 2 ranges [7,9].
            /// This also takes care of the situation of (x&gt;N), the last x-N partitions are empty enumerators. 
            /// An empty enumerator is indicated by 
            ///      (m_startIndex == list.Count &amp;&amp; m_endIndex == list.Count -1)
            /// </summary>
            /// <param name="partitionCount">specified number of partitions</param>
            /// <returns>a list of partitions</returns>
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }

                int quotient, remainder;
                quotient = Math.DivRem(SourceCount, partitionCount, out remainder);

                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                int lastEndIndex = -1;
                for (int i = 0; i < partitionCount; i++)
                {
                    int startIndex = lastEndIndex + 1;

                    if (i < remainder)
                    {
                        lastEndIndex = startIndex + quotient;
                    }
                    else
                    {
                        lastEndIndex = startIndex + quotient - 1;
                    }
                    partitions[i] = CreatePartition(startIndex, lastEndIndex);
                }
                return partitions;
            }
        }

        /// <summary>
        /// Static Partition for IList/Array.
        /// This class implements all methods required by IEnumerator interface, except for the Current property.
        /// Current Property is different for IList and Array. Arrays calls 'ldelem' instructions for faster element 
        /// retrieval.
        /// </summary>
        //We assume the source collection is not being updated concurrently. Otherwise it will break the  
        //static partitioning, since each partition operates on the source collection directly, it does 
        //not have a local cache of the elements assigned to them.  
        private abstract class StaticIndexRangePartition<TSource> : IEnumerator<KeyValuePair<long, TSource>>
        {
            //the start and end position in the source collection for the current partition
            //the partition is empty if and only if 
            // (m_startIndex == m_data.Count && m_endIndex == m_data.Count-1)
            protected readonly int m_startIndex;
            protected readonly int m_endIndex;

            //the current index of the current partition while enumerating on the source collection
            protected volatile int m_offset;

            /// <summary>
            /// Constructs an instance of StaticIndexRangePartition
            /// </summary>
            /// <param name="startIndex">the start index in the source collection for the current partition </param>
            /// <param name="endIndex">the end index in the source collection for the current partition</param>
            protected StaticIndexRangePartition(int startIndex, int endIndex)
            {
                m_startIndex = startIndex;
                m_endIndex = endIndex;
                m_offset = startIndex - 1;
            }

            /// <summary>
            /// Current Property is different for IList and Array. Arrays calls 'ldelem' instructions for faster 
            /// element retrieval.
            /// </summary>
            public abstract KeyValuePair<long, TSource> Current { get; }

            /// <summary>
            /// We don't dispose the source for IList and array
            /// </summary>
            public void Dispose()
            { }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Moves to the next item
            /// Before the first MoveNext is called: m_offset == m_startIndex-1;
            /// </summary>
            /// <returns>true if successful, false if there is no item left</returns>
            public bool MoveNext()
            {
                if (m_offset < m_endIndex)
                {
                    m_offset++;
                    return true;
                }
                else
                {
                    //After we have enumerated over all elements, we set m_offset to m_endIndex +1.
                    //The reason we do this is, for an empty enumerator, we need to tell the Current 
                    //property whether MoveNext has been called or not. 
                    //For an empty enumerator, it starts with (m_offset == m_startIndex-1 == m_endIndex), 
                    //and we don't set a new value to m_offset, then the above condition will always be 
                    //true, and the Current property will mistakenly assume MoveNext is never called.
                    m_offset = m_endIndex + 1;
                    return false;
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return ((StaticIndexRangePartition<TSource>)this).Current;
                }
            }
        }
        #endregion

        #region Static partitioning for IList
        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        private class StaticIndexRangePartitionerForIList<TSource> : StaticIndexRangePartitioner<TSource, IList<TSource>>
        {
            IList<TSource> m_list;
            internal StaticIndexRangePartitionerForIList(IList<TSource> list)
                : base()
            {
                Contract.Assert(list != null);
                m_list = list;
            }
            override protected int SourceCount
            {
                get { return m_list.Count; }
            }
            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForIList<TSource>(m_list, startIndex, endIndex);
            }
        }

        /// <summary>
        /// Inherits from StaticIndexRangePartition
        /// Provides customized implementation of Current property
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        private class StaticIndexRangePartitionForIList<TSource> : StaticIndexRangePartition<TSource>
        {
            //the source collection shared by all partitions
            private volatile IList<TSource> m_list;

            internal StaticIndexRangePartitionForIList(IList<TSource> list, int startIndex, int endIndex)
                : base(startIndex, endIndex)
            {
                Contract.Assert(startIndex >= 0 && endIndex <= list.Count - 1);
                m_list = list;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    //verify that MoveNext is at least called once before Current is called 
                    if (m_offset < m_startIndex)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                    }

                    Contract.Assert(m_offset >= m_startIndex && m_offset <= m_endIndex);
                    return (new KeyValuePair<long, TSource>(m_offset, m_list[m_offset]));
                }
            }
        }
        #endregion

        #region static partitioning for Arrays
        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition for Array
        /// </summary>
        private class StaticIndexRangePartitionerForArray<TSource> : StaticIndexRangePartitioner<TSource, TSource[]>
        {
            TSource[] m_array;
            internal StaticIndexRangePartitionerForArray(TSource[] array)
                : base()
            {
                Contract.Assert(array != null);
                m_array = array;
            }
            override protected int SourceCount
            {
                get { return m_array.Length; }
            }
            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForArray<TSource>(m_array, startIndex, endIndex);
            }
        }

        /// <summary>
        /// Inherits from StaticIndexRangePartitioner
        /// Provides customized implementation of SourceCount and CreatePartition
        /// </summary>
        private class StaticIndexRangePartitionForArray<TSource> : StaticIndexRangePartition<TSource>
        {
            //the source collection shared by all partitions
            private volatile TSource[] m_array;

            internal StaticIndexRangePartitionForArray(TSource[] array, int startIndex, int endIndex)
                : base(startIndex, endIndex)
            {
                Contract.Assert(startIndex >= 0 && endIndex <= array.Length - 1);
                m_array = array;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    //verify that MoveNext is at least called once before Current is called 
                    if (m_offset < m_startIndex)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                    }

                    Contract.Assert(m_offset >= m_startIndex && m_offset <= m_endIndex);
                    return (new KeyValuePair<long, TSource>(m_offset, m_array[m_offset]));
                }
            }
        }
        #endregion


        #region 实用函数
        /// <summary>
        /// 一个非常简单的原始的,允许我们在多个线程之间共享一个值。(分享Int)
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        private class SharedInt
        {
            internal volatile int Value;

            internal SharedInt(int value)
            {
                this.Value = value;
            }

        }

        /// <summary>
        /// 一个非常简单的原始的,允许我们在多个线程之间共享一个值。(分享Bool)
        /// </summary>
        private class SharedBool
        {
            internal volatile bool Value;

            internal SharedBool(bool value)
            {
                this.Value = value;
            }

        }

        /// <summary>
        /// 一个非常简单的原始的，允许我们在多个线程之间共享一个值。（分享）
        /// </summary>
        private class SharedLong
        {
            internal long Value;
            internal SharedLong(long value)
            {
                this.Value = value;
            }

        }

        //--------------------
        // 根据part计算出默认的块的大小，他是从System.Linq.Parallel.Scheduling复制出来的，因为mscorlib.dll不能使用System.Linq.Parallel.Scheduling
        //--------------------

        // The number of bytes we want "chunks" to be, when partitioning, etc. We choose 4 cache
        // bytes的数目是我们想的，当分区的时候。我们选择4行缓存值，并假设128b的缓存行，大多数的存取是采用64b，
        // lines worth, assuming 128b cache line.  Most (popular) architectures(存取) use 64b cache lines,
        // but choosing 128b works for 64b too whereas a multiple of 64b isn't necessarily sufficient
        // 但是相较于64b选择128b,是鉴于64b不是必须的足够的
        // for 128b cache systems.  So 128b it is.
        private const int DEFAULT_BYTES_PER_CHUNK = 128 * 4;
        
        private static int GetDefaultChunkSize<TSource>()
        {
            int chunkSize;

            if (typeof(TSource).IsValueType)//如果TSource是值类型的话
            {
#if !FEATURE_CORECLR // Marshal.SizeOf is not supported in CoreCLR
                // @

                if (typeof(TSource).StructLayoutAttribute.Value == LayoutKind.Explicit)//如果TSource的每个成员在非托管内存中的精确位置被显式控制
                {
                    chunkSize = Math.Max(1, DEFAULT_BYTES_PER_CHUNK / Marshal.SizeOf(typeof(TSource)));
                }
                else
                {
                    // We choose '128' because this ensures, no matter the actual size of the value type,
                    // the total bytes used will be a multiple of 128. This ensures it's cache aligned.
                    chunkSize = 128;
                }
#else
                chunkSize = 128;
#endif
            }
            else//如果不是，则返回128 * 4 / IntPtr.Size(指针大小)
            {
                Contract.Assert((DEFAULT_BYTES_PER_CHUNK % IntPtr.Size) == 0, "bytes per chunk should be a multiple of pointer size");
                chunkSize = (DEFAULT_BYTES_PER_CHUNK / IntPtr.Size);
            }
            return chunkSize;
        }
        #endregion

    }
}
