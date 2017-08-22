// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Interface: AsyncCallbackDelegate
**
** Purpose: Type of callback for async operations
**
===========================================================*/
namespace System {

    /// <summary>
    /// 引用在相应异步操作完成时调用的方法
    /// </summary>
    /// <param name="ar"></param>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate void AsyncCallback(IAsyncResult ar);

}
