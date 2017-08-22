// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
namespace System {
 
    using System;
    using System.Runtime.Serialization;
    /// <summary>
    /// 用作系统异常命名空间的基类。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class SystemException : Exception
    {
        /// <summary>
        /// 初始化 SystemException 类的新实例。
        /// </summary>
        public SystemException() 
            : base(Environment.GetResourceString("Arg_SystemException")) {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        /// <summary>
        /// 使用指定的错误消息初始化 SystemException 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        public SystemException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        /// <summary>
        /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 SystemException 类的新实例。
        /// </summary>
        /// <param name="message">解释异常原因的错误消息。</param>
        /// <param name="innerException">导致当前异常的异常。如果 innerException 参数不是空引用（在 Visual Basic 中为 Nothing），则在处理内部异常的 catch 块中引发当前异常。</param>
        public SystemException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        protected SystemException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
