// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: CLSCompliantAttribute
**
**
** Purpose: Container for assemblies.
**
**
=============================================================================*/

namespace System {

    /// <summary>
    /// 指示程序元素是否符合公共语言规范 (CLS)。 此类不能被继承。
    /// </summary>
    [Serializable]
    [AttributeUsage (AttributeTargets.All, Inherited=true, AllowMultiple=false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CLSCompliantAttribute : Attribute 
    {
        /// <summary>
        /// 是否符合公共语言规范
        /// </summary>
        private bool m_compliant;

        /// <summary>
        /// 用布尔值初始化 System.CLSCompliantAttribute 类的实例，该值指示所指示的程序元素是否符合 CLS。
        /// </summary>
        /// <param name="isCompliant">如果程序元素符合 CLS，则为 true；否则为 false。</param>
        public CLSCompliantAttribute (bool isCompliant)
        {
            m_compliant = isCompliant;
        }

        /// <summary>
        /// 获取指示所指示的程序元素是否符合 CLS 的布尔值。
        /// </summary>
        public bool IsCompliant 
        {
            get 
            {
                return m_compliant;
            }
        }
    }
}
