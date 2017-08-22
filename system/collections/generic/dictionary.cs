// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  Dictionary
** 
** <OWNER>[....]</OWNER>
**
** Purpose: Generic hash table implementation
**
** #DictionaryVersusHashtableThreadSafety
** Hashtable has multiple reader/single writer (MR/SW) thread safety built into 
** certain methods and properties, whereas Dictionary doesn't. If you're 
** converting framework code that formerly used Hashtable to Dictionary, it's
** important to consider whether callers may have taken a dependence on MR/SW
** thread safety. If a reader writer lock is available, then that may be used
** with a Dictionary to get the same thread safety guarantee. 
** 
** Reader writer locks don't exist in silverlight, so we do the following as a
** result of removing non-generic collections from silverlight: 
** 1. If the Hashtable was fully synchronized, then we replace it with a 
**    Dictionary with full locks around reads/writes (same thread safety
**    guarantee).
** 2. Otherwise, the Hashtable has the default MR/SW thread safety behavior, 
**    so we do one of the following on a case-by-case basis:
**    a. If the ---- can be addressed by rearranging the code and using a temp
**       variable (for example, it's only populated immediately after created)
**       then we address the ---- this way and use Dictionary.
**    b. If there's concern about degrading performance with the increased 
**       locking, we ifdef with FEATURE_NONGENERIC_COLLECTIONS so we can at 
**       least use Hashtable in the desktop build, but Dictionary with full 
**       locks in silverlight builds. Note that this is heavier locking than 
**       MR/SW, but this is the only option without rewriting (or adding back)
**       the reader writer lock. 
**    c. If there's no performance concern (e.g. debug-only code) we 
**       consistently replace Hashtable with Dictionary plus full locks to 
**       reduce complexity.
**    d. Most of serialization is dead code in silverlight. Instead of updating
**       those Hashtable occurences in serialization, we carved out references 
**       to serialization such that this code doesn't need to build in 
**       silverlight. 
===========================================================*/
namespace System.Collections.Generic {

    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    [DebuggerTypeProxy(typeof(Mscorlib_DictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class Dictionary<TKey,TValue>: IDictionary<TKey,TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, ISerializable, IDeserializationCallback  {
    
        /// <summary>
        /// 条目
        /// </summary>
        private struct Entry {
            public int hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int next;        // Index of next entry, -1 if last
            public TKey key;           // Key of entry
            public TValue value;         // Value of entry
        }

        /// <summary>
        /// 桶
        /// </summary>
        private int[] buckets;
        /// <summary>
        /// 条目数组
        /// </summary>
        private Entry[] entries;
        /// <summary>
        /// 数量
        /// </summary>
        private int count;
        /// <summary>
        /// 版本
        /// </summary>
        private int version;
        /// <summary>
        /// 释放队列
        /// </summary>
        private int freeList;
        /// <summary>
        /// 释放数量
        /// </summary>
        private int freeCount;
        /// <summary>
        /// 获取用于确定字典中的键是否相等的 IEqualityComparer<T>
        /// </summary>
        private IEqualityComparer<TKey> comparer;
        /// <summary>
        /// 键的集合
        /// </summary>
        private KeyCollection keys;
        /// <summary>
        /// 值的集合
        /// </summary>
        private ValueCollection values;
        /// <summary>
        /// 同步锁对象
        /// </summary>
        private Object _syncRoot;
        
        // 为序列化的常数
        private const String VersionName = "Version";
        private const String HashSizeName = "HashSize";  //必须保存的桶长度
        private const String KeyValuePairsName = "KeyValuePairs";
        private const String ComparerName = "Comparer";

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例为空，具有默认的初始容量并为键类型使用默认的相等比较器。
        /// </summary>
        public Dictionary(): this(0, null) {}

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例为空，具有指定的初始容量并为键类型使用默认的相等比较器
        /// </summary>
        /// <param name="capacity"></param>
        public Dictionary(int capacity): this(capacity, null) {}

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例为空，具有默认的初始容量并使用指定的 IEqualityComparer<T>。
        /// </summary>
        /// <param name="comparer"></param>
        public Dictionary(IEqualityComparer<TKey> comparer): this(0, comparer) {}

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例为空，具有指定的初始容量并使用指定的 IEqualityComparer<T>。
        /// </summary>
        /// <param name="capacity">Dictionary<TKey, TValue> 可包含的初始元素数。</param>
        /// <param name="comparer">比较键时要使用的 IEqualityComparer<T> 实现，或者为 null，以便为键类型使用默认的 EqualityComparer<T>。</param>
        public Dictionary(int capacity, IEqualityComparer<TKey> comparer) {
            if (capacity < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            if (capacity > 0) Initialize(capacity);
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;//设置默认比较器

#if FEATURE_CORECLR
            if (HashHelpers.s_UseRandomizedStringHashing && comparer == EqualityComparer<string>.Default)
            {
                this.comparer = (IEqualityComparer<TKey>) NonRandomizedStringEqualityComparer.Default;
            }
#endif // FEATURE_CORECLR
        }

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例包含从指定的 IDictionary<TKey, TValue> 复制的元素并为键类型使用默认的相等比较器。
        /// </summary>
        /// <param name="dictionary"></param>
        public Dictionary(IDictionary<TKey,TValue> dictionary): this(dictionary, null) {}

        /// <summary>
        /// 初始化 Dictionary<TKey, TValue> 类的新实例，该实例包含从指定的 IDictionary<TKey, TValue> 中复制的元素并使用指定的 IEqualityComparer<T>。
        /// </summary>
        /// <param name="dictionary">IDictionary<TKey, TValue>，它的元素被复制到新 Dictionary<TKey, TValue>。</param>
        /// <param name="comparer">比较键时要使用的 IEqualityComparer<T> 实现，或者为 null，以便为键类型使用默认的 EqualityComparer<T>。</param>
        public Dictionary(IDictionary<TKey,TValue> dictionary, IEqualityComparer<TKey> comparer):
            this(dictionary != null? dictionary.Count: 0, comparer) //如果字典为空则设置为0，否则设置为字典总数
        {

            if( dictionary == null) {//如果字典为空则抛出字典参数为空异常
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            foreach (KeyValuePair<TKey,TValue> pair in dictionary) {//将原字典中的键值对添加到新的字典中
                Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 用序列化数据初始化 Dictionary<TKey, TValue> 类的新实例。
        /// </summary>
        /// <param name="info">一个 System.Runtime.Serialization.SerializationInfo 对象包含序列化 Dictionary<TKey, TValue> 所需的信息。</param>
        /// <param name="context">一个 System.Runtime.Serialization.StreamingContext 结构包含与 Dictionary<TKey, TValue> 关联的序列化流的源和目标。</param>
        protected Dictionary(SerializationInfo info, StreamingContext context) {
            //We can't do anything with the keys and values until the entire graph has been deserialized
            //and we have a resonable estimate that GetHashCode is not going to fail.  For the time being,
            //we'll just cache this.  The graph is not valid until OnDeserialization has been called.
            HashHelpers.SerializationInfoTable.Add(this, info);
        }
          
        /// <summary>
        /// 获取用于确定字典中的键是否相等的 IEqualityComparer<T>。
        /// </summary>
        public IEqualityComparer<TKey> Comparer {
            get {
                return comparer;                
            }               
        }
        
        /// <summary>
        /// 获取包含在 Dictionary<TKey, TValue> 中的键/值对的数目。
        /// </summary>
        public int Count {
            get { return count - freeCount; }
        }

        /// <summary>
        /// 获得一个包含 Dictionary<TKey, TValue> 中的键的集合。
        /// </summary>
        public KeyCollection Keys {
            get {
                Contract.Ensures(Contract.Result<KeyCollection>() != null);
                if (keys == null) keys = new KeyCollection(this);//如果为空，则根据键值对创建键集合
                return keys;
            }
        }

        /// <summary>
        /// 获取一个ICollection<TKey>集合接口
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys {
            get {                
                if (keys == null) keys = new KeyCollection(this);                
                return keys;//接口转换
            }
        }

        /// <summary>
        /// 获取一个IEnumerable<TKey>接口
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get {                
                if (keys == null) keys = new KeyCollection(this);                
                return keys;
            }
        }

        /// <summary>
        /// 获取键值对中值集合
        /// </summary>
        public ValueCollection Values {
            get {
                Contract.Ensures(Contract.Result<ValueCollection>() != null);
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        /// <summary>
        /// 获取键值对中ICollection<TValue>接口
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values {
            get {                
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get {                
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }

        /// <summary>
        /// 获取或设置与指定的键关联的值。
        /// </summary>
        /// <param name="key">指定的键</param>
        /// <returns>相关的值</returns>
        public TValue this[TKey key] {
            get {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;//如果i大于0，则获取条目中的值
                ThrowHelper.ThrowKeyNotFoundException();//抛出键为被发现
                return default(TValue);//解决方案是使用 default 关键字，此关键字对于引用类型会返回空，对于数值类型会返回零。对于结构，此关键字将返回初始化为零或空的每个结构成员，具体取决于这些结构是值类型还是引用类型。
            }
            set {
                Insert(key, value, false);
            }
        }

        /// <summary>
        /// 将指定的键和值添加到字典中。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void Add(TKey key, TValue value) {
            Insert(key, value, true);
        }

        /// <summary>
        /// 显示接口实现add
        /// </summary>
        /// <param name="keyValuePair">键值对</param>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// 显示接口实现contains
        /// </summary>
        /// <param name="keyValuePair">查看是否包含的键值对</param>
        /// <returns>如果相同返回true，如果不同则返回false</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);//键包含
            if( i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {//值相同
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除集中的键值对
        /// </summary>
        /// <param name="keyValuePair">要移除的键值对</param>
        /// <returns>移除成功则返回为true</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair) {
            int i = FindEntry(keyValuePair.Key);
            if( i >= 0 && EqualityComparer<TValue>.Default.Equals(entries[i].value, keyValuePair.Value)) {
                Remove(keyValuePair.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 将字典中的内容清除
        /// </summary>
        public void Clear() {
            if (count > 0) {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }

        /// <summary>
        /// 确定是否 Dictionary<TKey, TValue> 包含指定键。
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public bool ContainsKey(TKey key) {
            return FindEntry(key) >= 0;
        }

        /// <summary>
        /// 确定 Dictionary<TKey, TValue> 是否包含特定值。
        /// </summary>
        /// <param name="value">要在 Dictionary<TKey, TValue> 中定位的值。对于引用类型，该值可以为 null</param>
        /// <returns></returns>
        public bool ContainsValue(TValue value) {
            if (value == null) {//如果值为空，则进行此搜索
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) return true;
                }
            }
            else {
                EqualityComparer<TValue> c = EqualityComparer<TValue>.Default;//使用一个默认的比较器
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;//如果相同则返回为true
                }
            }
            return false;
        }

        /// <summary>
        /// 将字典的索引处开始复制到键值对数组中
        /// </summary>
        /// <param name="array">键值对数组</param>
        /// <param name="index">开始索引</param>
        private void CopyTo(KeyValuePair<TKey,TValue>[] array, int index) {
            if (array == null) {//如果数组为空，则抛出异常
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }
            
            if (index < 0 || index > array.Length ) {//索引越界抛出异常
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count) {//数组大小越界
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            int count = this.count;
            Entry[] entries = this.entries;//创建新的条目引用，用于数组复制
            for (int i = 0; i < count; i++) {
                if (entries[i].hashCode >= 0) {
                    array[index++] = new KeyValuePair<TKey,TValue>(entries[i].key, entries[i].value);
                }
            }
        }

        /// <summary>
        /// 返回循环访问 Dictionary<TKey, TValue> 的枚举数。
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        /// <summary>
        /// 返回 IDictionaryEnumerator 的 IDictionary。
        /// </summary>
        /// <returns></returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }        


        /// <summary>
        /// 实现 System.Runtime.Serialization.ISerializable 接口，并返回序列化 Dictionary<TKey, TValue> 实例所需的数据。
        /// </summary>
        /// <param name="info">System.Runtime.Serialization.SerializationInfo 对象，该对象包含序列化 Dictionary<TKey, TValue> 实例所需的信息。</param>
        /// <param name="context">一个 System.Runtime.Serialization.StreamingContext 结构，它包含与 Dictionary<TKey, TValue> 实例关联的序列化流的源和目标。</param>
        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }
            info.AddValue(VersionName, version);

#if FEATURE_RANDOMIZED_STRING_HASHING
            info.AddValue(ComparerName, HashHelpers.GetEqualityComparerForSerialization(comparer), typeof(IEqualityComparer<TKey>));
#else
            info.AddValue(ComparerName, comparer, typeof(IEqualityComparer<TKey>));
#endif

            info.AddValue(HashSizeName, buckets == null ? 0 : buckets.Length); //This is the length of the bucket array.
            if( buckets != null) {
                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
                CopyTo(array, 0);
                info.AddValue(KeyValuePairsName, array, typeof(KeyValuePair<TKey, TValue>[]));
            }
        }

        /// <summary>
        /// 根据集合中的键获取其条目
        /// </summary>
        /// <param name="key">指定的键</param>
        /// <returns>键的条目</returns>
        private int FindEntry(TKey key) {
            if( key == null) {//如果键为空，则抛出为参数为空异常
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (buckets != null) {//如果沃森桶不为空，则先获取其哈希值，
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next) {//根据哈希的遍历，返回其索引
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 初始化数字容量
        /// </summary>
        /// <param name="capacity">容量大小</param>
        private void Initialize(int capacity) {
            int size = HashHelpers.GetPrime(capacity);//通过hash值返回大小
            buckets = new int[size];//初始化桶数组
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;//将桶数组全部初始化为-1
            entries = new Entry[size];//初始化条目数组
            freeList = -1;
        }

        /// <summary>
        /// 将一组键值插入键值对中
        /// </summary>
        /// <param name="key">插入的键</param>
        /// <param name="value">插入的值</param>
        /// <param name="add">是否允许再次添加相同的键</param>
        private void Insert(TKey key, TValue value, bool add) {
        
            if( key == null ) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (buckets == null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;//获取hashCode
            int targetBucket = hashCode % buckets.Length;//获取目标的沃森桶

#if FEATURE_RANDOMIZED_STRING_HASHING
            int collisionCount = 0;//设置碰撞数量
#endif

            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next) {//进行哈希遍历
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {
                    if (add) { //如果允许添加，则抛出参数异常
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
                    }
                    entries[i].value = value;//设置值
                    version++;
                    return;//函数返回
                } 

#if FEATURE_RANDOMIZED_STRING_HASHING
                collisionCount++;//碰撞数量加1
#endif
            }
            int index;
            if (freeCount > 0) {//如果释放数量大于0，则
                index = freeList;//设置索引
                freeList = entries[index].next;//定位freeList的当前位置
                freeCount--;//将free数量减一
            }
            else {//如果小于0
                if (count == entries.Length)//如果集合长度等于数量，则实现size扩充
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;//重新设置桶
                }
                index = count;//将索引设置为字典数量
                count++;
            }

            //设置条目索引处的信息
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;

#if FEATURE_RANDOMIZED_STRING_HASHING

#if FEATURE_CORECLR
            // In case we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
            // in this case will be EqualityComparer<string>.Default.
            // Note, randomized string hashing is turned on by default on coreclr so EqualityComparer<string>.Default will 
            // be using randomized string hashing

            if (collisionCount > HashHelpers.HashCollisionThreshold && comparer == NonRandomizedStringEqualityComparer.Default) 
            {
                comparer = (IEqualityComparer<TKey>) EqualityComparer<string>.Default;
                Resize(entries.Length, true);
            }
#else
            if(collisionCount > HashHelpers.HashCollisionThreshold && HashHelpers.IsWellKnownEqualityComparer(comparer)) 
            {
                comparer = (IEqualityComparer<TKey>) HashHelpers.GetRandomizedEqualityComparer(comparer);
                Resize(entries.Length, true);
            }
#endif // FEATURE_CORECLR

#endif

        }

        /// <summary>
        /// 实现 System.Runtime.Serialization.ISerializable 接口，并在完成反序列化之后引发反序列化事件。
        /// </summary>
        /// <param name="sender"></param>
        public virtual void OnDeserialization(Object sender) {
            SerializationInfo siInfo;
            HashHelpers.SerializationInfoTable.TryGetValue(this, out siInfo);
            
            if (siInfo==null) {
                // It might be necessary to call OnDeserialization from a container if the container object also implements
                // OnDeserialization. However, remoting will call OnDeserialization again.
                // We can return immediately if this function is called twice. 
                // Note we set remove the serialization info from the table at the end of this method.
                return;
            }            
            
            int realVersion = siInfo.GetInt32(VersionName);
            int hashsize = siInfo.GetInt32(HashSizeName);
            comparer   = (IEqualityComparer<TKey>)siInfo.GetValue(ComparerName, typeof(IEqualityComparer<TKey>));
            
            if( hashsize != 0) {
                buckets = new int[hashsize];
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                entries = new Entry[hashsize];
                freeList = -1;

                KeyValuePair<TKey, TValue>[] array = (KeyValuePair<TKey, TValue>[]) 
                    siInfo.GetValue(KeyValuePairsName, typeof(KeyValuePair<TKey, TValue>[]));

                if (array==null) {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                for (int i=0; i<array.Length; i++) {
                    if ( array[i].Key == null) {
                        ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_NullKey);
                    }
                    Insert(array[i].Key, array[i].Value, true);
                }
            }
            else {
                buckets = null;
            }

            version = realVersion;
            HashHelpers.SerializationInfoTable.Remove(this);
        }

        /// <summary>
        /// 重新设置size大小
        /// </summary>
        private void Resize() {
            Resize(HashHelpers.ExpandPrime(count), false);
        }

        /// <summary>
        /// 重新设置size
        /// </summary>
        /// <param name="newSize">新的大小</param>
        /// <param name="forceNewHashCodes">是否迫使产生新的HashCode</param>
        private void Resize(int newSize, bool forceNewHashCodes) {
            Contract.Assert(newSize >= entries.Length);
            int[] newBuckets = new int[newSize];//设置新的桶子
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;//设置新的桶子
            Entry[] newEntries = new Entry[newSize];//初始化新的条目数组
            Array.Copy(entries, 0, newEntries, 0, count);//实现数组的复制
            if(forceNewHashCodes) {//如果要生成新的hashcode
                for (int i = 0; i < count; i++) {//for循环
                    if(newEntries[i].hashCode != -1) {
                        newEntries[i].hashCode = (comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF);//设置每个条目的集合
                    }
                }
            }
            for (int i = 0; i < count; i++) {//设置桶子数组
                if (newEntries[i].hashCode >= 0) {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        /// <summary>
        /// 将带有指定键的值从 Dictionary<TKey, TValue> 中移除。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key) {
            if(key == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            if (buckets != null) {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;//获取hashcode
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next) {//遍历buckets
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) {//如果hashcode相同，并且键相同
                        if (last < 0) {
                            buckets[bucket] = entries[i].next;
                        }
                        else {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default(TKey);
                        entries[i].value = default(TValue);
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取与指定键关联的值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">要返回的值</param>
        /// <returns>是否包含</returns>
        public bool TryGetValue(TKey key, out TValue value) {
            int i = FindEntry(key);
            if (i >= 0) {
                value = entries[i].value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        // This is a convenience method for the internal callers that were converted from using Hashtable.
        // Many were combining key doesn't exist and key exists but null value (for non-value types) checks.
        // This allows them to continue getting that behavior with minimal code delta. This is basically
        // TryGetValue without the out param
        internal TValue GetValueOrDefault(TKey key) {
            int i = FindEntry(key);
            if (i >= 0) {
                return entries[i].value;//返回条目中的值
            }
            return default(TValue);//返回泛型中的默认构造值
        }

        /// <summary>
        /// 获取是否是只读
        /// </summary>
        bool ICollection<KeyValuePair<TKey,TValue>>.IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// 从特定的数组索引处开始，将 ICollection<T> 的元素复制到一个键值对数组中。
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection<KeyValuePair<TKey,TValue>>.CopyTo(KeyValuePair<TKey,TValue>[] array, int index) {
            CopyTo(array, index);
        }

        /// <summary>
        /// 从特定的数组索引处开始，将 ICollection<T> 的元素复制到一个数组中。
        /// </summary>
        /// <param name="array">一维数组，用作从 ICollection<T> 复制的元素的目标位置。该数组的索引必须从零开始。</param>
        /// <param name="index">array 中从零开始的索引，从此处开始复制。</param>
        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {//数组为空，抛出参数空异常
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }
            
            if (array.Rank != 1) {//数组维度不为1，抛出多维数组不支持
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if( array.GetLowerBound(0) != 0 ) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }
            
            if (index < 0 || index > array.Length) {//数组越界
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }
            
            KeyValuePair<TKey,TValue>[] pairs = array as KeyValuePair<TKey,TValue>[];//将数组转换为键值对
            if (pairs != null) {
                CopyTo(pairs, index);//键值对复制
            }
            else if( array is DictionaryEntry[]) {//如果数组是DictionaryEntry，则进行数组复制
                DictionaryEntry[] dictEntryArray = array as DictionaryEntry[];
                Entry[] entries = this.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) {
                        dictEntryArray[index++] = new DictionaryEntry(entries[i].key, entries[i].value);
                    }
                }                
            }
            else {//否则转换为object数组
                object[] objects = array as object[];
                if (objects == null) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }

                try {
                    int count = this.count;//将字典中的条目转换为KeyValuePair类型进行数组复制
                    Entry[] entries = this.entries;
                    for (int i = 0; i < count; i++) {
                        if (entries[i].hashCode >= 0) {
                            objects[index++] = new KeyValuePair<TKey,TValue>(entries[i].key, entries[i].value);
                        }
                    }
                }
                catch(ArrayTypeMismatchException) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                }
            }
        }

        /// <summary>
        /// 获取枚举元素的迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }
    
        /// <summary>
        /// 获取是否支持同步（不支持）
        /// </summary>
        bool ICollection.IsSynchronized {
            get { return false; }
        }

        /// <summary>
        /// 获取同步对象
        /// </summary>
        object ICollection.SyncRoot { 
            get { 
                if( _syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);    
                }
                return _syncRoot; 
            }
        }

        /// <summary>
        /// 显示接口实现
        /// 获取是否是固定大小的
        /// </summary>
        bool IDictionary.IsFixedSize {
            get { return false; }
        }

        /// <summary>
        /// 显示接口实现
        /// 获取是否是只读的
        /// </summary>
        bool IDictionary.IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// 显示接口实现
        /// 获取IDictionary.Keys集合
        /// </summary>
        ICollection IDictionary.Keys {
            get { return (ICollection)Keys; }
        }
        /// <summary>
        /// 显示接口实现
        /// 获取IDictionary.Values集合
        /// </summary>
        ICollection IDictionary.Values {
            get { return (ICollection)Values; }
        }
    
        /// <summary>
        /// 显示接口实现
        /// 获取或设置字典中的值
        /// </summary>
        /// <param name="key">字典中的值</param>
        /// <returns></returns>
        object IDictionary.this[object key] {
            get { 
                if( IsCompatibleKey(key)) { //先判断键是否兼容，返回集合中相应键的值              
                    int i = FindEntry((TKey)key);
                    if (i >= 0) { 
                        return entries[i].value;                
                    }
                }
                return null;
            }
            set { //设置相应键的值                
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);                          
                }
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

                try {
                    TKey tempKey = (TKey)key;
                    try {
                        this[tempKey] = (TValue)value; 
                    }
                    catch (InvalidCastException) { 
                        ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));   
                    }
                }
                catch (InvalidCastException) { 
                    ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
                }
            }
        }

        /// <summary>
        /// 判断键是否与集合中的键兼容
        /// </summary>
        /// <param name="key">匹配的键</param>
        /// <returns></returns>
        private static bool IsCompatibleKey(object key) {
            if( key == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);                          
                }
            return (key is TKey); 
        }
    
        /// <summary>
        /// 显示接口实现
        /// IDictionary.Add
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void IDictionary.Add(object key, object value) {            
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);                          
            }
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<TValue>(value, ExceptionArgument.value);

            try {
                TKey tempKey = (TKey)key;

                try {
                    Add(tempKey, (TValue)value);//由于是IDictionary接的实现，所以参数可能不一定符合，因此需要进行类型转换，如果转换失败，则抛出异常
                }
                catch (InvalidCastException) { 
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(TValue));   
                }
            }
            catch (InvalidCastException) { 
                ThrowHelper.ThrowWrongKeyTypeArgumentException(key, typeof(TKey));
            }
        }
    
        /// <summary>
        /// 显示接口实现
        /// IDictionary.Contains是否字典中包含对应键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IDictionary.Contains(object key) {    
            if(IsCompatibleKey(key)) {
                return ContainsKey((TKey)key);
            }
       
            return false;
        }
    
        IDictionaryEnumerator IDictionary.GetEnumerator() {
            return new Enumerator(this, Enumerator.DictEntry);
        }
    
        void IDictionary.Remove(object key) {            
            if(IsCompatibleKey(key)) {
                Remove((TKey)key);
            }
        }

        [Serializable]
        public struct Enumerator: IEnumerator<KeyValuePair<TKey,TValue>>,
            IDictionaryEnumerator
        {
            /// <summary>
            /// 键值对字典
            /// </summary>
            private Dictionary<TKey,TValue> dictionary;
            /// <summary>
            /// 版本
            /// </summary>
            private int version;
            /// <summary>
            /// 索引
            /// </summary>
            private int index;
            /// <summary>
            /// 当前键值对
            /// </summary>
            private KeyValuePair<TKey,TValue> current;
            private int getEnumeratorRetType;  // What should Enumerator.Current return?
            
            /// <summary>
            /// 字典条目
            /// </summary>
            internal const int DictEntry = 1;
            /// <summary>
            /// 键值对
            /// </summary>
            internal const int KeyValuePair = 2;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="dictionary">键值对字典</param>
            /// <param name="getEnumeratorRetType">枚举类型</param>
            internal Enumerator(Dictionary<TKey,TValue> dictionary, int getEnumeratorRetType) {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<TKey, TValue>();
            }

            /// <summary>
            /// 将枚举数推进到集合的下一个元素
            /// </summary>
            /// <returns>如果枚举数已成功地推进到下一个元素，则为 true；如果枚举数传递到集合的末尾，则为 false。</returns>
            public bool MoveNext() {
                if (version != dictionary.version) {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ((uint)index < (uint)dictionary.count) {//遍历数组
                    if (dictionary.entries[index].hashCode >= 0) {
                        current = new KeyValuePair<TKey, TValue>(dictionary.entries[index].key, dictionary.entries[index].value);//设置当前枚举对象
                        index++;
                        return true;
                    }
                    index++;
                }

                index = dictionary.count + 1;//如果索引位于数组末尾，则将当前枚举设置为无内容对象，返回为false
                current = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public KeyValuePair<TKey,TValue> Current {
                get { return current; }
            }

            public void Dispose() {
            }

            object IEnumerator.Current {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                    }      

                    if (getEnumeratorRetType == DictEntry) {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    } else {
                        return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    }
                }
            }

            void IEnumerator.Reset() {
                if (version != dictionary.version) {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                current = new KeyValuePair<TKey, TValue>();    
            }

            DictionaryEntry IDictionaryEnumerator.Entry {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                    }                        
                    
                    return new DictionaryEntry(current.Key, current.Value); 
                }
            }

            object IDictionaryEnumerator.Key {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                    }                        
                    
                    return current.Key; 
                }
            }

            object IDictionaryEnumerator.Value {
                get { 
                    if( index == 0 || (index == dictionary.count + 1)) {
                         ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                    }                        
                    
                    return current.Value; 
                }
            }
        }

        /// <summary>
        /// 键集合
        /// </summary>
        [DebuggerTypeProxy(typeof(Mscorlib_DictionaryKeyCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]        
        [Serializable]
        public sealed class KeyCollection: ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private Dictionary<TKey,TValue> dictionary;//字典键值对

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="dictionary">字典键值对</param>
            public KeyCollection(Dictionary<TKey,TValue> dictionary) {
                if (dictionary == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }
                this.dictionary = dictionary;
            }

            /// <summary>
            /// 获取迭代器
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);
            }

            /// <summary>
            /// 数组复制
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public void CopyTo(TKey[] array, int index) {
                if (array == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (index < 0 || index > array.Length) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }
                
                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<TKey>.IsReadOnly {
                get { return true; }
            }

            void ICollection<TKey>.Add(TKey item){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            }
            
            void ICollection<TKey>.Clear(){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
            }

            bool ICollection<TKey>.Contains(TKey item){
                return dictionary.ContainsKey(item);
            }

            bool ICollection<TKey>.Remove(TKey item){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_KeyCollectionSet);
                return false;
            }
            
            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array==null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if( array.GetLowerBound(0) != 0 ) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if (index < 0 || index > array.Length) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }
                
                TKey[] keys = array as TKey[];
                if (keys != null) {
                    CopyTo(keys, index);
                }
                else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }
                                         
                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    }                    
                    catch(ArrayTypeMismatchException) {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            Object ICollection.SyncRoot { 
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TKey>, System.Collections.IEnumerator
            {
                private Dictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TKey currentKey;
            
                internal Enumerator(Dictionary<TKey, TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default(TKey);                    
                }

                public void Dispose() {
                }

                public bool MoveNext() {
                    if (version != dictionary.version) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }

                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currentKey = default(TKey);
                    return false;
                }
                
                public TKey Current {
                    get {                        
                        return currentKey;
                    }
                }

                Object System.Collections.IEnumerator.Current {
                    get {                      
                        if( index == 0 || (index == dictionary.count + 1)) {
                             ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                        }                        
                        
                        return currentKey;
                    }
                }
                
                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);                        
                    }

                    index = 0;                    
                    currentKey = default(TKey);
                }
            }                        
        }

        [DebuggerTypeProxy(typeof(Mscorlib_DictionaryValueCollectionDebugView<,>))]
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection: ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private Dictionary<TKey,TValue> dictionary;

            public ValueCollection(Dictionary<TKey,TValue> dictionary) {
                if (dictionary == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            public void CopyTo(TValue[] array, int index) {
                if (array == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (index < 0 || index > array.Length) {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                }
                
                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++) {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].value;
                }
            }

            public int Count {
                get { return dictionary.Count; }
            }

            bool ICollection<TValue>.IsReadOnly {
                get { return true; }
            }

            void ICollection<TValue>.Add(TValue item){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            }

            bool ICollection<TValue>.Remove(TValue item){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
                return false;
            }

            void ICollection<TValue>.Clear(){
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ValueCollectionSet);
            }

            bool ICollection<TValue>.Contains(TValue item){
                return dictionary.ContainsValue(item);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return new Enumerator(dictionary);                
            }

            void ICollection.CopyTo(Array array, int index) {
                if (array == null) {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                }

                if (array.Rank != 1) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                }

                if( array.GetLowerBound(0) != 0 ) {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                }

                if (index < 0 || index > array.Length) { 
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                if (array.Length - index < dictionary.Count)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                
                TValue[] values = array as TValue[];
                if (values != null) {
                    CopyTo(values, index);
                }
                else {
                    object[] objects = array as object[];
                    if (objects == null) {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try {
                        for (int i = 0; i < count; i++) {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].value;
                        }
                    }
                    catch(ArrayTypeMismatchException) {
                        ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    }
                }
            }

            bool ICollection.IsSynchronized {
                get { return false; }
            }

            Object ICollection.SyncRoot { 
                get { return ((ICollection)dictionary).SyncRoot; }
            }

            [Serializable]
            public struct Enumerator : IEnumerator<TValue>, System.Collections.IEnumerator
            {
                private Dictionary<TKey, TValue> dictionary;
                private int index;
                private int version;
                private TValue currentValue;
            
                internal Enumerator(Dictionary<TKey, TValue> dictionary) {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default(TValue);
                }

                public void Dispose() {
                }

                public bool MoveNext() {                    
                    if (version != dictionary.version) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }
                    
                    while ((uint)index < (uint)dictionary.count) {
                        if (dictionary.entries[index].hashCode >= 0) {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default(TValue);
                    return false;
                }
                
                public TValue Current {
                    get {                        
                        return currentValue;
                    }
                }

                Object System.Collections.IEnumerator.Current {
                    get {                      
                        if( index == 0 || (index == dictionary.count + 1)) {
                             ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);                        
                        }                        
                        
                        return currentValue;
                    }
                }
                
                void System.Collections.IEnumerator.Reset() {
                    if (version != dictionary.version) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                    }
                    index = 0;                    
                    currentValue = default(TValue);
                }
            }
        }
    }
}
