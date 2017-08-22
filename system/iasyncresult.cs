// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface: IAsyncResult
**
** Purpose: Interface to encapsulate the results of an async
**          operation
**
===========================================================*/
namespace System {
    
    using System;
    using System.Threading;
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IAsyncResult
    {
        /// <summary>
        /// 获取一个值，该值指示异步操作是否已完成。
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// 获取用于等待异步操作完成的 WaitHandle。
        /// </summary>
        WaitHandle AsyncWaitHandle { get; }

        /// <summary>
        /// 获取用户定义的对象，它限定或包含关于异步操作的信息。
        /// </summary>
        Object AsyncState      { get; }

        /// <summary>
        /// 获取一个值，该值指示异步操作是否同步完成。
        /// </summary>
        bool CompletedSynchronously { get; }
   
    
    }

}
