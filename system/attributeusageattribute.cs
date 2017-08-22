// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  AttributeUsageAttribute
**
**
** Purpose: The class denotes how to specify the usage of an attribute
**          
**
===========================================================*/
namespace System {

    using System.Reflection;
    /// <summary>
    /// 指定另一特性类的用法。 此类不能被继承。
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        /// <summary>
        /// 默认特性的使用范围为ALL
        /// </summary>
        internal AttributeTargets m_attributeTarget = AttributeTargets.All; // Defaults to all
        /// <summary>
        /// 能否为一个程序元素指定多个指示特性实例，默认为False
        /// </summary>
        internal bool m_allowMultiple = false; // Defaults to false
        /// <summary>
        /// 指示的特性能否由派生类和重写成员继承。
        /// </summary>
        internal bool m_inherited = true; // Defaults to true
    
        internal static AttributeUsageAttribute Default = new AttributeUsageAttribute(AttributeTargets.All);

       //Constructors 
        public AttributeUsageAttribute(AttributeTargets validOn) {
            m_attributeTarget = validOn;
        }
       internal AttributeUsageAttribute(AttributeTargets validOn, bool allowMultiple, bool inherited) {
           m_attributeTarget = validOn;
           m_allowMultiple = allowMultiple;
           m_inherited = inherited;
       }
    
       
        /// <summary>
        /// 获取一组值，这组值标识指示的特性可应用到的程序元素。
        /// </summary>
        public AttributeTargets ValidOn 
        {
           get{ return m_attributeTarget; }
        }
    
        /// <summary>
        /// 获取或设置一个布尔值，该值指示能否为一个程序元素指定多个指示特性实例。
        /// </summary>
        public bool AllowMultiple 
        {
            get { return m_allowMultiple; }
            set { m_allowMultiple = value; }
        }
    
        /// <summary>
        /// 获取或设置一个布尔值，该值指示指示的特性能否由派生类和重写成员继承。
        /// </summary>
        public bool Inherited 
        {
            get { return m_inherited; }
            set { m_inherited = value; }
        }
    }
}
