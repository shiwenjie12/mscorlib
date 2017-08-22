// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IDictionary
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for all dictionaries.
**
** 
===========================================================*/
namespace System.Collections {
    using System;
    using System.Diagnostics.Contracts;


    // 一个IDictionary可能无序的一组键值对。
    // 键可以是任何非空对象。值可以是任何对象。
    // 你可以查找一个值在一个IDictionary通过默认索引属性,物品。
#if CONTRACTS_FULL
    [ContractClass(typeof(IDictionaryContract))]
#endif // CONTRACTS_FULL
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IDictionary : ICollection
    {
        // 接口是不能序列化的
        // 项目属性提供了方法在字典里阅读和编辑条目。

        Object this[Object key] {
            get;
            set;
        }
    
        // Returns a collections of the keys in this dictionary.
        ICollection Keys {
            get;
        }
    
        // Returns a collections of the values in this dictionary.
        ICollection Values {
            get;
        }
    
        // Returns whether this dictionary contains a particular key.
        //
        bool Contains(Object key);
    
        // Adds a key-value pair to the dictionary.
        // 
        void Add(Object key, Object value);
    
        // Removes all pairs from the dictionary.
        void Clear();
    
        bool IsReadOnly 
        { get; }

        bool IsFixedSize
        { get; }

        // Returns an IDictionaryEnumerator for this dictionary.
        new IDictionaryEnumerator GetEnumerator();
    
        // Removes a particular key from the dictionary.
        //
        void Remove(Object key);
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(IDictionary))]
    internal abstract class IDictionaryContract : IDictionary
    {
        Object IDictionary.this[Object key] {
            get { return default(Object); }
            set { }
        }

        ICollection IDictionary.Keys {
            get {
                Contract.Ensures(Contract.Result<ICollection>() != null);
                //Contract.Ensures(Contract.Result<ICollection>().Count == ((ICollection)this).Count);
                return default(ICollection);
            }
        }

        ICollection IDictionary.Values {
            get {
                Contract.Ensures(Contract.Result<ICollection>() != null);
                return default(ICollection);
            }
        }

        bool IDictionary.Contains(Object key)
        {
            return default(bool);
        }

        void IDictionary.Add(Object key, Object value)
        {
        }

        void IDictionary.Clear()
        {
        }

        bool IDictionary.IsReadOnly {
            get { return default(bool); }
        }

        bool IDictionary.IsFixedSize { 
            get { return default(bool); }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IDictionaryEnumerator>() != null);
            return default(IDictionaryEnumerator);
        }

        void IDictionary.Remove(Object key)
        {
        }

        #region ICollection members

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count { 
            get {
                return default(int);
            }
        }

        Object ICollection.SyncRoot {
            get {
                return default(Object);
            }
        }

        bool ICollection.IsSynchronized {
            get { return default(bool); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default(IEnumerator);
        }

        #endregion ICollection Members
    }
#endif // CONTRACTS_FULL
}
