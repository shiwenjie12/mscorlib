// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System.Runtime.InteropServices {
    using System;
    /// <summary>
    /// 控制当导出到非托管代码时对象的布局。
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public enum LayoutKind
    {
        /// <summary>
        /// 对象的成员按照他们被导出到非托管内存时出现的顺序依次布局。这些成员根据System.Runtime.InteropServices.StructLayoutAttribute.Pack
        /// 中指定的封装进行布局，并且可以不连续的。
        /// </summary>
        Sequential      = 0, // 0x00000008,
        /// <summary>
        /// 对象的各个成员在非托管内存中的精确位置被显式控制。每个成员必须使用System.Runtime.InteropServices.FieldOffsetAttribute
        /// 指示该字段在类型中的位置
        /// </summary>
        Explicit        = 2, // 0x00000010,
        /// <summary>
        /// 运行时自动为非托管内存中的对象的成员选择适当的布局。使用此枚举成员定义的对象不能在托管代码的外部公开。尝试这样将引起异常    
        /// </summary>
        Auto            = 3, // 0x00000000,
    }
}
