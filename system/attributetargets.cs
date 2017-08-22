// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System {
    
    using System;
    
    // Enum used to indicate all the elements of the
    // VOS it is valid to attach this element to.
[Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum AttributeTargets
    {
        /// <summary>
        /// 程序集
        /// </summary>
        Assembly      = 0x0001,
        /// <summary>
        /// 模板
        /// </summary>
        Module        = 0x0002,
        /// <summary>
        /// 类
        /// </summary>
        Class         = 0x0004,
        /// <summary>
        /// 结构
        /// </summary>
        Struct        = 0x0008,
        /// <summary>
        /// 枚举
        /// </summary>
        Enum          = 0x0010,
        /// <summary>
        /// 构造器
        /// </summary>
        Constructor   = 0x0020,
        /// <summary>
        /// 方法
        /// </summary>
        Method        = 0x0040,
        /// <summary>
        /// 属性
        /// </summary>
        Property      = 0x0080,
        /// <summary>
        /// 字段
        /// </summary>
        Field         = 0x0100,
        /// <summary>
        /// 事件
        /// </summary>
        Event         = 0x0200,
        /// <summary>
        /// 接口
        /// </summary>
        Interface     = 0x0400,
        /// <summary>
        /// 参数
        /// </summary>
        Parameter     = 0x0800,
        /// <summary>
        /// 委托
        /// </summary>
        Delegate      = 0x1000,
        /// <summary>
        /// 返回值
        /// </summary>
        ReturnValue   = 0x2000,
        //@todo GENERICS: document GenericParameter
        /// <summary>
        /// 泛型参数
        /// </summary>
        GenericParameter = 0x4000,
        
        
        All           = Assembly | Module   | Class | Struct | Enum      | Constructor | 
                        Method   | Property | Field | Event  | Interface | Parameter   | 
                        Delegate | ReturnValue | GenericParameter,
    }
}
