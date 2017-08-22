// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Interface: _Exception
**
**
** Purpose: COM backwards compatibility with v1 Exception
**        object layout.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    
    /// <summary>
    /// 向非托管代码公开 System.Exception 类的公共成员。此API不兼容CLS。
    /// </summary>
    [GuidAttribute("b36b5c63-42ef-38bc-a07e-0b34c98f164a")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
    [CLSCompliant(false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface _Exception
    {
#if !FEATURE_CORECLR
        // This contains all of our V1 Exception class's members.

        // From Object
        /// <summary>
        /// 为 COM 对象提供对 Exception.ToString 方法的版本无关的访问
        /// </summary>
        /// <returns>字符串</returns>
        String ToString();
        /// <summary>
        /// 为 COM 对象提供对 Object.Equals 方法的版本无关的访问。
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        bool Equals (Object obj);
        /// <summary>
        /// 为 COM 对象提供对 Object.GetHashCode 方法的版本无关的访问。
        /// </summary>
        /// <returns></returns>
        int GetHashCode ();

        /// <summary>
        /// 为 COM 对象提供对 Exception.GetType 方法的版本无关的访问。
        /// </summary>
        /// <returns></returns>
        Type GetType ();

        /// <summary>
        /// 为 COM 对象提供对 Exception.Message 属性的版本无关的访问。
        /// </summary>
        String Message {
            get;
        }

        /// <summary>
        /// 为 COM 对象提供对 Exception.GetBaseException 方法的版本无关的访问。
        /// </summary>
        /// <returns></returns>
        Exception GetBaseException();

        /// <summary>
        /// 为 COM 对象提供对 Exception.StackTrace 属性的版本无关的访问。
        /// </summary>
        String StackTrace {
            get;
        }

        /// <summary>
        /// 为 COM 对象提供对 Exception.HelpLink 属性的版本无关的访问。
        /// </summary>
        String HelpLink {
            get;
            set;
        }

        /// <summary>
        /// 为 COM 对象提供对 Exception.Source 属性的版本无关的访问。
        /// </summary>
        String Source {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get;
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            set;
        }

        /// <summary>
        /// 为 COM 对象提供对 Exception.GetObjectData 方法的版本无关的访问
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流的上下文</param>
        [System.Security.SecurityCritical]  // auto-generated_required
        void GetObjectData(SerializationInfo info, StreamingContext context);
#endif

        // 这个方法是故意包含在CoreCLR例外。get_InnerException“newslot虚拟决赛”。MEF get_InnerException ComposablePartException取决于隐式接口实现的基类所提供的。
        // 只有例外。get_InnerException是虚拟的。

        /// <summary>
        /// 为 COM 对象提供对 Exception.InnerException 属性的版本无关的访问。一些手机应用程序包括MEF从桌面Silverlight。
        /// </summary>
        Exception InnerException {
            get;
        }

#if !FEATURE_CORECLR     
        /// <summary>
        /// 为 COM 对象提供对 Exception.TargetSite 属性的版本无关的访问。
        /// </summary>
        MethodBase TargetSite {
            get;
        }
#endif
   }

}
