// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IDictionaryEnumerator
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Base interface for dictionary enumerators.
**
** 
===========================================================*/
namespace System.Collections {
    // Interfaces are not serializable
    
    using System;
    // This interface represents an enumerator that allows sequential access to the
    // elements of a dictionary. Upon creation, an enumerator is conceptually
    // positioned before the first element of the enumeration. The first call to the
    // MoveNext method brings the first element of the enumeration into view,
    // and each successive call to MoveNext brings the next element into
    // view until MoveNext returns false, indicating that there are no more
    // elements to enumerate. Following each call to MoveNext, the
    // Key and Value methods are used to obtain the key and
    // value of the element currently in view. The values returned by calls to
    // Key and Value are undefined before the first call to
    // MoveNext and following a call to MoveNext that returned false.
    // Enumerators are typically used in while loops of the form
    // 
    // IDictionaryEnumerator e = ...;
    // while (e.MoveNext()) {
    //     Object key = e.Key;
    //     Object value = e.Value;
    //     ...
    // }
    // 
    // The IDictionaryEnumerator interface extends the IEnumerator
    // inerface and can thus be used as a regular enumerator. The Current 
    // method of an IDictionaryEnumerator returns a DictionaryEntry containing
    // the current key and value pair.  However, the GetEntry method will
    // return the same DictionaryEntry and avoids boxing the DictionaryEntry (boxing
    // is somewhat expensive).
    // IDictionaryEnumerator接口扩展了IEnumerator交互,因此可以用作常规的枚举器。
    // 当前方法的IDictionaryEnumerator返回包含当前DictionaryEntry键和值。
    // 然而,GetEntry方法将返回相同的DictionaryEntry并避免拳击DictionaryEntry(拳击有点贵)。
    /// <summary>
    /// 枚举非泛型字典的元素。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IDictionaryEnumerator : IEnumerator
    {
        // 返回当前元素的键的枚举。
        // 返回的值是未定义在第一次调用GetNext调用GetNext后,返回false。
        // 多个调用GetKey没有干预GetNext调用将返回相同的对象。
        /// <summary>
        /// 获取当前字典项的键。
        /// </summary>
        Object Key {
            get; 
        }
        
        // Returns the value of the current element of the enumeration. The
        // returned value is undefined before the first call to GetNext and
        // following a call to GetNext that returned false. Multiple calls
        // to GetValue with no intervening calls to GetNext will
        // return the same object.
        /// <summary>
        /// 获取当前字典项的值。
        /// </summary>
        Object Value {
            get;
        }
        
        // GetBlock will copy dictionary values into the given Array.  It will either
        // fill up the array, or if there aren't enough elements, it will
        // copy as much as possible into the Array.  The number of elements
        // copied is returned.
        /// <summary>
        /// 同时获取当前字典项的键和值。
        /// </summary>
        DictionaryEntry Entry {
            get; 
        }
    }
}
