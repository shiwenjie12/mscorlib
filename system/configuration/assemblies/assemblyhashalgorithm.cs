// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** File:    AssemblyHashAlgorithm
**
**
** Purpose: 
**
**
===========================================================*/
using System.Runtime.InteropServices;

namespace System.Configuration.Assemblies {
    
    using System;

    /// <summary>
    /// 指定用于哈希文件和用于生成强名称的所有哈希算法。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum AssemblyHashAlgorithm
    {
        /// <summary>
        /// 一个掩码，它指示无哈希算法。如果为多模块程序集指定 None，则公共语言运行时默认采用 SHA1 算法，因为多模块程序集需要生成哈希。
        /// </summary>
        None = 0,
        /// <summary>
        /// 检索 MD5 消息摘要算法。MD5 是 Rivest 在 1991 年开发的。它与 MD4 基本相同，只是增加了安全性。
        /// 它虽然比 MD4 稍慢一些，但更安全。该算法包括四个不同的步骤，其设计与 MD4 的略有不同。消息摘要的大小以及填充要求保持不变。
        /// </summary>
        MD5 = 0x8003,
        /// <summary>
        /// 用于检索“安全哈希算法”修订版的掩码，该修订版更正了 SHA 中的一个未发布的错误。
        /// </summary>
        SHA1 = 0x8004,
        /// <summary>
        /// 用于检索“安全哈希算法”的版本的掩码，其哈希值大小为 256 位。
        /// </summary>
        [ComVisible(false)]
        SHA256 = 0x800c,
        /// <summary>
        /// 用于检索“安全哈希算法”的版本的掩码，其哈希值大小为 384 位。
        /// </summary>
        [ComVisible(false)]
        SHA384 = 0x800d,
        /// <summary>
        /// 用于检索“安全哈希算法”的版本的掩码，其哈希值大小为 512 位。
        /// </summary>
        [ComVisible(false)]
        SHA512 = 0x800e,
    }
}
