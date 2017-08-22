// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** File:    AssemblyVersionCompatibility
**
** <EMAIL>Author:  Suzanne Cook</EMAIL>
**
** Purpose: defining the different flavor's assembly version compatibility
**
** Date:    June 4, 1999
**
===========================================================*/
namespace System.Configuration.Assemblies {
    
    using System;
    /// <summary>
    /// 定义不同类型程序集版本的兼容性。.NET Framework 1.0 版中没有提供这项功能。
    /// </summary>
     [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum AssemblyVersionCompatibility
    {
         /// <summary>
        /// 该程序集无法与其他版本在同一台计算机上一起执行。
         /// </summary>
        SameMachine         = 1,
         /// <summary>
        /// 程序集无法与其他版本在同一进程中一起执行。
         /// </summary>
        SameProcess         = 2,
         /// <summary>
        /// 程序集无法与其他版本在同一应用程序域中一起执行。
         /// </summary>
        SameDomain          = 3,
    }
}
