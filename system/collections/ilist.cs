// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IList
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all Lists.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    using System.Diagnostics.Contracts;

    // An IList is an ordered collection of objects.  The exact ordering
    // is up to the implementation of the list, ranging from a sorted
    // order to insertion order. 
    // 表示可按照索引单独访问的对象的非泛型集合。
#if CONTRACTS_FULL
    [ContractClass(typeof(IListContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IList : ICollection
    {
        /// <summary>
        /// 获取或设置位于指定索引处的元素。
        /// </summary>
        /// <param name="index">要获得或设置的元素从零开始的索引。</param>
        /// <returns>位于指定索引处的元素。</returns>
        Object this[int index] {
            get;
            set;
        }
    
        // Adds an item to the list.  The exact position in the list is 
        // implementation-dependent, so while ArrayList may always insert
        // in the last available location, a SortedList most likely would not.
        // The return value is the position the new element was inserted in.
        // 将一个条目添加到列表中。
        // 列表中的确切位置是随具体的,所以虽然ArrayList总是在最后一个可用的位置,插入一个SortedList最有可能不会。
        // 返回值是插入新元素的位置。
        /// <summary>
        /// 将某项添加到 IList 中。
        /// </summary>
        /// <param name="value">要添加到 IList 的对象。</param>
        /// <returns>新元素所插入到的位置，或为 -1 以指示未将该项插入到集合中。</returns>
        int Add(Object value);
    
        /// <summary>
        /// 确定 IList 是否包含特定值。
        /// </summary>
        /// <param name="value">要在 IList 中定位的对象。</param>
        /// <returns>如果在 IList 中找到 Object，则为 true；否则为 false。</returns>
        bool Contains(Object value);
    
        /// <summary>
        /// 从 IList 中移除所有项。
        /// </summary>
        void Clear();

        bool IsReadOnly 
        { get; }

    
        bool IsFixedSize
        {
            get;
        }

        /// <summary>
        /// 确定 IList 中特定项的索引。
        /// </summary>
        /// <param name="value">要在 IList 中定位的对象。</param>
        /// <returns>如果在列表中找到，则为 value 的索引；否则为 -1。</returns>
        int IndexOf(Object value);
    
        // Inserts value into the list at position index.
        // index must be non-negative and less than or equal to the 
        // number of elements in the list.  If index equals the number
        // of items in the list, then value is appended to the end.
        // 将值插入到列表的索引位置。索引必须是非负数和小于或等于在列表中元素的数量。如果指数等于列表中的条目的数量,然后值附加到结束。
        /// <summary>
        /// 将一个项插入指定索引处的 IList。
        /// </summary>
        /// <param name="index">从零开始的索引，应在该位置插入 value。</param>
        /// <param name="value">要插入 IList 的对象。</param>
        void Insert(int index, Object value);
    
        /// <summary>
        /// 从 IList 中移除特定对象的第一个匹配项。
        /// </summary>
        /// <param name="value"></param>
        void Remove(Object value);
    
        /// <summary>
        /// 移除指定索引处的 IList 项。
        /// </summary>
        /// <param name="index"></param>
        void RemoveAt(int index);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IList))]
    internal abstract class IListContract : IList
    {
        int IList.Add(Object value)
        {
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) + 1);  // Not threadsafe
            // This method should return the index in which an item was inserted, but we have
            // some internal collections that don't always insert items into the list, as well
            // as an MSDN sample code showing us returning -1.  Allow -1 to mean "did not insert".
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < ((IList)this).Count);
            return default(int);
        }

        Object IList.this[int index] {
            get {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((IList)this).Count);
                return default(int);
            }
            set {
                //Contract.Requires(index >= 0);
                //Contract.Requires(index < ((IList)this).Count);
            }
        }

        bool IList.IsFixedSize {
            get { return default(bool); }
        }

        bool IList.IsReadOnly {
            get { return default(bool); }
        }

        bool ICollection.IsSynchronized {
            get { return default(bool); }
        }

        void IList.Clear()
        {
            //Contract.Ensures(((IList)this).Count == 0  || ((IList)this).IsFixedSize);  // not threadsafe
        }

        bool IList.Contains(Object value)
        {
            return default(bool);
        }

        void ICollection.CopyTo(Array array, int startIndex)
        {
            //Contract.Requires(array != null);
            //Contract.Requires(startIndex >= 0);
            //Contract.Requires(startIndex + ((IList)this).Count <= array.Length);
        }

        int ICollection.Count {
            get {
                return default(int);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        [Pure]
        int IList.IndexOf(Object value)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < ((IList)this).Count);
            return default(int);
        }

        void IList.Insert(int index, Object value)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index <= ((IList)this).Count);  // For inserting immediately after the end.
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) + 1);  // Not threadsafe
        }

        void IList.Remove(Object value)
        {
            // No information if removal fails.
        }

        void IList.RemoveAt(int index)
        {
            //Contract.Requires(index >= 0);
            //Contract.Requires(index < ((IList)this).Count);
            //Contract.Ensures(((IList)this).Count == Contract.OldValue(((IList)this).Count) - 1);  // Not threadsafe
        }
        
        Object ICollection.SyncRoot {
            get {
                return default(Object);
            }
        }
    }
#endif // CONTRACTS_FULL
}
