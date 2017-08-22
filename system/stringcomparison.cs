// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:  StringComparison
**
**
** Purpose: A mechanism to expose a simplified infrastructure for 
**          Comparing strings. This enum lets you choose of the custom 
**          implementations provided by the runtime for the user.
**
** 
===========================================================*/
namespace System{
    
    /// <summary>
    /// 指定 System.String.Compare(System.String,System.String) 和 System.String.Equals(System.Object)
    /// 方法的某些重载要使用的区域、大小写和排序规则。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum StringComparison {
        
        /// <summary>
        /// 使用区域敏感排序规则和当前区域比较字符串
        /// </summary>
        CurrentCulture = 0,
        /// <summary>
        /// 使用区域敏感排序规则、当前区域来比较字符串，同时忽略被比较字符串的大小写
        /// </summary>
        CurrentCultureIgnoreCase = 1,
        /// <summary>
        /// 使用区域敏感排序规则和固定区域比较字符串。
        /// </summary>
        InvariantCulture = 2,
        /// <summary>
        /// 使用区域敏感排序规则、固定区域来比较字符串，同时忽略被比较字符串的大小写。
        /// </summary>
        InvariantCultureIgnoreCase = 3,
        /// <summary>
        /// 使用序号排序规则比较字符串。
        /// </summary>
        Ordinal = 4,
        /// <summary>
        /// 使用序号排序规则并忽略被比较字符串的大小写，对字符串进行比较。
        /// </summary>
        OrdinalIgnoreCase = 5,
    }
}
