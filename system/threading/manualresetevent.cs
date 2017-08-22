// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// <OWNER>[....]</OWNER>
/*=============================================================================
**
** Class: ManualResetEvent
**
**
** Purpose: An example of a WaitHandle class
**
**
=============================================================================*/
namespace System.Threading {
    
    using System;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;

    /// <summary>
    /// 通知一个或多个正在等待的线程已发生事件。此类不能被继承。
    /// </summary>
    [HostProtection(Synchronization=true, ExternalThreading=true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ManualResetEvent : EventWaitHandle
    {        
        public ManualResetEvent(bool initialState) : base(initialState,EventResetMode.ManualReset){}
    }
}
    
