// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// <OWNER>[....]</OWNER>
/*============================================================
**
** Class:   ConcurrentDictionary
**
**
** Purpose: A scalable dictionary for concurrent access
**
**
===========================================================*/

// If CDS_COMPILE_JUST_THIS symbol is defined, the ConcurrentDictionary.cs file compiles separately,
// with no dependencies other than .NET Framework 3.5.

//#define CDS_COMPILE_JUST_THIS

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Collections.ObjectModel;

#if !CDS_COMPILE_JUST_THIS
using System.Diagnostics.Contracts;
#endif

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Concurrent
{

    /// <summary>
    /// 表示可由多个线程同时访问的键/值对的线程安全集合
    /// </summary>
    /// <typeparam name="TKey">字典中的键的类型</typeparam>
    /// <typeparam name="TValue">字典中的值的类型</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentDictionary{TKey,TValue}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
#if !FEATURE_CORECLR
    [Serializable]
#endif
    [ComVisible(false)]
    [DebuggerTypeProxy(typeof(Mscorlib_DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Tables that hold the internal state of the ConcurrentDictionary
        /// Wrapping the three tables in a single object allows us to atomically
        /// replace all tables at once.
        /// Tables维持ConcurrentDictionary的内部状态
        /// 在一个单一对象中允许我们以原子操作替换所有的tables   包装三个tables
        /// </summary>
        private class Tables
        {
            internal readonly Node[] m_buckets; // 一个单一连接list为每一个bucket
            internal readonly object[] m_locks; // 一系列锁，每个都保护table的一段
            internal volatile int[] m_countPerLock; // The number of elements guarded by each lock.
            internal readonly IEqualityComparer<TKey> m_comparer; // 键相等比较器

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="buckets">节点数组</param>
            /// <param name="locks">锁数组</param>
            /// <param name="countPerLock">每个锁数目</param>
            /// <param name="comparer">比较器</param>
            internal Tables(Node[] buckets, object[] locks, int[] countPerLock, IEqualityComparer<TKey> comparer)
            {
                m_buckets = buckets;
                m_locks = locks;
                m_countPerLock = countPerLock;
                m_comparer = comparer;
            }
        }
#if !FEATURE_CORECLR
        [NonSerialized]
#endif
        private volatile Tables m_tables; // Internal tables of the dictionary       
        // NOTE: this is only used for compat reasons to serialize the comparer.
        // This should not be accessed from anywhere else outside of the serialization methods.
        internal IEqualityComparer<TKey> m_comparer; 
#if !FEATURE_CORECLR
        [NonSerialized]
#endif
        private readonly bool m_growLockArray; // Whether to dynamically increase the size of the striped lock

        // How many times we resized becaused of collisions. 
        // This is used to make sure we don't resize the dictionary because of multi-threaded Add() calls
        // that generate collisions. Whenever a GrowTable() should be the only place that changes this
#if !FEATURE_CORECLR
        // The field should be have been marked as NonSerialized but because we shipped it without that attribute in 4.5.1.
        // we can't add it back without breaking compat. To maximize compat we are going to keep the OptionalField attribute 
        // This will prevent（预防） cases（情况） where the field was not serialized.
        [OptionalField]
#endif
        private int m_keyRehashCount;//键重新处理数目

#if !FEATURE_CORECLR
        [NonSerialized]
#endif
        private int m_budget; // The maximum number of elements per lock before a resize operation is triggered 在一个重置大小的操作前，每个锁元素数量最大数将引发？？？？

#if !FEATURE_CORECLR // These fields are not used in CoreCLR
        private KeyValuePair<TKey, TValue>[] m_serializationArray; // 用于消费序列化数组

        private int m_serializationConcurrencyLevel; // 用于存储在序列化过程中的并行等级

        private int m_serializationCapacity; // 用于存储序列化容量
#endif
        // The default concurrency level is DEFAULT_CONCURRENCY_MULTIPLIER * #CPUs. The higher the
        // DEFAULT_CONCURRENCY_MULTIPLIER, the more concurrent writes can take place without interference
        // and blocking, but also the more expensive operations that require all locks become (e.g. table
        // resizing, ToArray, Count, etc). According to brief benchmarks that we ran, 4 seems like a good
        // compromise.
        private const int DEFAULT_CONCURRENCY_MULTIPLIER = 4;// 默认并发数

        // The default capacity, i.e. the initial # of buckets. When choosing this value, we are making
        // a trade-off between the size of a very small dictionary, and the number of resizes when
        // constructing a large dictionary. Also, the capacity should not be divisible by a small prime.
        private const int DEFAULT_CAPACITY = 31;//默认容量

        // The maximum size of the striped lock that will not be exceeded when locks are automatically
        // added as the dictionary grows. However, the user is allowed to exceed this limit by passing
        // a concurrency level larger than MAX_LOCK_NUMBER into the constructor.
        private const int MAX_LOCK_NUMBER = 1024;//最大锁数目

        // Whether TValue is a type that can be written atomically (i.e., with no danger of torn reads)
        private static readonly bool s_isValueWriteAtomic = IsValueWriteAtomic();//值是否是原子写入


        /// <summary>
        /// 确定格式TValue是否是原子写入
        /// </summary>
        private static bool IsValueWriteAtomic()
        {
            Type valueType = typeof(TValue);//获取TValue的格式

            //
            // Section 12.6.6 of ECMA CLI explains which types can be read and written atomically without
            // the risk of tearing.
            //
            // See http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-335.pdf
            //
            bool isAtomic =  // 判断是否是原子格式
                (valueType.IsClass)
                || valueType == typeof(Boolean)
                || valueType == typeof(Char)
                || valueType == typeof(Byte)
                || valueType == typeof(SByte)
                || valueType == typeof(Int16)
                || valueType == typeof(UInt16)
                || valueType == typeof(Int32)
                || valueType == typeof(UInt32)
                || valueType == typeof(Single);

            if (!isAtomic && IntPtr.Size == 8)//不明白
            {
                isAtomic |= valueType == typeof(Double) || valueType == typeof(Int64);
            }

            return isAtomic;
        }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例为空，
        /// 具有默认的并发级别和默认的初始容量，并为键类型使用默认比较器。
        /// </summary>
        public ConcurrentDictionary() : this(DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例为空，具有指定的并发级别和容量，并为键类型使用默认比较器。
        /// </summary>
        /// <param name="concurrencyLevel">将同时更新 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 的线程的估计数量。</param>
        /// <param name="capacity">System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 可包含的初始元素数。</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/>小于1</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="capacity"/>小于0</exception>
        public ConcurrentDictionary(int concurrencyLevel, int capacity) : this(concurrencyLevel, capacity, false, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例包含从指定的
        /// System.Collections.Generic.IEnumerable<T> 中复制的元素，具有默认的并发级别和默认的初始容量，并为键类型使用默认比较器。
        /// </summary>
        /// <param name="collection">System.Collections.Generic.IEnumerable<T>，
        /// 其元素被复制到新的 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>中。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more
        /// duplicate keys.</exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, EqualityComparer<TKey>.Default) { }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例为空，具有默认的并发级别和容量，并使用指定的
        ///     System.Collections.Generic.IEqualityComparer<T>。
        /// </summary>
        /// <param name="comparer">在比较键时要使用的相等比较实现。</param>
        /// <exception cref="T:System.ArgumentNullException">comparer 为 null。</exception>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer) : this(DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, comparer) { }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例包含从指定的
        ///     System.Collections.IEnumerable 中复制的元素，具有默认的并发级别和默认的初始容量，并使用指定的 System.Collections.Generic.IEqualityComparer<T>。
        /// </summary>
        /// <param name="collection">System.Collections.Generic.IEnumerable<T>，
        /// 其元素被复制到新的 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>中</param>
        /// <param name="comparer">在比较键时要使用的 System.Collections.Generic.IEqualityComparer<T> 实现。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic). -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(comparer)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例包含从指定的
        ///     System.Collections.IEnumerable 中复制的元素并使用指定的 System.Collections.Generic.IEqualityComparer<T>。
        /// </summary>
        /// <param name="concurrencyLevel">将同时更新 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>
        /// 的线程的估计数量。</param>
        /// <param name="collection">System.Collections.Generic.IEnumerable<T>，
        /// 其元素被复制到新的 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue></param>
        /// <param name="comparer">在比较键时要使用的 System.Collections.Generic.IEqualityComparer<T> 实现。</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="collection"/> is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1.
        /// </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        public ConcurrentDictionary(
            int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, DEFAULT_CAPACITY, false, comparer)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (comparer == null) throw new ArgumentNullException("comparer");

            InitializeFromCollection(collection);
        }

        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey,TValue>> collection)
        {
            TValue dummy;
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key == null) throw new ArgumentNullException("key");
                if (!TryAddInternal(pair.Key, pair.Value, false, false, out dummy))
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_SourceContainsDuplicateKeys"));
                }
            }

            if (m_budget == 0)
            {
                m_budget = m_tables.m_buckets.Length / m_tables.m_locks.Length;
            }
        }

        /// <summary>
        /// 初始化 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 类的新实例，该实例为空，具有指定的并发级别和指定的初始容量，并使用指定的
        ///     System.Collections.Generic.IEqualityComparer<T>。
        /// </summary>
        /// <param name="concurrencyLevel">将同时更新 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 的线程的估计数量。</param>
        /// <param name="capacity"> System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 可包含的初始元素数。</param>
        /// <param name="comparer">在比较键时要使用的 System.Collections.Generic.IEqualityComparer<T> 实现。</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1. -or-
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, capacity, false, comparer)
        {
        }

        internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey> comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException("concurrencyLevel", GetResource("ConcurrentDictionary_ConcurrencyLevelMustBePositive"));
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", GetResource("ConcurrentDictionary_CapacityMustNotBeNegative"));
            }
            if (comparer == null) throw new ArgumentNullException("comparer");

            // The capacity should be at least as large as the concurrency level. Otherwise, we would have locks that don't guard
            // any buckets.
            // 容量应该至少和concurrencyLevel一样长，否则，我们不保护任何桶
            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            object[] locks = new object[concurrencyLevel];//初始化locks数组
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i] = new object();
            }

            int[] countPerLock = new int[locks.Length];//每个锁数目
            Node[] buckets = new Node[capacity];//节点数组
            m_tables = new Tables(buckets, locks, countPerLock, comparer);//初始化tables结构

            m_growLockArray = growLockArray;
            m_budget = buckets.Length / locks.Length;
        }


        /// <summary>
        /// 尝试将指定的键和值添加到 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>中。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="value">要添加的元素的值。 对于引用类型，该值可以为 null。</param>
        /// <returns>如果该键/值对已成功添加到 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>，则为
        ///     true；如果该键已存在，则为 false。</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/>为 null。</exception>
        /// <exception cref="T:System.OverflowException">字典已包含最大数目的元素 (System.Int32.MaxValue)。</exception>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");
            TValue dummy;
            return TryAddInternal(key, value, false, true, out dummy);
        }

        /// <summary>
        /// 确定 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 是否包含指定的键。
        /// </summary>
        /// <param name="key">要在 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中定位的键。</param>
        /// <returns>如果 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 包含具有指定键的元素，则为
        ///     true；否则为 false。</returns>
        /// <exception cref="T:System.ArgumentNullException">key 为 null。</exception>
        public bool ContainsKey(TKey key)
        {
            if (key == null) throw new ArgumentNullException("key");

            TValue throwAwayValue;
            return TryGetValue(key, out throwAwayValue);
        }

        /// <summary>
        /// 尝试从 System.Collections.Concurrent.ConcurrentDictionary中移除并返回具有指定键的值。
        /// </summary>
        /// <param name="key">要移除并返回的元素的键。</param>
        /// <param name="value">W当此方法返回时，将包含从 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>
        ///     中移除的对象；如果 key 不存在，则包含 TValue 类型。</param>
        /// <returns>如果已成功移除对象，则为 true；否则为 false。</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/>为空</exception>
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");

            return TryRemoveInternal(key, out value, false, default(TValue));
        }

        /// <summary>
        /// Removes the specified key from the dictionary if it exists and returns its associated value.
        /// If matchValue flag is set, the key will be removed only if is associated with a particular
        /// value.
        /// 如果存在，则移除指定的键从字典中并返回与他相关联的值
        /// 如果matchValue标志是设置的，如果和一个特殊的值相关联，键将会被移除
        /// </summary>
        /// <param name="key">The key to search for and remove if it exists.</param>
        /// <param name="value">The variable into which the removed value, if found, is stored.</param>
        /// <param name="matchValue">Whether removal of the key is conditional on its value.</param>
        /// <param name="oldValue">The conditional value to compare against if <paramref name="matchValue"/> is true</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        private bool TryRemoveInternal(TKey key, out TValue value, bool matchValue, TValue oldValue)
        {
            while (true)
            {
                Tables tables = m_tables;

                IEqualityComparer<TKey> comparer = tables.m_comparer;

                int bucketNo, lockNo;
                GetBucketAndLockNo(comparer.GetHashCode(key), out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);

                lock (tables.m_locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurence.
                    if (tables != m_tables)
                    {
                        continue;
                    }

                    Node prev = null;
                    for (Node curr = tables.m_buckets[bucketNo]; curr != null; curr = curr.m_next)
                    {
                        Assert((prev == null && curr == tables.m_buckets[bucketNo]) || prev.m_next == curr);

                        if (comparer.Equals(curr.m_key, key))
                        {
                            if (matchValue)
                            {
                                bool valuesMatch = EqualityComparer<TValue>.Default.Equals(oldValue, curr.m_value);//可以删除与oldValue相同的值，不相同的话，会删除失败。
                                if (!valuesMatch)
                                {
                                    value = default(TValue);
                                    return false;
                                }
                            }

                            if (prev == null)
                            {
                                Volatile.Write<Node>(ref tables.m_buckets[bucketNo], curr.m_next);
                            }
                            else
                            {
                                prev.m_next = curr.m_next;
                            }

                            value = curr.m_value;
                            tables.m_countPerLock[lockNo]--;
                            return true;
                        }
                        prev = curr;
                    }
                }

                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// 尝试从ConcurrentDictionary 获取与指定的键关联的值。
        /// </summary>
        /// <param name="key">要获取的值的键。</param>
        /// <param name="value">当此方法返回时，将包含 System.Collections.Concurrent.ConcurrentDictionary
        ///     中具有指定键的对象；如果操作失败，则包含默认值。</param>
        /// <returns>如果在 System.Collections.Concurrent.ConcurrentDictionary中找到该键，则为
        ///     true；否则为 false。</returns>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");

            int bucketNo, lockNoUnused;

            // We must capture the m_buckets field in a local variable. It is set to a new table on each table resize.
            Tables tables = m_tables;
            IEqualityComparer<TKey> comparer = tables.m_comparer;
            GetBucketAndLockNo(comparer.GetHashCode(key), out bucketNo, out lockNoUnused, tables.m_buckets.Length, tables.m_locks.Length);//利用hashcode计算出节点位置

            // We can get away w/out a lock here.
            // Volatile.Read保证从buckets[i]在加载之前不会被被移动
            Node n = Volatile.Read<Node>(ref tables.m_buckets[bucketNo]);

            while (n != null)
            {
                if (comparer.Equals(n.m_key, key))//如果键相同，则返回值，否则，继续寻找
                {
                    value = n.m_value;
                    return true;
                }
                n = n.m_next;
            }

            value = default(TValue);//构造默认值
            return false;
        }

        /// <summary>
        /// 将指定键的现有值与指定值进行比较，如果相等，则用第三个值更新该键。
        /// </summary>
        /// <param name="key">其值将与 comparisonValue 进行比较并且可能被替换的键。</param>
        /// <param name="newValue">一个值，当比较结果相等时，将替换具有指定 key 的元素的值。</param>
        /// <param name="comparisonValue">与具有指定 key 的元素的值进行比较的值。</param>
        /// <returns>如果具有 key 的值与 comparisonValue 相等且替换为 newValue，则为 true；否则为 false。</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null) throw new ArgumentNullException("key");

            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            while (true)
            {
                int bucketNo;
                int lockNo;
                int hashcode;

                Tables tables = m_tables;
                IEqualityComparer<TKey> comparer = tables.m_comparer;

                hashcode = comparer.GetHashCode(key);
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);

                lock (tables.m_locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurence.
                    if (tables != m_tables)
                    {
                        continue;
                    }

                    // Try to find this key in the bucket
                    Node prev = null;
                    for (Node node = tables.m_buckets[bucketNo]; node != null; node = node.m_next)
                    {
                        Assert((prev == null && node == tables.m_buckets[bucketNo]) || prev.m_next == node);
                        if (comparer.Equals(node.m_key, key))
                        {
                            if (valueComparer.Equals(node.m_value, comparisonValue))
                            {
                                if (s_isValueWriteAtomic)
                                {
                                    node.m_value = newValue;
                                }
                                else
                                {
                                    Node newNode = new Node(node.m_key, newValue, hashcode, node.m_next);

                                    if (prev == null)
                                    {
                                        tables.m_buckets[bucketNo] = newNode;
                                    }
                                    else
                                    {
                                        prev.m_next = newNode;
                                    }
                                }

                                return true;
                            }

                            return false;
                        }

                        prev = node;
                    }

                    //didn't find the key
                    return false;
                }
            }
        }

        /// <summary>
        /// 从 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中移除所有的键和值。
        /// </summary>
        public void Clear()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);//设置排他锁

                // 将原m_tables的locks、comparer进行赋值，其他则进行重新构造
                Tables newTables = new Tables(new Node[DEFAULT_CAPACITY], m_tables.m_locks, new int[m_tables.m_countPerLock.Length], m_tables.m_comparer);
                m_tables = newTables;
                m_budget = Math.Max(1, newTables.m_buckets.Length / newTables.m_locks.Length);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);//释放排他锁
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection"/> to an array of
        /// type <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>, starting at the
        /// specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array of type <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// that is the destination of the <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/> elements copied from the <see
        /// cref="T:System.Collections.ICollection"/>. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (index < 0) throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = 0;

                for (int i = 0; i < m_tables.m_locks.Length && count >= 0; i++)//获取字典内的数量
                {
                    count += m_tables.m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0) // "count" 他自己 和"count + index" 是否越界
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                }

                CopyToPairs(array, index);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ConcurrentDictionary{TKey,TValue}"/> to a
        /// new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                int count = 0;
                checked
                {
                    for (int i = 0; i < m_tables.m_locks.Length; i++)//求出字典内的个数
                    {
                        count += m_tables.m_countPerLock[i];
                    }
                }

                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];//根据个数创建键值对数组

                CopyToPairs(array, 0);//将内容复制到键值对数组中
                return array;
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// 复制字典内容到一个数组，在ToArray和CopyTo中分享实现
        /// 重要：在调用CopyToPair之前，调用者必须加载全部锁
        /// </summary>
        /// <param name="array">要复制到的KeyValuePair<TKey, TValue>数组</param>
        private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++; //this should never flow, CopyToPairs is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// 复制字典内容到一个数组，在ToArray和CopyTo中分享实现
        /// 重要：在调用CopyToPair之前，调用者必须加载全部锁
        /// </summary>
        /// <param name="array">要复制到的DictionaryEntry数组</param>
        private void CopyToEntries(DictionaryEntry[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new DictionaryEntry(current.m_key, current.m_value);
                    index++;  //this should never flow, CopyToEntries is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// 复制字典内容到一个数组，在ToArray和CopyTo中分享实现
        /// 重要：在调用CopyToPair之前，调用者必须加载全部锁
        /// </summary>
        /// <param name="array">要复制到的object数组</param>
        private void CopyToObjects(object[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++; //this should never flow, CopyToObjects is only called when there's no overflow risk
                }
            }
        }

        /// <summary>返回一个枚举数迭代ConcurrentDictionary{TKey,TValue}</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Node[] buckets = m_tables.m_buckets;

            for (int i = 0; i < buckets.Length; i++)
            {
                // The Volatile.Read ensures that the load of the fields of 'current' doesn't move before the load from buckets[i].
                Node current = Volatile.Read<Node>(ref buckets[i]);

                while (current != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    current = current.m_next;
                }
            }
        }

        /// <summary>
        /// 分享内部对于插入和更新的实现
        /// 如果key存在，我们总是返回false，以及 如果updateIfExists == true 我们将聚焦于修改value
        /// 如可key不存在，我们将添加值并返回true
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        private bool TryAddInternal(TKey key, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
        {
            while (true)
            {
                int bucketNo, lockNo;
                int hashcode;

                Tables tables = m_tables;//获取当前tables
                IEqualityComparer<TKey> comparer = tables.m_comparer;//获取键比较器
                hashcode = comparer.GetHashCode(key);//获取要要插入键的hashcode
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);//获取bucketNo，lockNo

                bool resizeDesired = false;//渴望重置大小
                bool lockTaken = false;
#if FEATURE_RANDOMIZED_STRING_HASHING
#if !FEATURE_CORECLR                
                bool resizeDueToCollisions = false;
#endif // !FEATURE_CORECLR
#endif

                try
                {
                    if (acquireLock)//如果需要获取锁，
                        Monitor.Enter(tables.m_locks[lockNo], ref lockTaken);

                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurrence.
                    // 如果tables重新调整大小，我们可能没有保存正确的lock，必须重试
                    // 这应该是一种极其罕见的情况
                    if (tables != m_tables)
                    {
                        continue;
                    }

#if FEATURE_RANDOMIZED_STRING_HASHING
#if !FEATURE_CORECLR
                    int collisionCount = 0;
#endif // !FEATURE_CORECLR
#endif

                    // 尝试在桶中发现这个键
                    Node prev = null;
                    for (Node node = tables.m_buckets[bucketNo]; node != null; node = node.m_next)
                    {
                        Assert((prev == null && node == tables.m_buckets[bucketNo]) || prev.m_next == node);
                        if (comparer.Equals(node.m_key, key))//判断key是否存在
                        {
                            // The key was found in the dictionary. If updates are allowed, update the value for that key.
                            // We need to create a new node for the update, in order to support TValue types that cannot
                            // be written atomically, since lock-free reads may be happening concurrently.
                            if (updateIfExists)//如果存在判断是否更新
                            {
                                if (s_isValueWriteAtomic)//判断是否是原子写入
                                {
                                    node.m_value = value;
                                }
                                else
                                {
                                    Node newNode = new Node(node.m_key, value, hashcode, node.m_next);//创建新的节点，但是节点的键还是相同的，但是更改了值
                                    if (prev == null)
                                    {
                                        tables.m_buckets[bucketNo] = newNode;
                                    }
                                    else
                                    {
                                        prev.m_next = newNode;
                                    }
                                }
                                resultingValue = value;//如果允许更新则，返回更新后的值
                            }
                            else
                            {
                                resultingValue = node.m_value;//如果不允许更新，则返回当前节点的值
                            }
                            return false;
                        }
                        prev = node;//更新前驱节点

#if FEATURE_RANDOMIZED_STRING_HASHING
#if !FEATURE_CORECLR
                        collisionCount++;
#endif // !FEATURE_CORECLR
#endif
                    }

#if FEATURE_RANDOMIZED_STRING_HASHING
#if !FEATURE_CORECLR
                    if(collisionCount > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(comparer)) //通过冲突数目，判断是否更新相应变量
                    {
                        resizeDesired = true;
                        resizeDueToCollisions = true;
                    }
#endif // !FEATURE_CORECLR
#endif

                    // The key was not found in the bucket. Insert the key-value pair.
                    // 键为在桶内发现，插入键值对
                    Volatile.Write<Node>(ref tables.m_buckets[bucketNo], new Node(key, value, hashcode, tables.m_buckets[bucketNo]));
                    checked //
                    {
                        tables.m_countPerLock[lockNo]++;
                    }

                    //
                    // If the number of elements guarded by this lock has exceeded the budget, resize the bucket table.
                    // It is also possible that GrowTable will increase the budget but won't resize the bucket table.
                    // That happens if the bucket table is found to be poorly utilized due to a bad hash function.
                    //
                    if (tables.m_countPerLock[lockNo] > m_budget)
                    {
                        resizeDesired = true;
                    }
                }
                finally
                {
                    if (lockTaken)//如果获取到锁对象，则释放指定对象上的排他锁
                        Monitor.Exit(tables.m_locks[lockNo]);
                }

                //
                // The fact that we got here means that we just performed an insertion. If necessary, we will grow the table.
                // 事实上我们到这意味着我们仅仅执行了一个插入，如果是必要的，我们将增长表
                // Concurrency notes:
                // - Notice that we are not holding any locks at when calling GrowTable. This is necessary to prevent deadlocks.
                // - As a result, it is possible that GrowTable will be called unnecessarily. But, GrowTable will obtain lock 0
                //   and then verify that the table we passed to it as the argument is still the current table.
                // 并发节点：
                // - 通知我们将调用GrowTable的时候，将不持有任何锁。预防处理锁是必须的。
                // - 作为结果，GrowTable不被调用时可能的。但是，GrowTable？？？
                if (resizeDesired)
                {
#if FEATURE_RANDOMIZED_STRING_HASHING
#if !FEATURE_CORECLR
                    if (resizeDueToCollisions)
                    {
                        GrowTable(tables, (IEqualityComparer<TKey>)HashHelpers.GetRandomizedEqualityComparer(comparer), true, m_keyRehashCount);
                    }
                    else
#endif // !FEATURE_CORECLR
                    {
                        GrowTable(tables, tables.m_comparer, false, m_keyRehashCount);
                    }
#else
                    GrowTable(tables, tables.m_comparer, false, m_keyRehashCount);
#endif
                }

                resultingValue = value;
                return true;
            }
        }

        /// <summary>
        /// 获取或设置与指定的键相关联的值。
        /// </summary>
        /// <param name="key">要获取或设置的值的键。</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a
        /// <see cref="T:Sytem.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new
        /// element with the specified key.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        /// <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException();
                }
                return value;
            }
            set
            {
                if (key == null) throw new ArgumentNullException("key");
                TValue dummy;
                TryAddInternal(key, value, true, true, out dummy);
            }
        }

        /// <summary>
        /// 获取包含在 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中的键/值对的数目。
        /// </summary>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value> 包含在 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中的键/值对的数目。</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        public int Count
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
            get
            {
                int count = 0;

                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    // Compute the count, we allow overflow
                    for (int i = 0; i < m_tables.m_countPerLock.Length; i++)
                    {
                        count += m_tables.m_countPerLock[i];//将每个锁内的数目求和
                    }

                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }

                return count;
            }
        }

        /// <summary>
        /// 如果该键尚不存在，则使用指定函数将键/值对添加到 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="valueFactory">用于为键生成值的函数</param>
        /// <exception cref="T:System.ArgumentNullException">key 或 valueFactory 为 null。</exception>
        /// <exception cref="T:System.OverflowException">字典已包含最大数目的元素 (System.Int32.MaxValue)。</exception>
        /// <returns>键的值。 如果字典中已存在指定的键，则为该键的现有值；
        /// 如果字典中不存在指定的键，则为 valueFactory 返回的键的新值。</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (valueFactory == null) throw new ArgumentNullException("valueFactory");

            TValue resultingValue;
            if (TryGetValue(key, out resultingValue))
            {
                return resultingValue;
            }
            TryAddInternal(key, valueFactory(key), false, true, out resultingValue);
            return resultingValue;
        }

        /// <summary>
        /// 如果指定的键尚不存在，则将键/值对添加到 <see cref="ConcurrentDictionary{TKey,TValue}"/> 中。
        /// </summary>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="value">指定的键不存在时要添加的值</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the 
        /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");

            TValue resultingValue;
            TryAddInternal(key, value, false, true, out resultingValue);
            return resultingValue;
        }

        /// <summary>
        /// 如果该键尚不存在，则使用指定函数将键/值对添加到 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>；如果该键已存在，则使用该函数更新
        ///     System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中的键/值对。
        /// </summary>
        /// <param name="key">要添加的键或应更新其值的键</param>
        /// <param name="addValueFactory">用于为空缺键生成值的函数</param>
        /// <param name="updateValueFactory">用于根据现有键的现有值为键生成新值的函数</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>键的新值。 这将是 addValueFactory 的结果（如果缺少键）或 updateValueFactory 的结果（如果存在键）。</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (addValueFactory == null) throw new ArgumentNullException("addValueFactory");
            if (updateValueFactory == null) throw new ArgumentNullException("updateValueFactory");

            TValue newValue, resultingValue;
            while (true)
            {
                TValue oldValue;
                if (TryGetValue(key, out oldValue))
                //键存在，尝试去更新
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else //尝试添加
                {
                    newValue = addValueFactory(key);
                    if (TryAddInternal(key, newValue, false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        /// <summary>
        /// 如果该键尚不存在，则将键/值对添加到 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue>；如果该键已存在，则使用指定函数更新
        ///     System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 中的键/值对。
        /// </summary>
        /// <param name="key">要添加的键或应更新其值的键</param>
        /// <param name="addValue">要为空缺键添加的值</param>
        /// <param name="updateValueFactory">用于根据现有键的现有值为键生成新值的函数</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>键的新值。 这将是 addValue 的结果（如果缺少键）或 updateValueFactory 的结果（如果存在键）。</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (updateValueFactory == null) throw new ArgumentNullException("updateValueFactory");
            TValue newValue, resultingValue;
            while (true)
            {
                TValue oldValue;
                if (TryGetValue(key, out oldValue))
                //key exists, try to update
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else //try add
                {
                    if (TryAddInternal(key, addValue, false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }



        /// <summary>
        /// 获取一个指示 System.Collections.Concurrent.ConcurrentDictionary是否为空的值。
        /// </summary>
        /// <value>如果 System.Collections.Concurrent.ConcurrentDictionary<TKey,TValue> 为空，
        /// 则为true；否则为 false。</value>
        public bool IsEmpty
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
            get
            {
                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    for (int i = 0; i < m_tables.m_countPerLock.Length; i++)
                    {
                        if (m_tables.m_countPerLock[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }

                return true;
            }
        }

        #region IDictionary<TKey,TValue> members

        /// <summary>
        /// Adds the specified key and value to the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// An element with the same key already exists in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</exception>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException(GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully remove; otherwise false. This method also returns
        /// false if
        /// <paramref name="key"/> was not found in the original <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            TValue throwAwayValue;
            return TryRemove(key, out throwAwayValue);
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TKey}"/> containing the keys in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TKey> Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TValue}"/> containing the values in
        /// the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TValue> Values
        {
            get { return GetValues(); }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IEnumerable{TValue}"/> containing the values
        /// in the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.IEnumerable{TValue}"/> containing the
        /// values in the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get { return GetValues(); }
        }
        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Adds the specified value to the <see cref="T:System.Collections.Generic.ICollection{TValue}"/>
        /// with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to add to the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="keyValuePair"/> of <paramref
        /// name="keyValuePair"/> is null.</exception>
        /// <exception cref="T:System.OverflowException">The <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/></exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>
        /// contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure to locate in the <see
        /// cref="T:System.Collections.Generic.ICollection{TValue}"/>.</param>
        /// <returns>true if the <paramref name="keyValuePair"/> is found in the <see
        /// cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>; otherwise, false.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TValue value;
            if (!TryGetValue(keyValuePair.Key, out value))
            {
                return false;
            }
            return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>, this property always returns
        /// false.</value>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes a key and value from the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to remove from the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the key and value represented by <paramref name="keyValuePair"/> is successfully
        /// found and removed; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">The Key property of <paramref
        /// name="keyValuePair"/> is a null reference (Nothing in Visual Basic).</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (keyValuePair.Key == null) throw new ArgumentNullException(GetResource("ConcurrentDictionary_ItemKeyIsNull"));

            TValue throwAwayValue;
            return TryRemoveInternal(keyValuePair.Key, out throwAwayValue, true, keyValuePair.Value);
        }

        #endregion

        #region IEnumerable Members

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ConcurrentDictionary<TKey, TValue>)this).GetEnumerator();
        }

        #endregion

        #region IDictionary Members

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key.</param>
        /// <param name="value">The object to use as the value.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="key"/> is of a type that is not assignable to the key type <typeparamref
        /// name="TKey"/> of the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>. -or-
        /// <paramref name="value"/> is of a type that is not assignable to <typeparamref name="TValue"/>,
        /// the type of values in the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// -or- A value with the same key already exists in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </exception>
        void IDictionary.Add(object key, object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (!(key is TKey)) throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));

            TValue typedValue;
            try
            {
                typedValue = (TValue)value;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
            }

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, typedValue);
        }

        /// <summary>
        /// Gets whether the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains an
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains
        /// an element with the specified key; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary.Contains(object key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return (key is TKey) && ((ConcurrentDictionary<TKey, TValue>)this).ContainsKey((TKey)key);
        }

        /// <summary>Provides an <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the
        /// <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a fixed size.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a
        /// fixed size; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</value>
        ICollection IDictionary.Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        void IDictionary.Remove(object key)
        {
            if (key == null) throw new ArgumentNullException("key");

            TValue throwAwayValue;
            if (key is TKey)
            {
                this.TryRemove((TKey)key, out throwAwayValue);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Values
        {
            get { return GetValues(); }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key, or a null reference (Nothing in Visual Basic)
        /// if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is
        /// not assignable to the key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException">
        /// A value is being assigned, and <paramref name="key"/> is of a type that is not assignable to the
        /// key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>. -or- A value is being
        /// assigned, and <paramref name="key"/> is of a type that is not assignable to the value type
        /// <typeparamref name="TValue"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>
        /// </exception>
        object IDictionary.this[object key]
        {
            get
            {
                if (key == null) throw new ArgumentNullException("key");

                TValue value;
                if (key is TKey && this.TryGetValue((TKey)key, out value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (key == null) throw new ArgumentNullException("key");

                if (!(key is TKey)) throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
                if (!(value is TValue)) throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));

                ((ConcurrentDictionary<TKey, TValue>)this)[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an array, starting
        /// at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from
        /// the <see cref="T:System.Collections.ICollection"/>. The array must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (index < 0) throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                Tables tables = m_tables;

                int count = 0;

                for (int i = 0; i < tables.m_locks.Length && count >= 0; i++)
                {
                    count += tables.m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0) //"count" itself or "count + index" can overflow
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                }

                // To be consistent with the behavior of ICollection.CopyTo() in Dictionary<TKey,TValue>,
                // we recognize three types of target arrays:
                //    - an array of KeyValuePair<TKey, TValue> structs
                //    - an array of DictionaryEntry structs
                //    - an array of objects

                KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
                if (pairs != null)
                {
                    CopyToPairs(pairs, index);
                    return;
                }

                DictionaryEntry[] entries = array as DictionaryEntry[];
                if (entries != null)
                {
                    CopyToEntries(entries, index);
                    return;
                }

                object[] objects = array as object[];
                if (objects != null)
                {
                    CopyToObjects(objects, index);
                    return;
                }

                throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayIncorrectType"), "array");
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// (thread safe); otherwise, false. For <see
        /// cref="T:System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
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

        #endregion

        /// <summary>
        /// Replaces the bucket table with a larger one. To prevent multiple threads from resizing the
        /// table as a result of ----s, the Tables instance that holds the table of buckets deemed too
        /// small is passed in as an argument to GrowTable(). GrowTable() obtains a lock, and then checks
        /// the Tables instance has been replaced in the meantime or not. 
        /// The <paramref name="rehashCount"/> will be used to ensure that we don't do two subsequent resizes
        /// because of a collision
        /// </summary>
        private void GrowTable(Tables tables, IEqualityComparer<TKey> newComparer, bool regenerateHashKeys, int rehashCount)
        {
            int locksAcquired = 0;
            try
            {
                // The thread that first obtains m_locks[0] will be the one doing the resize operation
                AcquireLocks(0, 1, ref locksAcquired);

                if (regenerateHashKeys && rehashCount == m_keyRehashCount)
                {
                    // This method is called with regenerateHashKeys==true when we detected 
                    // more than HashHelpers.HashCollisionThreshold collisions when adding a new element.
                    // In that case we are in the process of switching to another (randomized) comparer
                    // and we have to re-hash all the keys in the table.
                    // We are only going to do this if we did not just rehash the entire table while waiting for the lock
                    tables = m_tables;
                }
                else
                {
                    // If we don't require a regeneration of hash keys we want to make sure we don't do work when
                    // we don't have to
                    if (tables != m_tables)
                    {
                        // We assume that since the table reference is different, it was already resized (or the budget
                        // was adjusted). If we ever decide to do table shrinking, or replace the table for other reasons,
                        // we will have to revisit this logic.
                        return;
                    }

                    // Compute the (approx.) total size. Use an Int64 accumulation variable to avoid an overflow.
                    long approxCount = 0;
                    for (int i = 0; i < tables.m_countPerLock.Length; i++)
                    {
                        approxCount += tables.m_countPerLock[i];
                    }

                    //
                    // If the bucket array is too empty, double the budget instead of resizing the table
                    //
                    if (approxCount < tables.m_buckets.Length / 4)
                    {
                        m_budget = 2 * m_budget;
                        if (m_budget < 0)
                        {
                            m_budget = int.MaxValue;
                        }

                        return;
                    }
                }
                // Compute the new table size. We find the smallest integer larger than twice the previous table size, and not divisible by
                // 2,3,5 or 7. We can consider a different table-sizing policy in the future.
                int newLength = 0;
                bool maximizeTableSize = false;
                try
                {
                    checked
                    {
                        // Double the size of the buckets table and add one, so that we have an odd integer.
                        newLength = tables.m_buckets.Length * 2 + 1;

                        // Now, we only need to check odd integers, and find the first that is not divisible
                        // by 3, 5 or 7.
                        while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0)
                        {
                            newLength += 2;
                        }

                        Assert(newLength % 2 != 0);

                        if (newLength > Array.MaxArrayLength)
                        {
                            maximizeTableSize = true;
                        }
                    }
                }
                catch (OverflowException)
                {
                    maximizeTableSize = true;
                }

                if (maximizeTableSize)
                {
                    newLength = Array.MaxArrayLength;

                    // We want to make sure that GrowTable will not be called again, since table is at the maximum size.
                    // To achieve that, we set the budget to int.MaxValue.
                    //
                    // (There is one special case that would allow GrowTable() to be called in the future: 
                    // calling Clear() on the ConcurrentDictionary will shrink the table and lower the budget.)
                    m_budget = int.MaxValue;
                }

                // Now acquire all other locks for the table
                AcquireLocks(1, tables.m_locks.Length, ref locksAcquired);

                object[] newLocks = tables.m_locks;

                // Add more locks
                if (m_growLockArray && tables.m_locks.Length < MAX_LOCK_NUMBER)
                {
                    newLocks = new object[tables.m_locks.Length * 2];
                    Array.Copy(tables.m_locks, newLocks, tables.m_locks.Length);

                    for (int i = tables.m_locks.Length; i < newLocks.Length; i++)
                    {
                        newLocks[i] = new object();
                    }
                }

                Node[] newBuckets = new Node[newLength];
                int[] newCountPerLock = new int[newLocks.Length];

                // Copy all data into a new table, creating new nodes for all elements
                for (int i = 0; i < tables.m_buckets.Length; i++)
                {
                    Node current = tables.m_buckets[i];
                    while (current != null)
                    {
                        Node next = current.m_next;
                        int newBucketNo, newLockNo;
                        int nodeHashCode = current.m_hashcode;

                        if (regenerateHashKeys)
                        {
                            // Recompute the hash from the key
                            nodeHashCode = newComparer.GetHashCode(current.m_key);
                        }

                        GetBucketAndLockNo(nodeHashCode, out newBucketNo, out newLockNo, newBuckets.Length, newLocks.Length);

                        newBuckets[newBucketNo] = new Node(current.m_key, current.m_value, nodeHashCode, newBuckets[newBucketNo]);

                        checked
                        {
                            newCountPerLock[newLockNo]++;
                        }

                        current = next;
                    }
                }

                // If this resize regenerated the hashkeys, increment the count
                if (regenerateHashKeys)
                {
                    // We use unchecked here because we don't want to throw an exception if 
                    // an overflow happens
                    unchecked
                    {
                        m_keyRehashCount++;
                    }
                }

                // Adjust the budget
                m_budget = Math.Max(1, newBuckets.Length / newLocks.Length);

                // Replace tables with the new versions
                m_tables = new Tables(newBuckets, newLocks, newCountPerLock, newComparer);
            }
            finally
            {
                // Release all locks that we took earlier
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// 计算桶和一个特殊键的锁数目
        /// </summary>
        private void GetBucketAndLockNo(
                int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount)
        {
            bucketNo = (hashcode & 0x7fffffff) % bucketCount;//计算bucketNo
            lockNo = bucketNo % lockCount;//计算lockNo

            Assert(bucketNo >= 0 && bucketNo < bucketCount);
            Assert(lockNo >= 0 && lockNo < lockCount);
        }

        /// <summary>
        /// The number of concurrent writes for which to optimize by default.
        /// </summary>
        private static int DefaultConcurrencyLevel
        {

            get { return DEFAULT_CONCURRENCY_MULTIPLIER * PlatformHelper.ProcessorCount; }
        }

        /// <summary>
        /// 请求对hashtable的所有锁，通过成功地获取锁的数目来增加lockAcquired。通过增加order来获取锁
        /// </summary>
        private void AcquireAllLocks(ref int locksAcquired)
        {
#if !FEATURE_PAL && !FEATURE_CORECLR    // PAL and CoreClr don't support  eventing
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(m_tables.m_buckets.Length);
            }
#endif //!FEATURE_PAL && !FEATURE_CORECLR

            // First, acquire lock 0
            // 首先获取lock 0
            AcquireLocks(0, 1, ref locksAcquired);

            // 现在我们已经有了lock 0， m_locks数组将不会进行改变
            // 之后，我们将可以安全的读取吗）locks.Length
            AcquireLocks(1, m_tables.m_locks.Length, ref locksAcquired);
            Assert(locksAcquired == m_tables.m_locks.Length);
        }

        /// <summary>
        /// 在hash table 获取一个连续范围的锁，通过成功地获取锁的数目来增加lockAcquired。通过增加order来获取锁
        /// </summary>
        private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
        {
            Assert(fromInclusive <= toExclusive);
            object[] locks = m_tables.m_locks;//获取锁对象数组

            for (int i = fromInclusive; i < toExclusive; i++)//对locks数组上的对象进行加锁设置
            {
                bool lockTaken = false;
                try
                {
#if CDS_COMPILE_JUST_THIS
                    Monitor.Enter(m_tables.m_locks[i]);
                    lockTaken = true;
#else
                    Monitor.Enter(locks[i], ref lockTaken);
#endif
                }
                finally
                {
                    if (lockTaken)//如果加锁成功，则自增locksAcquired
                    {
                        locksAcquired++;
                    }
                }
            }
        }

        /// <summary>
        /// 释放一个连续范围内的锁
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        private void ReleaseLocks(int fromInclusive, int toExclusive)
        {
            Assert(fromInclusive <= toExclusive);

            for (int i = fromInclusive; i < toExclusive; i++)//将m_locks数组内的对象锁进行释放
            {
                Monitor.Exit(m_tables.m_locks[i]);
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
        private ReadOnlyCollection<TKey> GetKeys()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TKey> keys = new List<TKey>();

                for (int i = 0; i < m_tables.m_buckets.Length; i++)
                {
                    Node current = m_tables.m_buckets[i];
                    while (current != null)
                    {
                        keys.Add(current.m_key);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TKey>(keys);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
        private ReadOnlyCollection<TValue> GetValues()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TValue> values = new List<TValue>();

                for (int i = 0; i < m_tables.m_buckets.Length; i++)
                {
                    Node current = m_tables.m_buckets[i];
                    while (current != null)
                    {
                        values.Add(current.m_value);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TValue>(values);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// 一个对于asserts的帮助方法
        /// </summary>
        [Conditional("DEBUG")]
        private void Assert(bool condition)
        {
#if CDS_COMPILE_JUST_THIS
            if (!condition)
            {
                throw new Exception("Assertion failed.");
            }
#else
            Contract.Assert(condition);
#endif
        }

        /// <summary>
        /// A helper function to obtain the string for a particular resource key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string GetResource(string key)
        {
            Assert(key != null);

#if CDS_COMPILE_JUST_THIS
            return key;
#else
            return Environment.GetResourceString(key);
#endif
        }

        /// <summary>
        /// 一个单链列表代表一个特殊hashtable桶中的节点
        /// </summary>
        private class Node
        {
            internal TKey m_key;//键
            internal TValue m_value;//值
            internal volatile Node m_next;//下一个节点
            internal int m_hashcode;//节点的hashcode

            /// <summary>
            /// 节点的构造函数
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="hashcode"></param>
            /// <param name="next"></param>
            internal Node(TKey key, TValue value, int hashcode, Node next)
            {
                m_key = key;
                m_value = value;
                m_next = next;
                m_hashcode = hashcode;
            }
        }

        /// <summary>
        /// A private class to represent enumeration over the dictionary that implements the 
        /// IDictionaryEnumerator interface.
        /// </summary>
        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator; // Enumerator over the dictionary.

            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                m_enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(m_enumerator.Current.Key, m_enumerator.Current.Value); }
            }

            public object Key
            {
                get { return m_enumerator.Current.Key; }
            }

            public object Value
            {
                get { return m_enumerator.Current.Value; }
            }

            public object Current
            {
                get { return this.Entry; }
            }

            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }

            public void Reset()
            {
                m_enumerator.Reset();
            }
        }

#if !FEATURE_CORECLR
        /// <summary>
        /// Get the data array to be serialized
        /// </summary>
        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            Tables tables = m_tables;

            // save the data into the serialization array to be saved
            m_serializationArray = ToArray();
            m_serializationConcurrencyLevel = tables.m_locks.Length;
            m_serializationCapacity = tables.m_buckets.Length;
            m_comparer = (IEqualityComparer<TKey>)HashHelpers.GetEqualityComparerForSerialization(tables.m_comparer);
        }

        /// <summary>
        /// Construct the dictionary from a previously serialized one
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            KeyValuePair<TKey, TValue>[] array = m_serializationArray;

            var buckets = new Node[m_serializationCapacity];
            var countPerLock = new int[m_serializationConcurrencyLevel];

            var locks = new object[m_serializationConcurrencyLevel];
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i] = new object();
            }

            m_tables = new Tables(buckets, locks, countPerLock, m_comparer);

            InitializeFromCollection(array);
            m_serializationArray = null;

        }
#endif
    }
}
