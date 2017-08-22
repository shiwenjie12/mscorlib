// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// <OWNER>kimhamil</OWNER>
// 

namespace System.Collections.Generic {
    using System;

    // 通用IEqualityComparer接口实现方法,
    // 如果检查两个对象是相等的,为一个对象生成Hashcode。
    // 它是用字典类。
    /// <summary>
    /// 定义方法以支持对象的相等比较。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEqualityComparer<in T>
    {
        bool Equals(T x, T y);
        int GetHashCode(T obj);                
    }
}

