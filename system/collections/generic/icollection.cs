// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  ICollection
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all generic collections.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    using System;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    // 基于接口集合,定义枚举器,大小,和同步方法。

    // Note that T[] : IList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special hack internally though - see VM\compile.cpp.
    // The same attribute is on IEnumerable<T> and ICollection<T>.
    // 注意,T[]:IList < T >,我们想确保如果你使用IList < YourValueType >,我们确保YourValueType没有jit可以使用[]。
    // 因此,TypeDependencyAttribute SZArrayHelper。
    //这是一个特殊的内部攻击虽然——看到VM \ compile.cpp。相同的属性是IEnumerable < T >和ICollection < T >。
#if CONTRACTS_FULL
    [ContractClass(typeof(ICollectionContract<>))]
#endif
    [TypeDependencyAttribute("System.SZArrayHelper")]
    public interface ICollection<T> : IEnumerable<T>
    {
        /// <summary>
        ///  获取在集合中的条目数量  
        /// </summary>
        int Count { get; }
        /// <summary>
        /// 获取是否是只读
        /// </summary>
        bool IsReadOnly { get; }
        /// <summary>
        /// 将
        /// </summary>
        /// <param name="item"></param>
        void Add(T item);

        void Clear();

        bool Contains(T item); 
                
        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        // 
        void CopyTo(T[] array, int arrayIndex);
                
        //void CopyTo(int sourceIndex, T[] destinationArray, int destinationIndex, int count);

        bool Remove(T item);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(ICollection<>))]
    internal abstract class ICollectionContract<T> : ICollection<T>
    {
        int ICollection<T>.Count {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return default(int);
            }
        }

        bool ICollection<T>.IsReadOnly {
            get { return default(bool); }
        }

        void ICollection<T>.Add(T item)
        {
            //Contract.Ensures(((ICollection<T>)this).Count == Contract.OldValue(((ICollection<T>)this).Count) + 1);  // not threadsafe
        }

        void ICollection<T>.Clear()
        {
        }

        bool ICollection<T>.Contains(T item)
        {
            return default(bool);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
        }

        bool ICollection<T>.Remove(T item)
        {
            return default(bool);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return default(IEnumerator<T>);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }
    }
#endif // CONTRACTS_FULL
}
