// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface: IHashCodeProvider
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: A bunch of strings.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    /// <summary>
    /// 一个哈希表的用户提供了一种机制,覆盖默认GetHashCode方法()函数对象,提供自己的哈希函数。
    /// </summary>
    [Obsolete("Please use IEqualityComparer instead.")]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IHashCodeProvider 
    {
        // Interfaces are not serializable
        // Returns a hash code for the given object.  
        // 
        int GetHashCode (Object obj);
    }
}
