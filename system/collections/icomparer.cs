// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface:  IComparer
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Interface for comparing two Objects.
**
** 
===========================================================*/
namespace System.Collections {
    
    using System;
    // 公开一种比较两个对象的方法。
    // 接口不能被序列化
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IComparer {

        /// <summary>
        /// 比较两个对象并返回一个值，该值指示一个对象小于、等于还是大于另一个对象。
        /// </summary>
        /// <param name="x">要比较的第一个对象。</param>
        /// <param name="y">要比较的第二个对象。</param>
        /// <returns>一个有符号整数，指示 x 与 y 的相对值，如下表所示。</returns>
        int Compare(Object x, Object y);
    }
}
