// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//  PermissionState.cs
// 
// <OWNER>ShawnFa</OWNER>
//
//   
//      可以在完全受限或完全不受限状态下创建权限。 完全受限状态不允许对资源进行任何访问，完全不受限状态允许对特定资源进行所有访问。 例如，文件权限构造函数可以创建一个对象，该对象表示不能对任何文件进行任何访问或可对全部文件进行所有访问。
//      每个权限类型均明确定义了极端的状态，表示该类型中可表现的所有权限或没有任何权限。 因此，可以在完全受限或完全不受限状态下创建不具有特定权限信息的一般权限；但是，中间状态只能根据特定的权限语义进行设置。
//      在 .NET Framework 中实现的所有代码访问权限可将 PermissionState 值作为其构造函数的参数。
//

namespace System.Security.Permissions {
    
    using System;
    /// <summary>
    /// 指定权限在创建时是否对资源有所有访问权限或没有任何访问权限。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum PermissionState
    {
        /// <summary>
        /// 可以对该权限所保护的资源进行完全访问。
        /// </summary>
        Unrestricted = 1,
        /// <summary>
        /// 不能对该权限所保护的资源进行访问。
        /// </summary>
        None = 0,
    } 
    
}
