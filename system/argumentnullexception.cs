// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: ArgumentNullException
**
**
** Purpose: Exception class for null arguments to a method.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.Remoting;
    using System.Security.Permissions;
    
    // 当一个参数不应该为空时，抛出一个参数异常
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable] 
    public class ArgumentNullException : ArgumentException
    {
        // 创建一个新的ArgumentNullException的消息字符串设置为默认消息解释一个参数是null。
       public ArgumentNullException() 
            : base(Environment.GetResourceString("ArgumentNull_Generic")) {
                SetErrorCode(__HResults.E_POINTER);//使用E_POINTER - COM使用空指针。描述是“无效的指针”
        }

        public ArgumentNullException(String paramName) 
            : base(Environment.GetResourceString("ArgumentNull_Generic"), paramName) {
            SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.E_POINTER);
        }
            
        public ArgumentNullException(String paramName, String message) 
            : base(message, paramName) {
            SetErrorCode(__HResults.E_POINTER);   
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        protected ArgumentNullException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
