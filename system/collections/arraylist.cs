// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  ArrayList
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: Implements a dynamically sized List as an array,
**          and provides many convenience methods for treating
**          an array as an IList.
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Runtime;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    // 实现一个适应可变列表,使用一个对象数组来存储元素。
    // 一个ArrayList能力,分配的内部数组的长度。
    // 元素被添加到一个ArrayList,ArrayList是自动增加的容量要求重新分配内部数组。
    /// <summary>
    /// 使用大小会根据需要动态增加的数组来实现 IList 接口。
    /// </summary>
#if FEATURE_CORECLR
    [FriendAccessAllowed]
#endif
    [DebuggerTypeProxy(typeof(System.Collections.ArrayList.ArrayListDebugView))]   
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ArrayList : IList, ICloneable
    {
        /// <summary>
        /// items对象数组
        /// </summary>
        private Object[] _items;
        /// <summary>
        /// 实际包含的元素数
        /// </summary>
        [ContractPublicPropertyName("Count")]
        private int _size;
        /// <summary>
        /// ArrayList对象版本
        /// </summary>
        private int _version;
        /// <summary>
        /// 同步锁（不序列化）
        /// </summary>
        [NonSerialized]
        private Object _syncRoot;
        
        /// <summary>
        /// 默认容量
        /// </summary>
        private const int _defaultCapacity = 4;
        /// <summary>
        /// 初始化空对象数组
        /// </summary>
        private static readonly Object[] emptyArray = EmptyArray<Object>.Value; 
    
        /// <summary>
        /// 注意:这个构造函数是一个虚假的构造函数,并没有与SyncArrayList仅供使用。
        /// </summary>
        /// <param name="trash"></param>
        internal ArrayList( bool trash )
        {
        }

        // 构造一个ArrayList。最初是空的,有能力为零。在第一个元素添加到列表的能力增加_defaultCapacity,然后根据需要增加两个的倍数。
        /// <summary>
        /// 初始化 ArrayList 类的新实例，该实例为空并且具有默认初始容量。
        /// </summary>
        public ArrayList() {
            _items = emptyArray;  
        }

        // 构造一个与给定的初始容量ArrayList。列表最初是空的,但会有空间给定数量的元素之前需要重新分配。
        /// <summary>
        /// 初始化 ArrayList 类的新实例，该实例为空并且具有指定的初始容量。
        /// </summary>
        /// <param name="capacity">初始容量</param>
         public ArrayList(int capacity) {
             if (capacity < 0) //如果初始容量小于零，则抛出ArgumentOutOfRangeException异常
                 throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "capacity"));
             Contract.EndContractBlock();

             if (capacity == 0)//如果初始容量为零，则设置为空数组
                 _items = emptyArray;
             else//初始化Object数组
                 _items = new Object[capacity];
        }

         // 构造一个ArrayList,复制给定集合的内容。新列表的大小和容量都将等于给定集合的大小。
        /// <summary>
         /// 初始化 ArrayList 类的新实例，该实例包含从指定集合复制的元素，具有与复制的元素数相同的初始容量。
        /// </summary>
        /// <param name="c"></param>
        public ArrayList(ICollection c) {
            if (c == null)//如果参数为空，则抛出ArgumentNullException异常
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            Contract.EndContractBlock();

            int count = c.Count;//获取集合的数量
            if (count == 0)//如果集合数量为零，则初始化为空数组
            {
                _items = emptyArray;
            }
            else {
                _items = new Object[count];//初始化Object数组
                AddRange(c);
            }
        }
    
        // 获取或设置这个列表的能力，能力的大小为内部数组用来保存的项目数量，设置时，内部列表的数组分配给给定的能力。
        /// <summary>
        /// 获取或设置 ArrayList 可包含的元素数。
        /// </summary>
         public virtual int Capacity {
            get {
                Contract.Ensures(Contract.Result<int>() >= Count);
                return _items.Length;
            }
            set {
                if (value < _size) {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }
                Contract.Ensures(Capacity >= 0);
                Contract.EndContractBlock();
                //我们不想更新版本号当我们改变的能力。一些现有的应用程序依赖于此。
                if (value != _items.Length) {//当值不等于当前值时，更新数组大小
                    if (value > 0) {//如果值大于0，则将内容数组更新
                        Object[] newItems = new Object[value];
                        if (_size > 0) { 
                            Array.Copy(_items, 0, newItems, 0, _size);//调用Array数组内容复制
                        }
                        _items = newItems;
                    }
                    else {//如果小于零，则将数字设置为长度为0的数字
                        _items = new Object[_defaultCapacity];
                    }
                }            
            }
        }

        /// <summary>
         /// 获取 ArrayList 中实际包含的元素数。
        /// </summary>
        public virtual int Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        /// <summary>
        /// 获取一个值，该值指示 ArrayList 是否具有固定大小。
        /// </summary>
        public virtual bool IsFixedSize {
            get { return false; }
        }

            
        /// <summary>
        /// 获取一个值，该值指示 ArrayList 是否为只读。
        /// </summary>
        public virtual bool IsReadOnly {
            get { return false; }
        }

        /// <summary>
        /// 获取一个值，该值指示是否同步对 ArrayList 的访问（线程安全）。
        /// </summary>
        public virtual bool IsSynchronized {
            get { return false; }
        }
    
        /// <summary>
        /// 获取可用于同步对 ArrayList 的访问的对象。
        /// </summary>
        public virtual Object SyncRoot {
            get { 
                if( _syncRoot == null) {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);    
                }
                return _syncRoot; 
            }
        }
    
        /// <summary>
        /// 获取或设置指定索引处的元素。
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns>当前索引出对象</returns>
 
        public virtual Object this[int index] {
            get {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                return _items[index];
            }
            set {
                if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();
                _items[index] = value;//更换索引出值
                _version++;//增加版本
            }
        }
    
        // 创建一个特定IList ArrayList包装。
        // 这并不复制IList的内容,但只有包装IList接口。
        // 所以对底层列表的任何更改将影响ArrayList。
        // 如果你想扭转IList的子范围,或想要使用一种通用BinarySearch或方法没有实现一个自己,这将是有用的。
        // 然而,由于这些方法是通用的,性能可能不是那么好操作就像IList本身。
        /// <summary>
        /// 创建一个特定IList ArrayList包装器。
        /// </summary>
        /// <param name="list">List接口</param>
        /// <returns></returns>
        public static ArrayList Adapter(IList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();
            return new IListWrapper(list);
        }
        
        /// <summary>
        /// 将给定对象添加到这个列表中。列表的大小增加1。如果需要,列表的能力是添加新元素之前翻了一倍。
        /// </summary>
        /// <param name="value">增加对象</param>
        /// <returns>返回实际包含大小</returns>
        public virtual int Add(Object value) {
            Contract.Ensures(Contract.Result<int>() >= 0);
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size] = value;
            _version++;
            return _size++;
        }

        //将给定集合的元素添加到此列表的结束。如果需要,列表的能力增加到两次之前的能力或新尺寸,哪个比较大。
        /// <summary>
        /// 添加 ICollection 的元素到 ArrayList 的末尾。
        /// </summary>
        /// <param name="c">集合</param>
        public virtual void AddRange(ICollection c) {
            InsertRange(_size, c);
        }
    
        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        /// <summary>
        /// 使用指定的比较器在整个已排序的 ArrayList 中搜索元素，并返回该元素从零开始的索引。
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="count">总数</param>
        /// <param name="value">值</param>
        /// <param name="comparer">比较器</param>
        /// <returns></returns>
        public virtual int BinarySearch(int index, int count, Object value, IComparer comparer) {
            if (index < 0)//如果索引小于零，则抛出ArgumentOutOfRange_NeedNonNegNum index的异常
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)//如果索引大于count，则抛出ArgumentOutOfRange_NeedNonNegNum count的异常
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.Ensures(Contract.Result<int>() < index + count);
            Contract.EndContractBlock();
    
            return Array.BinarySearch((Array)_items, index, count, value, comparer);//搜索值在ArrayList中的索引
        }
        
        /// <summary>
        /// 使用默认比较器搜索整个排序数组列表的一个元素并返回元素的从零开始的索引。
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public virtual int BinarySearch(Object value)
        {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, null);
        }

        /// <summary>
        /// 使用给定比较器搜索整个排序数组列表的一个元素并返回元素的从零开始的索引。
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="comparer">比较器</param>
        /// <returns>索引</returns>
        public virtual int BinarySearch(Object value, IComparer comparer)
        {
            Contract.Ensures(Contract.Result<int>() < Count);
            return BinarySearch(0, Count, value, comparer);
        }

    
        /// <summary>
        /// 从 ArrayList 中移除所有元素。
        /// </summary>
        public virtual void Clear() {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size); // 不需要医生,但我们清楚以便gc回收的元素引用。
                _size = 0;
            }
            _version++;
        }

        //克隆这个ArrayList,做一个浅拷贝。(一份由所有对象引用在ArrayList,但是指向的对象不是克隆)。
        /// <summary>
        /// 创建一个ArrayList的浅拷贝。(即只复制引用)
        /// </summary>
        /// <returns>ArrayList</returns>
        public virtual Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            ArrayList la = new ArrayList(_size);//创建新的ArrayList
            la._size = _size;
            la._version = _version;
            Array.Copy(_items, 0, la._items, 0, _size);//复制数字内容引用
            return la;
        }
    
        // 包含返回true,如果指定的元素在ArrayList。一个线性,O(n)搜索。平等是由调用item.Equals()。
        /// <summary>
        /// 确定某元素是否在 ArrayList 中。
        /// </summary>
        /// <param name="item">元素对象</param>
        /// <returns></returns>
        public virtual bool Contains(Object item) {
            if (item==null) {//如果元素对象为空，则判断ArrayList中是否用空元素
                for(int i=0; i<_size; i++)
                    if (_items[i]==null)
                        return true;
                return false;
            }
            else {
                for(int i=0; i<_size; i++)
                    if ( (_items[i] != null) && (_items[i].Equals(item)) )//元素不为空，且元素对象相同
                        return true;
                return false;
            }
        }

        // 将此数组列表复制到数组,必须兼容数组类型。
        /// <summary>
        /// 从目标数组的开头开始，将整个 ArrayList 复制到一维兼容 Array。
        /// </summary>
        /// <param name="array"></param>
        public virtual void CopyTo(Array array) {
            CopyTo(array, 0);
        }

        // 将此数组列表复制到数组,必须兼容数组类型。
        /// <summary>
        /// 整个数组列表复制到一个兼容的一维数组,从目标的指定索引数组。
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public virtual void CopyTo(Array array, int arrayIndex) {
            if ((array != null) && (array.Rank != 1))//如果array为空，或array的维度不为1，则抛出Arg_RankMultiDimNotSupported
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // 委托其他Array.Copy错误检查。
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        // 副本的ArrayList兼容一维数组中的元素,从目标的指定索引数组。
        // 
        /// <summary>
        /// 从目标数组的指定索引处开始，将一定范围的元素从 System.Collections.ArrayList 复制到兼容的一维 System.Array中
        /// </summary>
        /// <param name="index">源 System.Collections.ArrayList 中复制开始位置的从零开始的索引。</param>
        /// <param name="array">作为从 System.Collections.ArrayList 复制的元素的目标位置的一维 System.Array。System.Array必须具有从零开始的索引。</param>
        /// <param name="arrayIndex">array 中从零开始的索引，将在此处开始复制。</param>
        /// <param name="count">要复制的元素数。</param>
        public virtual void CopyTo(int index, Array array, int arrayIndex, int count) {
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if ((array != null) && (array.Rank != 1))
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        // 确保这个列表的能力至少是给定的最小值。如果小于最小值列表的电子商务能力,能力是当前能力或分钟增加到两次,哪个比较大。
        /// <summary>
        /// 保证数组容量合格
        /// </summary>
        /// <param name="min">最小数组容量</param>
        private void EnsureCapacity(int min) {
            if (_items.Length < min) {//如果Obejct数组小于最小值
                int newCapacity = _items.Length == 0? _defaultCapacity: _items.Length * 2;//如果数组内容不为零，则将数组大小扩大为原来的两倍
                // 在遇到溢出之前，允许将列表增加到最大，并检查新的数组内容大小
                if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;//设置新的数组内容大小
            }
        }

        // 返回包装器固定在当前列表的大小。添加或删除条目的操作就会失败,但是,替换物品是被允许的。
        /// <summary>
        /// 返回一个IList包装固定大小。
        /// </summary>
        /// <param name="list">要包装的 System.Collections.IList。</param>
        /// <returns>具有固定大小的 System.Collections.IList 包装。</returns>
        /// <exception cref="System.ArgumentNullException">list 为 null。</exception>
        public static IList FixedSize(IList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<IList>() != null);
            Contract.EndContractBlock();
            return new FixedSizeList(list);
        }

        /// <summary>
        /// 返回具有固定大小的 System.Collections.ArrayList 包装。
        /// </summary>
        /// <param name="list">要包装的 System.Collections.ArrayList</param>
        /// <returns>具有固定大小的 System.Collections.ArrayList 包装。</returns>
        /// <exception cref="System.ArgumentNullException"> list 为 null。</exception>
        public static ArrayList FixedSize(ArrayList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();
            return new FixedSizeArrayList(list);
        }

        //返回一个枚举器,这个列表的允许删除元素。如果修改在进步,虽然枚举列表枚举器的MoveNext和GetObject方法将抛出异常。
        /// <summary>
        /// 返回用于整个 ArrayList 的枚举数。
        /// </summary>
        /// <returns>用于整个 System.Collections.ArrayList 的 System.Collections.IEnumerator。</returns>
        public virtual IEnumerator GetEnumerator() {
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            return new ArrayListEnumeratorSimple(this);
        }
    
        /// <summary>
        /// 返回 System.Collections.ArrayList 中某个范围内的元素的枚举器。
        /// </summary>
        /// <param name="index">枚举器应引用的 System.Collections.ArrayList 部分从零开始的起始索引。</param>
        /// <param name="count">枚举器应引用的 System.Collections.ArrayList 部分中的元素数。</param>
        /// <returns>System.Collections.ArrayList 中指定范围内的元素的 System.Collections.IEnumerator。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index 小于零。- 或 -count 小于零。</exception>
        public virtual IEnumerator GetEnumerator(int index, int count) {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            Contract.EndContractBlock();
    
            return new ArrayListEnumerator(this, index, count);
        }
    
        /// <summary>
        /// 搜索指定的 System.Object，并返回整个 System.Collections.ArrayList 中第一个匹配项的从零开始的索引。
        /// </summary>
        /// <param name="value">要在ArrayList中查找的Object.该值可以为null</param>
        /// <returns>如果在整个ArrayList中找到value的第一个匹配项，则返回该项的从零开始的索引；否则为-1</returns>
        public virtual int IndexOf(Object value) {
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf((Array)_items, value, 0, _size);
        }
    
        // 返回第一次出现的指数的给定值列表。向前搜索列表时,从指数startIndex,最后计算元素的数量。列表的元素使用对象的相等方法给定值进行比较。
        // This method uses the Array.IndexOf method to perform the
        // search.
        /// <summary>
        /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中从指定索引到最后一个元素的元素范围内第一个匹配项的从零开始的索引。
        /// </summary>
        /// <param name="value">要在ArrayList中查找的Object。该值可以为null</param>
        /// <param name="startIndex">从零开始的搜索的起始索引。空列表中 0（零）为有效值。</param>
        /// <returns>如果在 System.Collections.ArrayList 中从 startIndex 到最后一个元素的元素范围内找到 value 的第一个匹配项，则为该项的从零开始的索引；否则为-1</returns>
        /// <exception cref="ArgumentOutOfRange_Index">startIndex 不在 System.Collections.ArrayList 的有效索引范围内。</exception>
        public virtual int IndexOf(Object value, int startIndex) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, _size - startIndex);
        }

        /// <summary>
        /// 搜索指定的 Object，并返回 ArrayList 中从指定索引开始并包含指定元素数的这部分元素中第一个匹配项的从零开始索引。
        /// </summary>
        /// <param name="value">要在 System.Collections.ArrayList 中查找的 System.Object。该值可以为 null。</param>
        /// <param name="startIndex">从零开始的搜索的起始索引。空列表中 0（零）为有效值。</param>
        /// <param name="count">要搜索的部分中的元素数。</param>
        /// <returns>如果在 System.Collections.ArrayList 中从 startIndex 开始并包含 count 个元素的元素范围内找到 value的第一个匹配项，则为该项的从零开始的索引；否则为 -1。</returns>
        /// <exception cref="ArgumentOutOfRange_Index">startIndex 不在 System.Collections.ArrayList 的有效索引范围内。- 或 -count 小于零。- 或 -startIndex和 count 未指定 System.Collections.ArrayList 中的有效部分。</exception>
        public virtual int IndexOf(Object value, int startIndex, int count) {
            if (startIndex > _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count <0 || startIndex > _size - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return Array.IndexOf((Array)_items, value, startIndex, count);
        }
    
        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        /// <summary>
        /// 将元素插入 System.Collections.ArrayList 的指定索引处。
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 value。</param>
        /// <param name="value">要插入的 System.Object。该值可以为 null。</param>
        /// <exception cref="ArgumentOutOfRangeException">index 小于零。- 或 -index 大于 System.Collections.ArrayList.Count。</exception>
        public virtual void Insert(int index, Object value) {
            // Note that insertions at the end are legal.
            if (index < 0 || index > _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_ArrayListInsert"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + 1);
            Contract.EndContractBlock();

            if (_size == _items.Length) EnsureCapacity(_size + 1);//如果容量达到最大时，扩展容量
            if (index < _size) {//将数字内容进行浅复制，将index索引为后内容向后移动
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = value;//在index索引位进行赋值
            _size++;//增加实际容量
            _version++;//增加版本号
        }

        //插入给定的集合的元素在给定索引。如果需要,列表的能力增加到两次之前的能力或新尺寸,哪个比较大。范围可能被添加到列表的最后通过设置索引数组列表的大小。
        /// <summary>
        /// 插入给定的集合的元素在给定索引。
        /// </summary>
        /// <param name="index">从index的索引为增加</param>
        /// <param name="c">集合</param>
        public virtual void InsertRange(int index, ICollection c) {
            if (c == null)//如果集合为空，则抛出ArgumentNullException异常
                throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            if (index < 0 || index > _size) //如果索引小于0或超过数组大小，则抛出ArgumentOutOfRangeException异常
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            //Contract.Ensures(Count == Contract.OldValue(Count) + c.Count);
            Contract.EndContractBlock();

            int count = c.Count;//获取集合大小
            if (count > 0) {//如果内容大小大于零
                EnsureCapacity(_size + count);                
                // shift existing items
                if (index < _size) {//如果索引小于大小，则进行数组赋值
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                Object[] itemsToInsert = new Object[count];//初始化插入数组
                c.CopyTo(itemsToInsert, 0);//将集合内容赋值到插入数组中
                itemsToInsert.CopyTo(_items, index);//将插入数组内容插入到_items中的第index索引
                _size += count;//增加实际包含的元素数
                _version++;//增加版本
            }
        }
    
        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public virtual int LastIndexOf(Object value)
        {
            Contract.Ensures(Contract.Result<int>() < _size);
            return LastIndexOf(value, _size - 1, _size);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // startIndex and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        /// <summary>
        /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中包含指定的元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引。
        /// </summary>
        /// <param name="value">要在 System.Collections.ArrayList 中查找的 System.Object。该值可以为 null。</param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public virtual int LastIndexOf(Object value, int startIndex)
        {
            if (startIndex >= _size)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // startIndex and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        /// <summary>
        /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中包含指定的元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引。
        /// </summary>
        /// <param name="value">要在 System.Collections.ArrayList 中查找的 System.Object。该值可以为 null。</param>
        /// <param name="startIndex">向后搜索的从零开始的起始索引。</param>
        /// <param name="count">要搜索的部分中的元素数。</param>
        /// <returns>如果在 System.Collections.ArrayList 中包含 count 个元素、在 startIndex 处结尾的元素范围内找到 value的最后一个匹配项，则为该项的从零开始的索引；否则为 -1。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">startIndex 不在 System.Collections.ArrayList 的有效索引范围内。- 或 -count 小于零。- 或 -startIndex和 count 未指定 System.Collections.ArrayList 中的有效部分。</exception>
        public virtual int LastIndexOf(Object value, int startIndex, int count) {
            if (Count != 0 && (startIndex < 0 || count < 0))
                throw new ArgumentOutOfRangeException((startIndex<0 ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();

            if (_size == 0)  // Special case for an empty list
                return -1;

            if (startIndex >= _size || count > startIndex + 1) 
                throw new ArgumentOutOfRangeException((startIndex>=_size ? "startIndex" : "count"), Environment.GetResourceString("ArgumentOutOfRange_BiggerThanCollection"));

            return Array.LastIndexOf((Array)_items, value, startIndex, count);
        }
    
        /// <summary>
        /// 返回只读的 System.Collections.IList 包装。
        /// </summary>
        /// <param name="list">要包装的 System.Collections.IList。</param>
        /// <returns>list 周围的只读 System.Collections.IList 包装。</returns>
        /// <exception cref="System.ArgumentNullException">list 为 null</exception>
#if FEATURE_CORECLR
        [FriendAccessAllowed]
#endif
        public static IList ReadOnly(IList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<IList>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyList(list);
        }

        /// <summary>
        /// 返回只读的 System.Collections.ArrayList 包装。
        /// </summary>
        /// <param name="list">要包装的 System.Collections.ArrayList。</param>
        /// <returns>list 周围的只读 System.Collections.ArrayList 包装。</returns>
        /// <exception cref="System.ArgumentNullException">list 为 null。</exception>
        public static ArrayList ReadOnly(ArrayList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();
            return new ReadOnlyArrayList(list);
        }
    
        // 移除索引出的元素，list的大小减一
        /// <summary>
        /// 从 System.Collections.ArrayList 中移除特定对象的第一个匹配项。
        /// </summary>
        /// <param name="obj">要从 System.Collections.ArrayList 移除的 System.Object。该值可以为 null。</param>
        public virtual void Remove(Object obj) {
            Contract.Ensures(Count >= 0);

            int index = IndexOf(obj);//获取obj的索引位置
            BCLDebug.Correctness(index >= 0 || !(obj is Int32), "You passed an Int32 to Remove that wasn't in the ArrayList." + Environment.NewLine + "Did you mean RemoveAt?  int: "+obj+"  Count: "+Count);
            if (index >=0) 
                RemoveAt(index);
        }
    
        /// <summary>
        /// 移除 System.Collections.ArrayList 的指定索引处的元素。
        /// </summary>
        /// <param name="index">要移除的元素的从零开始的索引。</param>
        /// <exception cref="System.ArgumentOutOfRangeException">index 小于零。- 或 -index 等于或大于 System.Collections.ArrayList.Count。</exception>
        /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。- 或 -System.Collections.ArrayList 具有固定大小。</exception>
        public virtual void RemoveAt(int index) {
            if (index < 0 || index >= _size) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - 1);
            Contract.EndContractBlock();

            _size--;//减少list大小
            if (index < _size) {//将第index位以后的元素向前移一位
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = null;//将第_size位设置为null
            _version++;//版本加1
        }
    
        /// <summary>
        /// 从 System.Collections.ArrayList 中移除一定范围的元素。
        /// </summary>
        /// <param name="index">要移除的元素的范围从零开始的起始索引。</param>
        /// <param name="count">要移除的元素数。</param>
        /// <exception cref="System.ArgumentOutOfRangeException:"> index 小于零。- 或 -count 小于零。</exception>
        /// <exception cref="System.ArgumentException:">index 和 count 不表示 System.Collections.ArrayList 中元素的有效范围。</exception>
        /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。- 或 -System.Collections.ArrayList 具有固定大小。</exception>
        public virtual void RemoveRange(int index, int count) {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Count >= 0);
            //Contract.Ensures(Count == Contract.OldValue(Count) - count);
            Contract.EndContractBlock();
    
            if (count > 0) {
                int i = _size;//获取_size值
                _size -= count;//更新ArrayList实际包含大小
                if (index < _size) {//将数组元素向前移动count位
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
                while (i > _size) _items[--i] = null;//将倒数_size为的元素设置为null
                _version++;//版本加1
            }
        }
    
        /// <summary>
        /// 返回 System.Collections.ArrayList，它的元素是指定值的副本。
        /// </summary>
        /// <param name="value">要在新 System.Collections.ArrayList 中对其进行多次复制的 System.Object。该值可以为 null。</param>
        /// <param name="count">value 应被复制的次数。</param>
        /// <returns>具有 count 所指定的元素数的 System.Collections.ArrayList，其中的所有元素都是 value 的副本。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">count 小于零。</exception>
        public static ArrayList Repeat(Object value, int count) {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count",Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();

            ArrayList list = new ArrayList((count>_defaultCapacity)?count:_defaultCapacity);//如果count大于默认容量，则将数字容量设置为count，否则设置为默认容量
            for(int i=0; i<count; i++)//将count个value副本插入list中
                list.Add(value);
            return list;
        }

        /// <summary>
        /// 将整个 System.Collections.ArrayList 中元素的顺序反转。
        /// </summary>
        public virtual void Reverse() {
            Reverse(0, Count);
        }

        // 逆转这个列表的元素范围。一个元素调用该方法后,在索引的范围和计算以前位于索引我现在将位于索引索引+(指数+计数- i - 1)。 
        /// <summary>
        /// 将指定范围中元素的顺序反转。
        /// </summary>
        /// <param name="index">要反转的范围的从零开始的起始索引。</param>
        /// <param name="count">要反转的范围内的元素数。</param>
        /// <exception cref="System.ArgumentOutOfRangeException:">index 小于零。- 或 -count 小于零。</exception>
        /// <exception cref="System.ArgumentException:">index 和 count 不表示 System.Collections.ArrayList 中元素的有效范围。</exception>
        /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。</exception>
        public virtual void Reverse(int index, int count) {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            Array.Reverse(_items, index, count);
            _version++;
        }

        // 将元素从给定的索引设置为给定的集合的元素。
        /// <summary>
        /// 将集合中的元素复制到 System.Collections.ArrayList 中一定范围的元素上。
        /// </summary>
        /// <param name="index">从零开始的 System.Collections.ArrayList 索引，从该位置开始复制 c 的元素。</param>
        /// <param name="c">System.Collections.ICollection，要将其元素复制到 System.Collections.ArrayList 中。集合本身不能为null，但它可以包含为 null 的元素。</param>
        /// <exception cref="System.ArgumentOutOfRangeException:">index 小于零。- 或 -index 加上 c 中的元素数大于 System.Collections.ArrayList.Count。</exception>
        /// <exception cref="System.ArgumentNullException:">c 为 null。</exception>
        public virtual void SetRange(int index, ICollection c) {
            if (c==null) throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
            Contract.EndContractBlock();
            int count = c.Count;
            if (index < 0 || index > _size - count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            
            if (count > 0) {
                c.CopyTo(_items, index);
                _version++;
            }
        }
        
        /// <summary>
        /// 返回 System.Collections.ArrayList，它表示源 System.Collections.ArrayList 中元素的子集。
        /// </summary>
        /// <param name="index">范围开始处的从零开始的 System.Collections.ArrayList 索引。</param>
        /// <param name="count">范围中的元素数。</param>
        /// <returns>System.Collections.ArrayList，它表示源 System.Collections.ArrayList 中元素的子集。</returns>
        /// <exception cref="System.ArgumentOutOfRangeException:">index 小于零。- 或 -count 小于零。</exception>
        /// <exception cref="System.ArgumentException:">index 和 count 不表示 System.Collections.ArrayList 中元素的有效范围。</exception>
        public virtual ArrayList GetRange(int index, int count) {
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();
            return new Range(this,index, count);//获取从index位的ArrayList子集
        }
        
        /// <summary>
        /// 使用每个元素的 System.IComparable 实现对整个 System.Collections.ArrayList 中的元素进行排序。
        /// </summary>
        public virtual void Sort()
        {
            Sort(0, Count, Comparer.Default);
        }

        /// <summary>
        /// 使用指定的比较器对整个 System.Collections.ArrayList 中的元素进行排序。
        /// </summary>
        /// <param name="comparer">比较元素时要使用的 System.Collections.IComparer 实现。- 或 -null 引用（Visual Basic 中为 Nothing）将使用每个元数的System.IComparable 实现。</param>
        /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。</exception>
        public virtual void Sort(IComparer comparer)
        {
            Sort(0, Count, comparer);
        }

        // 这个列表的元素部分。那种相互比较的元素使用给定IComparer接口。如果零比较器,比较的元素使用IComparable接口,在这种情况下必须实现的所有元素的列表。
        // This method uses the Array.Sort method to sort the elements.
        /// <summary>
        /// 使用指定的比较器对 System.Collections.ArrayList 中某个范围内的元素进行排序。
        /// </summary>
        /// <param name="index">要排序的范围的从零开始的起始索引。</param>
        /// <param name="count">要排序的范围的长度。</param>
        /// <param name="comparer">比较元素时要使用的 System.Collections.IComparer 实现。- 或 -null 引用（Visual Basic 中为 Nothing）将使用每个元数的System.IComparable 实现。</param>
        /// <exception cref="System.ArgumentOutOfRangeException:">index 小于零。- 或 -count 小于零。</exception>
        /// <exception cref="System.ArgumentException:">index 和 count 未指定 System.Collections.ArrayList 中的有效范围。</exception>
        /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。</exception>
        public virtual void Sort(int index, int count, IComparer comparer) {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (_size - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            
            Array.Sort(_items, index, count, comparer);
            _version++;
        }
    
        /// <summary>
        /// 返回同步的（线程安全）System.Collections.IList 包装。
        /// </summary>
        /// <param name="list">要同步的 System.Collections.IList。</param>
        /// <returns>同步的（线程安全）System.Collections.IList 包装。</returns>
        [HostProtection(Synchronization=true)]
        public static IList Synchronized(IList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<IList>() != null);
            Contract.EndContractBlock();
            return new SyncIList(list);
        }
    
        /// <summary>
        /// 返回同步的（线程安全）System.Collections.ArrayList 包装。
        /// </summary>
        /// <param name="list">要同步的 System.Collections.ArrayList。</param>
        /// <returns>同步的（线程安全）System.Collections.ArrayList 包装。</returns>
        [HostProtection(Synchronization=true)]
        public static ArrayList Synchronized(ArrayList list) {
            if (list==null)
                throw new ArgumentNullException("list");
            Contract.Ensures(Contract.Result<ArrayList>() != null);
            Contract.EndContractBlock();
            return new SyncArrayList(list);
        }
    
        /// <summary>
        /// 将 System.Collections.ArrayList 的元素复制到新 System.Object 数组中。
        /// </summary>
        /// <returns>System.Object 数组，它包含 System.Collections.ArrayList 中元素的副本。</returns>
        public virtual Object[] ToArray() {
            Contract.Ensures(Contract.Result<Object[]>() != null);

            Object[] array = new Object[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }
        //ToArray返回一个特定类型的新数组,其中包含的内容ArrayList。
        //这需要复制ArrayList并可能向下类型转换所有的元素。这个副本可能失败,是一种O(n)运算。
        //在内部,这个实现调用Array.Copy。
        /// <summary>
        /// 将 System.Collections.ArrayList 的元素复制到指定元素类型的新数组中。
        /// </summary>
        /// <param name="type">要创建并向其复制元素的目标数组的元素 System.Type。</param>
        /// <returns>指定元素类型的数组，它包含 System.Collections.ArrayList 中元素的副本。</returns>
        [SecuritySafeCritical]
        public virtual Array ToArray(Type type) {
            if (type==null)
                throw new ArgumentNullException("type");
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.EndContractBlock();
            Array array = Array.UnsafeCreateInstance(type, _size);
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        // 集的能力这个列表的大小。
        // 该方法可用于最小化一个列表的内存开销曾经众所周知,没有新元素将被添加到列表中。
        // 完全清晰的列表和释放所有内存引用列表,执行下面的语句:
        // list.Clear();
        // list.TrimToSize();
        /// <summary>
        /// 将容量设置为 System.Collections.ArrayList 中元素的实际数目。
        /// </summary>
        /// <exception cref="System.NotSupportedException">System.Collections.ArrayList 是只读的。- 或 -System.Collections.ArrayList 具有固定大小</exception>
        public virtual void TrimToSize() {
            Capacity = _size;
        }


        // 这类包装一个IList,暴露出它作为一个ArrayList注意这需要重新实现一半ArrayList……
        /// <summary>
        /// IList包装器
        /// </summary>
        [Serializable]
        private class IListWrapper : ArrayList
        {
            private IList _list;
            
            /// <summary>
            /// IList包装器
            /// </summary>
            /// <param name="list">继承IList的类</param>
            internal IListWrapper(IList list) {
                _list = list;
                _version = 0; // list不包含版本号
            }
            
            /// <summary>
            /// 获取或设置list容量
            /// </summary>
             public override int Capacity {
                get { return _list.Count; }//返回list的数量
                set {
                    if (value < Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                    Contract.EndContractBlock();
                }
            }
            
            /// <summary>
             /// 获取 ICollection 中包含的元素数。（从 ICollection 继承。）
            /// </summary>
            public override int Count { 
                get { return _list.Count; }
            }
            
            /// <summary>
            /// 获取一个值，该值指示 IList 是否为只读。
            /// </summary>
            public override bool IsReadOnly { 
                get { return _list.IsReadOnly; }
            }

            /// <summary>
            /// 获取一个值，该值指示 IList 是否具有固定大小。
            /// </summary>
            public override bool IsFixedSize {
                get { return _list.IsFixedSize; }
            }

            
            /// <summary>
            /// 获取一个值，该值指示是否同步对 ICollection 的访问（线程安全）。（从 ICollection 继承。）
            /// </summary>
            public override bool IsSynchronized { 
                get { return _list.IsSynchronized; }
            }
            
            /// <summary>
            /// 获取或设置位于指定索引处的元素。
            /// </summary>
            /// <param name="index">索引</param>
            /// <returns>索引处对象</returns>
             public override Object this[int index] {
                get {
                    return _list[index];
                }
                set {
                    _list[index] = value;
                    _version++;
                }
            }
            
            /// <summary>
            /// 获取可用于同步对 ICollection 的访问的对象。（从 ICollection 继承。）
            /// </summary>
            public override Object SyncRoot {
                get { return _list.SyncRoot; }
            }
            
            /// <summary>
            /// 将某项添加到 IList 中。
            /// </summary>
            /// <param name="obj">添加对象</param>
            /// <returns>添加对象的索引</returns>
            public override int Add(Object obj) {
                int i = _list.Add(obj);
                _version++;
                return i;
            }
            
            /// <summary>
            /// 添加集合范围内的元素
            /// </summary>
            /// <param name="c">集合</param>
            public override void AddRange(ICollection c) {
                InsertRange(Count, c);//插入范围
            }
    
            /// <summary>
            /// 使用给定比较器搜索整个排序数组列表的一个元素并返回元素的从零开始的索引
            /// </summary>
            /// <param name="index">启动索引</param>
            /// <param name="count">搜索数量</param>
            /// <param name="value">搜索对象</param>
            /// <param name="comparer">比较器</param>
            /// <returns>元素所在处索引</returns>
            /// <exception cref="ArgumentOutOfRangeException">index 和 count 未指定 System.Collections.ArrayList 中的有效范围。</exception>
            /// <exception cref="ArgumentException"></exception>
            public override int BinarySearch(int index, int count, Object value, IComparer comparer) 
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (this.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();
                if (comparer == null)//如果比较器为空，则设置为默认比较器
                    comparer = Comparer.Default;
                
                int lo = index;//开始位
                int hi = index + count - 1;//结束位
                int mid;
                while (lo <= hi) {//如果开始位小于或等于结束位，则进行循环判断
                    mid = (lo+hi)/2;//获取中间位
                    int r = comparer.Compare(value, _list[mid]);//使用比较器比较大小
                    if (r == 0)
                        return mid;
                    if (r < 0)
                        hi = mid-1;
                    else 
                        lo = mid+1;
                }
                // return bitwise complement of the first element greater than value.
                // Since hi is less than lo now, ~lo is the correct item.
                return ~lo;//返回第一个元素的位补大于价值。
            }
            
            /// <summary>
            /// 从 IList 中移除所有项。
            /// </summary>
            /// <exception cref="NotSupportedException">System.Collections.ArrayList 是只读的。- 或 -System.Collections.ArrayList 具有固定大小</exception>
            public override void Clear() {
                //如果_list是一个数组,它将支持明确的方法。我们不应该允许明确操作FixedSized ArrayList
                if(_list.IsFixedSize) {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
                }

                _list.Clear();//移除所有项
                _version++;
            }
            
            /// <summary>
            /// 创建一个ArrayList的浅拷贝。(即只复制引用)
            /// 这并不做_list变成一个ArrayList的浅拷贝!
            /// 这个克隆IListWrapper,创建另一个包装器类!
            /// </summary>
            /// <returns></returns>
            public override Object Clone() {
                // This does not do a shallow copy of _list into a ArrayList!
                // This clones the IListWrapper, creating another wrapper class!
                return new IListWrapper(_list);
            }
            
            /// <summary>
            /// 确定 IList 是否包含特定值。
            /// </summary>
            /// <param name="obj">元素对象</param>
            /// <returns></returns>
            public override bool Contains(Object obj) {
                return _list.Contains(obj);
            }
            
            /// <summary>
            /// 从特定的 System.Array 索引处开始，将 System.Collections.ICollection 的元素复制到一个 System.Array中。
            /// </summary>
            /// <param name="array">作为从 System.Collections.ICollection 复制的元素的目标位置的一维 System.Array。System.Array必须具有从零开始的索引。</param>
            /// <param name="index">array 中从零开始的索引，将在此处开始复制。</param>
            public override void CopyTo(Array array, int index) {
                _list.CopyTo(array, index);
            }
    
            /// <summary>
            /// 从特定的 System.Array 索引处开始，将 System.Collections.ICollection 的元素复制到一个 System.Array中
            /// </summary>
            /// <param name="index">IList开始位置</param>
            /// <param name="array">作为从 System.Collections.ICollection 复制的元素的目标位置的一维 System.Array。</param>
            /// <param name="arrayIndex">System.Array必须具有从arrayIndex开始的索引。</param>
            /// <param name="count">复制数量</param>
            /// <exception cref="ArgumentNullException">array为空</exception>
            /// <exception cref="ArgumentOutOfRangeException">index、arrayIndex、count小于零</exception>
            /// <exception cref="ArgumentException">count数字越界</exception>
            public override void CopyTo(int index, Array array, int arrayIndex, int count) {
                if (array==null)
                    throw new ArgumentNullException("array");
                if (index < 0 || arrayIndex < 0)
                    throw new ArgumentOutOfRangeException((index < 0) ? "index" : "arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if( count < 0)
                    throw new ArgumentOutOfRangeException( "count" , Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));                 
                if (array.Length - arrayIndex < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                if (array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                Contract.EndContractBlock();

                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                
                for(int i=index; i<index+count; i++)//将list中部分内容复制到array中
                    array.SetValue(_list[i], arrayIndex++);
            }
            
            /// <summary>
            /// 返回循环访问集合的枚举数。（从 IEnumerable 继承。）
            /// </summary>
            /// <returns></returns>
            public override IEnumerator GetEnumerator() {
                return _list.GetEnumerator();
            }
            
            /// <summary>
            /// 返回循环访问集合的枚举数。（从 IEnumerable 继承。）
            /// </summary>
            /// <param name="index">起始索引位</param>
            /// <param name="count">枚举数量</param>
            /// <returns></returns>
            public override IEnumerator GetEnumerator(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
    
                return new IListWrapperEnumWrapper(this, index, count);
            }
            
            /// <summary>
            /// 确定 System.Collections.IList 中特定项的索引。
            /// </summary>
            /// <param name="value">要在 System.Collections.IList 中查找的对象。</param>
            /// <returns>如果在列表中找到 value，则为该项的索引；否则为 -1。</returns>
            public override int IndexOf(Object value) {
                return _list.IndexOf(value);
            }

            /// <summary>
            /// 确定 System.Collections.IList 中特定项的索引。
            /// </summary>
            /// <param name="value">要在 System.Collections.IList 中查找的对象。</param>
            /// <param name="startIndex">在起始位置索引</param>
            /// <returns>如果在列表中找到 value，则为该项的索引；否则为 -1。</returns>
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // 跳过额外的错误检查避免**AppCompat潜在的问题
            public override int IndexOf(Object value, int startIndex) {
                return IndexOf(value, startIndex, _list.Count - startIndex);
            }
    
            /// <summary>
            /// 确定 System.Collections.IList 中特定项的索引。
            /// </summary>
            /// <param name="value">要在IList中特定项的对象</param>
            /// <param name="startIndex">在起始位置索引</param>
            /// <param name="count">搜索数量</param>
            /// <returns>如果在列表中找到value，则为该项索引；否则为-1。</returns>
            /// <exception cref="ArgumentOutOfRangeException">startIndex小于零或大于包含元素数量，count超出边界</exception>
            public override int IndexOf(Object value, int startIndex, int count) {
                if (startIndex < 0 || startIndex > this.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (count < 0 || startIndex > this.Count - count) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
                Contract.EndContractBlock();

                int endIndex = startIndex + count;//获取结束项索引
                if (value == null) {//如果结束项为空，则判断范围内是否有为空的值
                    for(int i=startIndex; i<endIndex; i++)
                        if (_list[i] == null)
                            return i;
                    return -1;
                } else {//如果不为空，则判断范围内是否有相等的值
                    for(int i=startIndex; i<endIndex; i++)
                        if (_list[i] != null && _list[i].Equals(value))
                            return i;
                    return -1;
                }
            }
            
            /// <summary>
            /// 将元素对象插入特定位置处
            /// </summary>
            /// <param name="index">插入为索引</param>
            /// <param name="obj">插入元素对象</param>
            public override void Insert(int index, Object obj) {
                _list.Insert(index, obj);//调用IList.Insert
                _version++;
            }
            
            /// <summary>
            /// 在List中插入集合
            /// </summary>
            /// <param name="index">起始位索引</param>
            /// <param name="c">所要插入的集合</param>
            /// <exception cref="ArgumentNullException">c集合为空</exception>
            /// <exception cref="ArgumentOutOfRangeException">index超出索引范围</exception>
            public override void InsertRange(int index, ICollection c) {
                if (c==null)
                    throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
                if (index < 0 || index > this.Count) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();

                if( c.Count > 0) {//如果集合为空，则不进行操作，否则将集合插入到list中
                    ArrayList al = _list as ArrayList;//将IList转换为ArrayList,如果不成功，则使用枚举迭代插入
                    if( al != null) { //如果al不为空，则调用ArrayList.InsertRange
                        // 我们需要特例ArrayList。
                        // 一系列_list c时,我们需要在一种特殊的方式处理这个问题。
                        // 看到ArrayList。InsertRange详情。
                        al.InsertRange(index, c);    
                    }
                    else {
                        IEnumerator en = c.GetEnumerator();//枚举迭代插入
                        while(en.MoveNext()) {
                            _list.Insert(index++, en.Current);
                        }                   
                    }
                    _version++;//版本加1
                }
            }
            
            /// <summary>
            /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中包含指定的元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引(重载了ArrayList)
            /// </summary>
            /// <param name="value">搜索对象</param>
            /// <returns>对象所在处索引，如果不存在，则返回为-1</returns>
            public override int LastIndexOf(Object value) {
                 return LastIndexOf(value,_list.Count - 1, _list.Count);
            }

            /// <summary>
            /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中包含指定的元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引(重载了ArrayList)
            /// </summary>
            /// <param name="value">搜索对象</param>
            /// <param name="startIndex">起始处索引</param>
            /// <returns>对象所在处索引，如果不存在，则返回为-1</returns>
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  //  跳过额外的错误检查避免**AppCompat潜在的问题
            public override int LastIndexOf(Object value, int startIndex) {
                return LastIndexOf(value, startIndex, startIndex + 1);
            }

            /// <summary>
            /// 搜索指定的 System.Object，并返回 System.Collections.ArrayList 中包含指定的元素数并在指定索引处结束的元素范围内最后一个匹配项的从零开始的索引(重载了ArrayList)
            /// </summary>
            /// <param name="value">搜索对象</param>
            /// <param name="startIndex">起始处索引</param>
            /// <param name="count">搜索数量</param>
            /// <returns>对象所在处索引，如果不存在，则返回为-1</returns>
            /// <exception cref="ArgumentOutOfRangeException">startIndex,count索引越界</exception>
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // 跳过额外的错误检查避免**AppCompat潜在的问题
            public override int LastIndexOf(Object value, int startIndex, int count) {
                if (_list.Count == 0)//如果List包含对象，则返回-1
                    return -1;
   
                if (startIndex < 0 || startIndex >= _list.Count) throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (count < 0 || count > startIndex + 1) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

                int endIndex = startIndex - count + 1;//获取结束位索引
                if (value == null) {
                    for(int i=startIndex; i >= endIndex; i--)
                        if (_list[i] == null)
                            return i;
                    return -1;
                } else {
                    for(int i=startIndex; i >= endIndex; i--)
                        if (_list[i] != null && _list[i].Equals(value))
                            return i;
                    return -1;
                }
            }
            
            /// <summary>
            /// 从 ArrayList 中移除特定对象的第一个匹配项。
            /// </summary>
            /// <param name="value">要从 System.Collections.IList 中移除的对象。</param>
            public override void Remove(Object value) {
                int index = IndexOf(value);//获取移除对象的索引          
                if (index >=0) //如果索引大于等于0，则移除该索引出的对象
                    RemoveAt(index);
            }
            
            /// <summary>
            /// 移除指定索引处的 System.Collections.IList 项。
            /// </summary>
            /// <param name="index">从零开始的索引（属于要移除的项）。</param>
            /// <exception cref="System.NotSupportedException">System.Collections.IList 是只读的。- 或 -System.Collections.IList 具有固定大小。</exception>
            public override void RemoveAt(int index) {
                _list.RemoveAt(index);
                _version++;
            }
            
            /// <summary>
            /// 从 ArrayList 中移除一定范围的元素。
            /// </summary>
            /// <param name="index">起始索引</param>
            /// <param name="count">结束索引</param>
            /// <exception cref="ArgumentOutOfRangeException">index,count小于零</exception>
            /// <exception cref="ArgumentException">count超出范围</exception>
            public override void RemoveRange(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                
                if( count > 0)    // 和ArrayList是一致的
                    _version++;

                while(count > 0) {//通过count来计数，删除index位索引，直到count小于0
                    _list.RemoveAt(index);
                    count--;
                }
            }
    
            /// <summary>
            /// 将指定范围中元素的顺序反转。
            /// </summary>
            /// <param name="index">要反转的范围的从零开始的起始索引。</param>
            /// <param name="count">要反转的范围内的元素数。</param>
            /// <exception cref="System.ArgumentOutOfRangeExceptio">index 小于零。- 或 -count 小于零。</exception>
            /// <exception cref="System.ArgumentException">index 和 count 不表示 System.Collections.ArrayList 中元素的有效范围。</exception>
            /// <exception cref="System.NotSupportedException">System.Collections.ArrayList 是只读的。</exception>
            public override void Reverse(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                
                // 获取起始索引和结束索引
                int i = index;
                int j = index + count - 1;
                while (i < j)//交换元素
                {
                    Object tmp = _list[i];
                    _list[i++] = _list[j];
                    _list[j--] = tmp;
                }
                _version++;
            }
    
            /// <summary>
            /// 将集合中的元素复制到 System.Collections.ArrayList 中一定范围的元素上。
            /// </summary>
            /// <param name="index">从零开始的 System.Collections.ArrayList 索引，从该位置开始复制 c 的元素。</param>
            /// <param name="c">System.Collections.ICollection，要将其元素复制到 System.Collections.ArrayList 中。集合本身不能为null，但它可以包含为 null 的元素。</param>
            /// <exception cref="System.ArgumentOutOfRangeException">index 小于零。- 或 -index 加上 c 中的元素数大于 System.Collections.ArrayList.Count。</exception>
            /// <exception cref="System.ArgumentNullException"> c 为 null</exception>
            /// <exception cref="System.NotSupportedException">System.Collections.ArrayList 是只读的</exception>
            public override void SetRange(int index, ICollection c) {
                if (c==null) {
                    throw new ArgumentNullException("c", Environment.GetResourceString("ArgumentNull_Collection"));
                }
                Contract.EndContractBlock();

                if (index < 0 || index > _list.Count - c.Count) {
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));            
                }
                   
                if( c.Count > 0) {                                     
                    IEnumerator en = c.GetEnumerator();//获取集合中的枚举
                    while(en.MoveNext()) {//遍历集合中的元素，并插入list中
                        _list[index++] = en.Current;
                    }
                    _version++;
                }
            }
    
            /// <summary>
            /// 获取Range
            /// </summary>
            /// <param name="index">起始索引</param>
            /// <param name="count">数量</param>
            /// <returns>ArrayList</returns>
            public override ArrayList GetRange(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                return new Range(this,index, count);
            }

            /// <summary>
            /// 使用指定的比较器对 System.Collections.ArrayList 中某个范围内的元素进行排序。
            /// </summary>
            /// <param name="index">要排序的范围的从零开始的起始索引。</param>
            /// <param name="count">要排序的范围的长度。</param>
            /// <param name="comparer">比较元素时要使用的 System.Collections.IComparer 实现。- 或 -null 引用（Visual Basic 中为 Nothing）将使用每个元数的System.IComparable 实现。</param>
            /// <exception cref="System.ArgumentOutOfRangeException:">index 小于零。- 或 -count 小于零。</exception>
            /// <exception cref="System.ArgumentException:">index 和 count 未指定 System.Collections.ArrayList 中的有效范围。</exception>
            /// <exception cref="System.NotSupportedException:">System.Collections.ArrayList 是只读的。</exception>
            public override void Sort(int index, int count, IComparer comparer) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                if (_list.Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                
                Object [] array = new Object[count];//创建一个新的Object数组
                CopyTo(index, array, 0, count);//将数组内容复制到Object数组中
                Array.Sort(array, 0, count, comparer);//使用比较器将object数组内容进行排序
                for(int i=0; i<count; i++)
                    _list[i+index] = array[i];//数组内容进行复制

                _version++;
            }


            public override Object[] ToArray() {
                Object[] array = new Object[Count];
                _list.CopyTo(array, 0);
                return array;
            }

            [SecuritySafeCritical]
            public override Array ToArray(Type type)
            {
                if (type==null)
                    throw new ArgumentNullException("type");
                Contract.EndContractBlock();
                Array array = Array.UnsafeCreateInstance(type, _list.Count);
                _list.CopyTo(array, 0);
                return array;
            }

            public override void TrimToSize()
            {
                // Can't really do much here...
            }
    
        
            /// <summary>
            /// 这是IList的枚举器封装在另一个类的实现ArrayList的所有方法。
            /// </summary>
            [Serializable]
            private sealed class IListWrapperEnumWrapper : IEnumerator, ICloneable
            {
                private IEnumerator _en;
                private int _remaining;     // 当前剩余遍历数量
                private int _initialStartIndex;   // 用于重置
                private int _initialCount;        // 用于重置
                private bool _firstCall;       // 第一次调用MoveNext
    
                private IListWrapperEnumWrapper()
                {
                }

                /// <summary>
                /// 这是IList的枚举器封装在另一个类的实现ArrayList的所有方法。
                /// </summary>
                /// <param name="listWrapper">IList包装器</param>
                /// <param name="startIndex">开始索引处</param>
                /// <param name="count">数量</param>
                internal IListWrapperEnumWrapper(IListWrapper listWrapper, int startIndex, int count) 
                {
                    _en = listWrapper.GetEnumerator();//获取IEnumerator
                    _initialStartIndex = startIndex;//设置初始化起始索引
                    _initialCount = count;//设置初始化数量
                    while(startIndex-- > 0 && _en.MoveNext());
                    _remaining = count;
                    _firstCall = true;
                }

                /// <summary>
                /// 克隆IListWrapperEnumWrapper对象
                /// </summary>
                /// <returns></returns>
                public Object Clone() {
                    // We must clone the underlying enumerator, I think.
                    IListWrapperEnumWrapper clone = new IListWrapperEnumWrapper();
                    clone._en = (IEnumerator) ((ICloneable)_en).Clone();
                    clone._initialStartIndex = _initialStartIndex;
                    clone._initialCount = _initialCount;
                    clone._remaining = _remaining;
                    clone._firstCall = _firstCall;
                    return clone;
                }

                /// <summary>
                /// 将枚举数推进到集合的下一个元素。
                /// </summary>
                /// <returns>如果枚举数已成功地推进到下一个元素，则为 true；如果枚举数传递到集合的末尾，则为 false。</returns>
                public bool MoveNext() {
                    if (_firstCall) {//如果是第一次调用,将_firstCall设置为false
                        _firstCall = false;
                        //return _remaining-- > 0 && _en.MoveNext();
                    }
                    if (_remaining < 0)
                        return false;
                    bool r = _en.MoveNext();
                    return r && _remaining-- > 0;
                }
                
                /// <summary>
                /// 获取当前对象
                /// </summary>
                /// <exception cref="InvalidOperationException">第一次被调用</exception>
                /// <exception cref="InvalidOperationException">剩余遍历数量小于零</exception>
                public Object Current {
                    get {
                        if (_firstCall)
                            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                        if (_remaining < 0)
                            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                        return _en.Current;
                    }
                }
    
                /// <summary>
                /// 将IListWrapperEnumWrapper重置
                /// </summary>
                public void Reset() {
                    _en.Reset();
                    int startIndex = _initialStartIndex;//初始化，默认索引
                    while(startIndex-- > 0 && _en.MoveNext());
                    _remaining = _initialCount;//将初始化数量设置为剩余遍历数量
                    _firstCall = true;
                }
            }
        }
    
    
        /// <summary>
        /// 同步ArrayList(支持序列化)
        /// </summary>
        [Serializable]
        private class SyncArrayList : ArrayList
        {
            private ArrayList _list;
            private Object _root;//用于同步锁对象

            /// <summary>
            /// 同步构造函数
            /// </summary>
            /// <param name="list">用于同步的ArrayList</param>
            internal SyncArrayList(ArrayList list)
                : base( false )
            {
                _list = list;
                _root = list.SyncRoot;
            }
    
            /// <summary>
            /// 重置ArrayList的容纳能力
            /// </summary>
            public override int Capacity {
                get {
                    lock(_root) {//添加对象锁，调用ArrayList.Capacity
                        return _list.Capacity;
                    }
                }
                [SuppressMessage("Microsoft.Contracts", "CC1055")]  // 跳过额外的错误检查避免* * AppCompat潜在问题。
                set {
                    lock(_root) {
                        _list.Capacity = value;
                    }
                }
            }
    
            /// <summary>
            /// 获取 ArrayList 中实际包含的元素数。（支持同步访问）
            /// </summary>
            public override int Count { 
                get { lock(_root) { return _list.Count; } }
            }
    
            /// <summary>
            /// 获取一个值，该值指示ArrayList是否为只读
            /// </summary>
            public override bool IsReadOnly {
                get { return _list.IsReadOnly; }
            }

            /// <summary>
            /// 获取一个值，该值指示ArrayList是否为固定大小
            /// </summary>
            public override bool IsFixedSize {
                get { return _list.IsFixedSize; }
            }

            /// <summary>
            /// 获取一个值，该值指示是否同步对 ArrayList 的访问（线程安全）。
            /// SyncArrayList支持同步访问，返回为true
            /// </summary>
            public override bool IsSynchronized { 
                get { return true; }
            }
            
            /// <summary>
            /// 获取或设置位于指定索引处的元素。（支持同步访问）
            /// </summary>
            /// <param name="index">索引</param>
            /// <returns>当前索引出对象</returns>
             public override Object this[int index] {
                get {
                    lock(_root) {
                        return _list[index];
                    }
                }
                set {
                    lock(_root) {
                        _list[index] = value;
                    }
                }
            }
    
            /// <summary>
            /// 获取同步对象
            /// </summary>
            public override Object SyncRoot {
                get { return _root; }
            }
    
            /// <summary>
            /// 将给定对象添加到这个列表中。列表的大小增加1。如果需要,列表的能力是添加新元素之前翻了一倍。
            /// </summary>
            /// <param name="value">添加对象</param>
            /// <returns>返回实际包含大小</returns>
            public override int Add(Object value) {
                lock(_root) {
                    return _list.Add(value);
                }
            }
    
            /// <summary>
            /// 添加 ICollection 的元素到 ArrayList 的末尾。
            /// </summary>
            /// <param name="c">集合</param>
            public override void AddRange(ICollection c) {
                lock(_root) {
                    _list.AddRange(c);
                }
            }

            /// <summary>
            /// 使用默认比较器搜索整个排序数组列表的一个元素并返回元素的从零开始的索引。
            /// </summary>
            /// <param name="value">搜索元素对象</param>
            /// <returns></returns>
            public override int BinarySearch(Object value) {
                lock(_root) {
                    return _list.BinarySearch(value);
                }
            }

            /// <summary>
            /// 使用给定比较器搜索整个排序数组列表的一个元素并返回元素的从零开始的索引。
            /// </summary>
            /// <param name="value">搜索元素对象</param>
            /// <param name="comparer">比较器</param>
            /// <returns></returns>
            public override int BinarySearch(Object value, IComparer comparer) {
                lock(_root) {
                    return _list.BinarySearch(value, comparer);
                }
            }

            /// <summary>
            /// 使用指定的比较器在整个已排序的 ArrayList 中搜索元素，并返回该元素从零开始的索引。
            /// </summary>
            /// <param name="index">索引</param>
            /// <param name="count">总数</param>
            /// <param name="value">值</param>
            /// <param name="comparer">比较器</param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
                lock(_root) {
                    return _list.BinarySearch(index, count, value, comparer);
                }
            }

            /// <summary>
            /// 从 ArrayList 中移除所有元素。
            /// </summary>
            public override void Clear() {
                lock(_root) {
                    _list.Clear();
                }
            }

            /// <summary>
            /// 创建一个ArrayList的浅拷贝。(即只复制引用)
            /// </summary>
            /// <returns></returns>
            public override Object Clone() {
                lock(_root) {
                    return new SyncArrayList((ArrayList)_list.Clone());
                }
            }
    
            /// <summary>
            /// 确定某元素是否在 ArrayList 中。
            /// </summary>
            /// <param name="item">元素对象</param>
            /// <returns></returns>
            public override bool Contains(Object item) {
                lock(_root) {
                    return _list.Contains(item);
                }
            }
    
            /// <summary>
            /// 从目标数组的开头开始，将整个 ArrayList 复制到一维兼容 Array。
            /// </summary>
            /// <param name="array"></param>
            public override void CopyTo(Array array) {
                lock(_root) {
                    _list.CopyTo(array);
                }
            }

            /// <summary>
            ///  整个数组列表复制到一个兼容的一维数组,从目标的指定索引数组。
            /// </summary>
            /// <param name="array"></param>
            /// <param name="index"></param>
            public override void CopyTo(Array array, int index) {
                lock(_root) {
                    _list.CopyTo(array, index);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void CopyTo(int index, Array array, int arrayIndex, int count) {
                lock(_root) {
                    _list.CopyTo(index, array, arrayIndex, count);
                }
            }
    
            public override IEnumerator GetEnumerator() {
                lock(_root) {
                    return _list.GetEnumerator();
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override IEnumerator GetEnumerator(int index, int count) {
                lock(_root) {
                    return _list.GetEnumerator(index, count);
                }
            }
    
            public override int IndexOf(Object value) {
                lock(_root) {
                    return _list.IndexOf(value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex) {
                lock(_root) {
                    return _list.IndexOf(value, startIndex);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex, int count) {
                lock(_root) {
                    return _list.IndexOf(value, startIndex, count);
                }
            }
    
            public override void Insert(int index, Object value) {
                lock(_root) {
                    _list.Insert(index, value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void InsertRange(int index, ICollection c) {
                lock(_root) {
                    _list.InsertRange(index, c);
                }
            }
    
            public override int LastIndexOf(Object value) {
                lock(_root) {
                    return _list.LastIndexOf(value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex) {
                lock(_root) {
                    return _list.LastIndexOf(value, startIndex);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex, int count) {
                lock(_root) {
                    return _list.LastIndexOf(value, startIndex, count);
                }
            }
    
            public override void Remove(Object value) {
                lock(_root) {
                    _list.Remove(value);
                }
            }
    
            public override void RemoveAt(int index) {
                lock(_root) {
                    _list.RemoveAt(index);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void RemoveRange(int index, int count) {
                lock(_root) {
                    _list.RemoveRange(index, count);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Reverse(int index, int count) {
                lock(_root) {
                    _list.Reverse(index, count);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void SetRange(int index, ICollection c) {
                lock(_root) {
                    _list.SetRange(index, c);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override ArrayList GetRange(int index, int count) {
                lock(_root) {
                    return _list.GetRange(index, count);
                }
            }

            public override void Sort() {
                lock(_root) {
                    _list.Sort();
                }
            }
    
            public override void Sort(IComparer comparer) {
                lock(_root) {
                    _list.Sort(comparer);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Sort(int index, int count, IComparer comparer) {
                lock(_root) {
                    _list.Sort(index, count, comparer);
                }
            }
    
            public override Object[] ToArray() {
                lock(_root) {
                    return _list.ToArray();
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override Array ToArray(Type type) {
                lock(_root) {
                    return _list.ToArray(type);
                }
            }
    
            public override void TrimToSize() {
                lock(_root) {
                    _list.TrimToSize();
                }
            }
        }
    
    
        /// <summary>
        /// 同步IList(支持序列化)
        /// </summary>
        [Serializable]
        private class SyncIList : IList
        {
            private IList _list;
            private Object _root;//同步锁对象
    
            internal SyncIList(IList list) {
                _list = list;
                _root = list.SyncRoot;
            }
    
            public virtual int Count { 
                get { lock(_root) { return _list.Count; } }
            }
    
            public virtual bool IsReadOnly {
                get { return _list.IsReadOnly; }
            }

            public virtual bool IsFixedSize {
                get { return _list.IsFixedSize; }
            }

            
            public virtual bool IsSynchronized { 
                get { return true; }
            }
            
             public virtual Object this[int index] {
                get {
                    lock(_root) {
                        return _list[index];
                    }
                }
                set {
                    lock(_root) {
                        _list[index] = value;
                    }
                }
            }
    
            public virtual Object SyncRoot {
                get { return _root; }
            }
    
            public virtual int Add(Object value) {
                lock(_root) {
                    return _list.Add(value);
                }
            }
                 
    
            public virtual void Clear() {
                lock(_root) {
                    _list.Clear();
                }
            }
    
            public virtual bool Contains(Object item) {
                lock(_root) {
                    return _list.Contains(item);
                }
            }
    
            public virtual void CopyTo(Array array, int index) {
                lock(_root) {
                    _list.CopyTo(array, index);
                }
            }
    
            public virtual IEnumerator GetEnumerator() {
                lock(_root) {
                    return _list.GetEnumerator();
                }
            }
    
            public virtual int IndexOf(Object value) {
                lock(_root) {
                    return _list.IndexOf(value);
                }
            }
    
            public virtual void Insert(int index, Object value) {
                lock(_root) {
                    _list.Insert(index, value);
                }
            }
    
            public virtual void Remove(Object value) {
                lock(_root) {
                    _list.Remove(value);
                }
            }
    
            public virtual void RemoveAt(int index) {
                lock(_root) {
                    _list.RemoveAt(index);
                }
            }
        }
    
        /// <summary>
        /// 固定大小List（不允许增加或删除，只允许修改，支持序列化）
        /// </summary>
        [Serializable]
        private class FixedSizeList : IList
        {
            private IList _list;
    
            internal FixedSizeList(IList l) {
                _list = l;
            }
    
            public virtual int Count { 
                get { return _list.Count; }
            }
    
            public virtual bool IsReadOnly {
                get { return _list.IsReadOnly; }
            }

            public virtual bool IsFixedSize {
                get { return true; }
            }

            public virtual bool IsSynchronized { 
                get { return _list.IsSynchronized; }
            }
            
             public virtual Object this[int index] {
                get {
                    return _list[index];
                }
                set {
                    _list[index] = value;
                }
            }
    
            public virtual Object SyncRoot {
                get { return _list.SyncRoot; }
            }
            
            /// <summary>
            /// 增加元素对象
            /// </summary>
            /// <param name="obj">元素对象</param>
            /// <returns>返回异常</returns>
            /// <exception cref="NotSupportedException">不支持增加</exception>
            public virtual int Add(Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
        
            /// <summary>
            /// 将移除IList内容
            /// </summary>
            /// <exception cref="NotSupportedException">不支持移除</exception>
            public virtual void Clear() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public virtual bool Contains(Object obj) {
                return _list.Contains(obj);
            }
    
            public virtual void CopyTo(Array array, int index) {
                _list.CopyTo(array, index);
            }
    
            public virtual IEnumerator GetEnumerator() {
                return _list.GetEnumerator();
            }
    
            public virtual int IndexOf(Object value) {
                return _list.IndexOf(value);
            }
    
            public virtual void Insert(int index, Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public virtual void Remove(Object value) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public virtual void RemoveAt(int index) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
        }

        /// <summary>
        ///  固定大小ArrayList（不允许增加或删除，只允许修改，支持序列化）
        /// </summary>
        [Serializable]
        private class FixedSizeArrayList : ArrayList
        {
            private ArrayList _list;
    
            internal FixedSizeArrayList(ArrayList l) {
                _list = l;
                _version = _list._version;
            }
    
            public override int Count { 
                get { return _list.Count; }
            }
    
            public override bool IsReadOnly {
                get { return _list.IsReadOnly; }
            }

            /// <summary>
            /// 是否是固定大小的（返回为真）
            /// </summary>
            public override bool IsFixedSize {
                get { return true; }
            }

            public override bool IsSynchronized { 
                get { return _list.IsSynchronized; }
            }
            
             public override Object this[int index] {
                get {
                    return _list[index];
                }
                set {
                    _list[index] = value;
                    _version = _list._version;
                }
            }
    
            public override Object SyncRoot {
                get { return _list.SyncRoot; }
            }
            
            public override int Add(Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public override void AddRange(ICollection c) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
                return _list.BinarySearch(index, count, value, comparer);
            }

            public override int Capacity {
                get { return _list.Capacity; }
                [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
                set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection")); }
            }

            public override void Clear() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public override Object Clone() {
                FixedSizeArrayList arrayList = new FixedSizeArrayList(_list);
                arrayList._list = (ArrayList)_list.Clone();
                return arrayList;
            }

            public override bool Contains(Object obj) {
                return _list.Contains(obj);
            }
    
            public override void CopyTo(Array array, int index) {
                _list.CopyTo(array, index);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void CopyTo(int index, Array array, int arrayIndex, int count) {
                _list.CopyTo(index, array, arrayIndex, count);
            }

            public override IEnumerator GetEnumerator() {
                return _list.GetEnumerator();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override IEnumerator GetEnumerator(int index, int count) {
                return _list.GetEnumerator(index, count);
            }

            public override int IndexOf(Object value) {
                return _list.IndexOf(value);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex) {
                return _list.IndexOf(value, startIndex);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex, int count) {
                return _list.IndexOf(value, startIndex, count);
            }
    
            /// <summary>
            /// 插入元素对象
            /// </summary>
            /// <param name="index">要插入对象的索引</param>
            /// <param name="obj">对象元素</param>
            public override void Insert(int index, Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void InsertRange(int index, ICollection c) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }

            public override int LastIndexOf(Object value) {
                return _list.LastIndexOf(value);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex) {
                return _list.LastIndexOf(value, startIndex);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex, int count) {
                return _list.LastIndexOf(value, startIndex, count);
            }

            public override void Remove(Object value) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
    
            public override void RemoveAt(int index) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void RemoveRange(int index, int count) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void SetRange(int index, ICollection c) {
                _list.SetRange(index, c);
                _version = _list._version;
            }

            public override ArrayList GetRange(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                return new Range(this,index, count);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Reverse(int index, int count) {
                _list.Reverse(index, count);
                _version = _list._version;
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Sort(int index, int count, IComparer comparer) {
                _list.Sort(index, count, comparer);
                _version = _list._version;
            }

            public override Object[] ToArray() {
                return _list.ToArray();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override Array ToArray(Type type) {
                return _list.ToArray(type);
            }
    
            public override void TrimToSize() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
            }
        }
    
        /// <summary>
        /// 只读IList
        /// </summary>
        [Serializable]
        private class ReadOnlyList : IList
        {
            private IList _list;
    
            /// <summary>
            /// 只能在同一程序集中访问的构造函数
            /// </summary>
            /// <param name="l"></param>
            internal ReadOnlyList(IList l) {
                _list = l;
            }
    
            /// <summary>
            /// 获取 IList 中实际包含的元素数。
            /// </summary>
            public virtual int Count { 
                get { return _list.Count; }
            }
    
            /// <summary>
            /// 获取一个值用于判断IList是否为只读
            /// 返回为true
            /// </summary>
            public virtual bool IsReadOnly {
                get { return true; }
            }

            public virtual bool IsFixedSize {
                get { return true; }
            }

            /// <summary>
            /// 获取一个值用于判断是否支持同步访问（线程安全）
            /// </summary>
            public virtual bool IsSynchronized { 
                get { return _list.IsSynchronized; }
            }
            
             public virtual Object this[int index] {
                get {
                    return _list[index];
                }
                set {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
                }
            }
    
            public virtual Object SyncRoot {
                get { return _list.SyncRoot; }
            }
            
            public virtual int Add(Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public virtual void Clear() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public virtual bool Contains(Object obj) {
                return _list.Contains(obj);
            }
            
            public virtual void CopyTo(Array array, int index) {
                _list.CopyTo(array, index);
            }
    
            public virtual IEnumerator GetEnumerator() {
                return _list.GetEnumerator();
            }
    
            public virtual int IndexOf(Object value) {
                return _list.IndexOf(value);
            }
    
            public virtual void Insert(int index, Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            public virtual void Remove(Object value) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public virtual void RemoveAt(int index) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
        }

        [Serializable]
        private class ReadOnlyArrayList : ArrayList
        {
            private ArrayList _list;
    
            internal ReadOnlyArrayList(ArrayList l) {
                _list = l;
            }
    
            public override int Count { 
                get { return _list.Count; }
            }
    
            public override bool IsReadOnly {
                get { return true; }
            }

            public override bool IsFixedSize {
                get { return true; }
            }

            public override bool IsSynchronized { 
                get { return _list.IsSynchronized; }
            }
            
             public override Object this[int index] {
                get {
                    return _list[index];
                }
                set {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
                }
            }
    
            public override Object SyncRoot {
                get { return _list.SyncRoot; }
            }
            
            public override int Add(Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public override void AddRange(ICollection c) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int BinarySearch(int index, int count, Object value, IComparer comparer) {
                return _list.BinarySearch(index, count, value, comparer);
            }


            public override int Capacity {
                get { return _list.Capacity; }
                [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
                set { throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection")); }
            }

            public override void Clear() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            public override Object Clone() {
                ReadOnlyArrayList arrayList = new ReadOnlyArrayList(_list);
                arrayList._list = (ArrayList)_list.Clone();
                return arrayList;
            }
    
            public override bool Contains(Object obj) {
                return _list.Contains(obj);
            }
    
            public override void CopyTo(Array array, int index) {
                _list.CopyTo(array, index);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void CopyTo(int index, Array array, int arrayIndex, int count) {
                _list.CopyTo(index, array, arrayIndex, count);
            }

            public override IEnumerator GetEnumerator() {
                return _list.GetEnumerator();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override IEnumerator GetEnumerator(int index, int count) {
                return _list.GetEnumerator(index, count);
            }

            public override int IndexOf(Object value) {
                return _list.IndexOf(value);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex) {
                return _list.IndexOf(value, startIndex);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int IndexOf(Object value, int startIndex, int count) {
                return _list.IndexOf(value, startIndex, count);
            }
    
            public override void Insert(int index, Object obj) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void InsertRange(int index, ICollection c) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            public override int LastIndexOf(Object value) {
                return _list.LastIndexOf(value);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex) {
                return _list.LastIndexOf(value, startIndex);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex, int count) {
                return _list.LastIndexOf(value, startIndex, count);
            }

            public override void Remove(Object value) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public override void RemoveAt(int index) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void RemoveRange(int index, int count) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void SetRange(int index, ICollection c) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
    
            public override ArrayList GetRange(int index, int count) {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index<0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (Count - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                return new Range(this,index, count);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Reverse(int index, int count) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void Sort(int index, int count, IComparer comparer) {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }

            public override Object[] ToArray() {
                return _list.ToArray();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override Array ToArray(Type type) {
                return _list.ToArray(type);
            }
    
            public override void TrimToSize() {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));
            }
        }

        /// <summary>
        /// 实现了一个ArrayList的枚举器。枚举器使用的内部版本号列表,以确保没有修改虽然枚举列表。
        /// </summary>
        [Serializable]
        private sealed class ArrayListEnumerator : IEnumerator, ICloneable
        {
            private ArrayList list;
            private int index;
            private int endIndex;       // Where to stop.
            private int version;
            private Object currentElement;
            private int startIndex;     // Save this for Reset.
    
            /// <summary>
            /// 将ArrayList转换为枚举
            /// </summary>
            /// <param name="list">用于转换的ArrayList</param>
            /// <param name="index">起始索引</param>
            /// <param name="count">转换数目</param>
            internal ArrayListEnumerator(ArrayList list, int index, int count) {
                this.list = list;
                startIndex = index;
                this.index = index - 1;
                endIndex = this.index + count;  // last valid index
                version = list._version;
                currentElement = null;
            }

            /// <summary>
            /// 返回一个ArrayListEnumerator对象
            /// </summary>
            /// <returns></returns>
            public Object Clone() {
                return MemberwiseClone();
            }
    
            /// <summary>
            /// 将枚举数推进到集合的下一个元素。
            /// </summary>
            /// <returns>如果枚举数已成功地推进到下一个元素，则为 true；如果枚举数传递到集合的末尾，则为 false。</returns>
            public bool MoveNext() {
                if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                if (index < endIndex) {//判断索引是否出界，如果未出界，则设置当前访问索引和当前对象
                    currentElement = list[++index];
                    return true;
                }
                else {
                    index = endIndex + 1;
                }
                
                return false;
            }
    
            /// <summary>
            /// 获取集合中的当前元素。
            /// </summary>
            public Object Current {
                get {
                    if (index < startIndex) 
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    else if (index > endIndex) {
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    }
                    return currentElement;
                }
            }
       
            public void Reset() {
                if (version != list._version) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                index = startIndex - 1;                
            }
        }

        /// <summary>
        ///  实现一个通用的列表的子范围。这个类的一个实例是List.GetRange返回的默认实现
        /// </summary>
        [Serializable]
        private class Range: ArrayList
        {
            /// <summary>
            /// 基类ArrayList
            /// </summary>
            private ArrayList _baseList;
            /// <summary>
            /// 基类索引
            /// </summary>
            private int _baseIndex;

            /// <summary>
            /// 基类大小
            /// </summary>
            [ContractPublicPropertyName("Count")]
            private int _baseSize;
            /// <summary>
            /// 基类版本
            /// </summary>
            private int _baseVersion;
                 
            /// <summary>
            /// 通过ArrayList构造内部子返回
            /// </summary>
            /// <param name="list">构造对象</param>
            /// <param name="index">起始索引</param>
            /// <param name="count">总数</param>
            internal Range(ArrayList list, int index, int count) : base(false) {
                _baseList = list;
                _baseIndex = index;
                _baseSize = count;
                _baseVersion = list._version;
                // we also need to update _version field to make Range of Range work
                _version = list._version;                
            }

            /// <summary>
            /// 内部更新ArrayList子范围
            /// </summary>
            /// <exception cref="InvalidOperationException">InvalidOperation_UnderlyingArrayListChanged</exception>
            private void InternalUpdateRange()
            {
                if (_baseVersion != _baseList._version)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnderlyingArrayListChanged"));
            }

            /// <summary>
            /// 更新版本（包括子集合，ArrayList版本）
            /// </summary>
            private void InternalUpdateVersion() {
                _baseVersion++;
                _version++;                
            }
            
            /// <summary>
            /// 将对象添加到范围内
            /// </summary>
            /// <param name="value">元素对象</param>
            /// <returns></returns>
            public override int Add(Object value) {
                InternalUpdateRange();//更新集合
                _baseList.Insert(_baseIndex + _baseSize, value);//调用基类Insert函数
                InternalUpdateVersion();//更新版本
                return _baseSize++;
            }

            /// <summary>
            /// 将集合插入到子范围内()
            /// </summary>
            /// <param name="c">集合</param>
            public override void AddRange(ICollection c) {
                if( c ==  null ) {
                    throw new ArgumentNullException("c");
                }
                Contract.EndContractBlock();

                InternalUpdateRange();
                int count = c.Count;
                if( count > 0) {
                    _baseList.InsertRange(_baseIndex + _baseSize, c);//调用ArrayList.InsertRange
                    InternalUpdateVersion();
                    _baseSize += count;
                }
            }

            #region 重载其他ArrayList函数
            public override int BinarySearch(int index, int count, Object value, IComparer comparer)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();
                InternalUpdateRange();

                int i = _baseList.BinarySearch(_baseIndex + index, count, value, comparer);
                if (i >= 0) return i - _baseIndex;
                return i + _baseIndex;
            }

            public override int Capacity
            {
                get
                {
                    return _baseList.Capacity;
                }

                set
                {
                    if (value < Count) throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                    Contract.EndContractBlock();
                }
            }


            public override void Clear()
            {
                InternalUpdateRange();
                if (_baseSize != 0)
                {
                    _baseList.RemoveRange(_baseIndex, _baseSize);
                    InternalUpdateVersion();
                    _baseSize = 0;
                }
            }

            public override Object Clone()
            {
                InternalUpdateRange();
                Range arrayList = new Range(_baseList, _baseIndex, _baseSize);
                arrayList._baseList = (ArrayList)_baseList.Clone();
                return arrayList;
            }

            public override bool Contains(Object item)
            {
                InternalUpdateRange();
                if (item == null)
                {
                    for (int i = 0; i < _baseSize; i++)
                        if (_baseList[_baseIndex + i] == null)
                            return true;
                    return false;
                }
                else
                {
                    for (int i = 0; i < _baseSize; i++)
                        if (_baseList[_baseIndex + i] != null && _baseList[_baseIndex + i].Equals(item))
                            return true;
                    return false;
                }
            }

            public override void CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (array.Length - index < _baseSize)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.CopyTo(_baseIndex, array, index, _baseSize);
            }

            public override void CopyTo(int index, Array array, int arrayIndex, int count)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (array.Length - arrayIndex < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.CopyTo(_baseIndex + index, array, arrayIndex, count);
            }

            public override int Count
            {
                get
                {
                    InternalUpdateRange();
                    return _baseSize;
                }
            }

            public override bool IsReadOnly
            {
                get { return _baseList.IsReadOnly; }
            }

            public override bool IsFixedSize
            {
                get { return _baseList.IsFixedSize; }
            }

            public override bool IsSynchronized
            {
                get { return _baseList.IsSynchronized; }
            }

            public override IEnumerator GetEnumerator()
            {
                return GetEnumerator(0, _baseSize);
            }

            public override IEnumerator GetEnumerator(int index, int count)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                return _baseList.GetEnumerator(_baseIndex + index, count);
            }

            public override ArrayList GetRange(int index, int count)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                return new Range(this, index, count);
            }

            public override Object SyncRoot
            {
                get
                {
                    return _baseList.SyncRoot;
                }
            }


            public override int IndexOf(Object value)
            {
                InternalUpdateRange();
                int i = _baseList.IndexOf(value, _baseIndex, _baseSize);
                if (i >= 0) return i - _baseIndex;
                return -1;
            }

            public override int IndexOf(Object value, int startIndex)
            {
                if (startIndex < 0)
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (startIndex > _baseSize)
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                int i = _baseList.IndexOf(value, _baseIndex + startIndex, _baseSize - startIndex);
                if (i >= 0) return i - _baseIndex;
                return -1;
            }

            public override int IndexOf(Object value, int startIndex, int count)
            {
                if (startIndex < 0 || startIndex > _baseSize)
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));

                if (count < 0 || (startIndex > _baseSize - count))
                    throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                int i = _baseList.IndexOf(value, _baseIndex + startIndex, count);
                if (i >= 0) return i - _baseIndex;
                return -1;
            }

            public override void Insert(int index, Object value)
            {
                if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.Insert(_baseIndex + index, value);
                InternalUpdateVersion();
                _baseSize++;
            }

            public override void InsertRange(int index, ICollection c)
            {
                if (index < 0 || index > _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (c == null)
                {
                    throw new ArgumentNullException("c");
                }
                Contract.EndContractBlock();

                InternalUpdateRange();
                int count = c.Count;
                if (count > 0)
                {
                    _baseList.InsertRange(_baseIndex + index, c);
                    _baseSize += count;
                    InternalUpdateVersion();
                }
            }

            public override int LastIndexOf(Object value)
            {
                InternalUpdateRange();
                int i = _baseList.LastIndexOf(value, _baseIndex + _baseSize - 1, _baseSize);
                if (i >= 0) return i - _baseIndex;
                return -1;
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex)
            {
                return LastIndexOf(value, startIndex, startIndex + 1);
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override int LastIndexOf(Object value, int startIndex, int count)
            {
                InternalUpdateRange();
                if (_baseSize == 0)
                    return -1;

                if (startIndex >= _baseSize)
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                if (startIndex < 0)
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));

                int i = _baseList.LastIndexOf(value, _baseIndex + startIndex, count);
                if (i >= 0) return i - _baseIndex;
                return -1;
            }

            // Don't need to override Remove

            public override void RemoveAt(int index)
            {
                if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.RemoveAt(_baseIndex + index);
                InternalUpdateVersion();
                _baseSize--;
            }

            public override void RemoveRange(int index, int count)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                // RemoveRange如果计数为0，不需要调用_bastList。
                // 此外,如果计数为0，baseList不会改变vresion数量
                if (count > 0)
                {
                    _baseList.RemoveRange(_baseIndex + index, count);
                    InternalUpdateVersion();
                    _baseSize -= count;
                }
            }

            public override void Reverse(int index, int count)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.Reverse(_baseIndex + index, count);
                InternalUpdateVersion();
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Skip extra error checking to avoid *potential* AppCompat problems.
            public override void SetRange(int index, ICollection c)
            {
                InternalUpdateRange();
                if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                _baseList.SetRange(_baseIndex + index, c);
                if (c.Count > 0)
                {
                    InternalUpdateVersion();
                }
            }

            public override void Sort(int index, int count, IComparer comparer)
            {
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                if (_baseSize - index < count)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                Contract.EndContractBlock();

                InternalUpdateRange();
                _baseList.Sort(_baseIndex + index, count, comparer);
                InternalUpdateVersion();
            }

            public override Object this[int index]
            {
                get
                {
                    InternalUpdateRange();
                    if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                    return _baseList[_baseIndex + index];
                }
                set
                {
                    InternalUpdateRange();
                    if (index < 0 || index >= _baseSize) throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                    _baseList[_baseIndex + index] = value;
                    InternalUpdateVersion();
                }
            }

            public override Object[] ToArray()
            {
                InternalUpdateRange();
                Object[] array = new Object[_baseSize];
                Array.Copy(_baseList._items, _baseIndex, array, 0, _baseSize);
                return array;
            }

            [SecuritySafeCritical]
            public override Array ToArray(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException("type");
                Contract.EndContractBlock();

                InternalUpdateRange();
                Array array = Array.UnsafeCreateInstance(type, _baseSize);
                _baseList.CopyTo(_baseIndex, array, 0, _baseSize);
                return array;
            }

            public override void TrimToSize()
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RangeCollection"));
            } 
            #endregion
        }

        [Serializable]
        private sealed class ArrayListEnumeratorSimple : IEnumerator, ICloneable {
            private ArrayList list;
            private int index;
            private int version;
            private Object currentElement;
            [NonSerialized]
            private bool isArrayList;
            // this object is used to indicate enumeration has not started or has terminated
            static Object dummyObject = new Object();  
                            
            internal ArrayListEnumeratorSimple(ArrayList list) {
                this.list = list;
                this.index = -1;
                version = list._version;
                isArrayList = (list.GetType() == typeof(ArrayList));
                currentElement = dummyObject;                
            }
            
            public Object Clone() {
                return MemberwiseClone();
            }
    
            public bool MoveNext() {
                if (version != list._version) {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                }

                if( isArrayList) {  // avoid calling virtual methods if we are operating on ArrayList to improve performance
                    if (index < list._size - 1) {
                        currentElement = list._items[++index];
                        return true;
                    }
                    else {
                        currentElement = dummyObject;
                        index =list._size;
                        return false;
                    }                    
                }
                else {                    
                    if (index < list.Count - 1) {
                        currentElement = list[++index];
                        return true;
                    }
                    else {
                        index = list.Count;
                        currentElement = dummyObject;
                        return false;
                    }
                }
            }
    
            public Object Current {
                get {
                    object temp = currentElement;
                    if(dummyObject == temp) { // check if enumeration has not started or has terminated
                        if (index == -1) {
                            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                        }
                        else {                    
                            throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));                        
                        }
                    }

                    return temp;
                }
            }
    
            public void Reset() {
                if (version != list._version) {
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                }    
                
                currentElement = dummyObject;
                index = -1;
            }
        }

        internal class ArrayListDebugView {
            private ArrayList arrayList; 
        
            public ArrayListDebugView( ArrayList arrayList) {
                if( arrayList == null)
                    throw new ArgumentNullException("arrayList");

                this.arrayList = arrayList;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Object[] Items { 
                get {
                    return arrayList.ToArray();
                }
            }
        }
    }    
}
