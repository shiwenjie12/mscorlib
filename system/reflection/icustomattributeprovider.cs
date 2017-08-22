// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// ICustomAttributeProvider is an interface that is implemented by reflection
// 
// <OWNER>WESU</OWNER>
//    objects which support custom attributes.
//
// <EMAIL>Author: darylo & Rajesh Chandrashekaran (rajeshc)</EMAIL>
// Date: July 99
//
namespace System.Reflection {
    
    using System;

    /// <summary>
    /// 为支持自定义属性的反映对象提供自定义属性。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICustomAttributeProvider
    {

        /// <summary>
        /// 返回此成员上定义的自定义属性的数组（由类型标识），如果该类型没有自定义属性，则返回空数组。
        /// </summary>
        /// <param name="attributeType">自定义属性的类型。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        /// <returns>表示自定义属性的对象的数组或空数组。</returns>
        /// <exception cref="System.TypeLoadException">无法加载自定义特性类型。</exception>
        /// <exception cref="System.ArgumentNullException">attributeType 为 null</exception>
        Object[] GetCustomAttributes(Type attributeType, bool inherit);


        /// <summary>
        /// 返回在此成员上定义的所有自定义属性（命名属性除外）的数组，或如果没有自定义属性，返回一个空数组。
        /// </summary>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        /// <returns>表示自定义属性的对象的数组或空数组。</returns>
        /// <exception cref="System.TypeLoadException">无法加载自定义特性类型。</exception>
        /// <exception cref="System.ArgumentNullException">attributeType 为 null</exception>
        Object[] GetCustomAttributes(bool inherit);

    
        /// <summary>
        /// 指示是否在此成员上定义一个或多个 attributeType 的实例。
        /// </summary>
        /// <param name="attributeType">自定义属性的类型。</param>
        /// <param name="inherit">当为 true 时，查找继承的自定义属性的层次结构链。</param>
        /// <returns>如果在此成员上定义 attributeType，则为 true；否则为 false。</returns>
        bool IsDefined (Type attributeType, bool inherit);
    
    }
}
