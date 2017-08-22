// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:   FileOptions
** 
** <OWNER>gpaperin</OWNER>
**
**
** Purpose: Additional options to how to create a FileStream.
**    Exposes the more obscure CreateFile functionality.
**
**
===========================================================*/

using System;
using System.Runtime.InteropServices;

namespace System.IO {
    // Maps to FILE_FLAG_DELETE_ON_CLOSE and similar values from winbase.h.
    // We didn't expose(揭露) a number of these values because we didn't believe 
    // a number of them made sense(有意义的) in managed code, at least not yet.
    /// <summary>
    /// 表示用于创建 FileStream 对象的高级选项。
    /// </summary>
    [Serializable]
    [Flags]
    [ComVisible(true)]
    public enum FileOptions
    {
        // NOTE: any change to FileOptions enum needs to be 
        // matched in the FileStream ctor for error validation
        /// <summary>
        /// 指示在生成 FileStream 对象时，不应使用其他选项。
        /// </summary>
        None = 0,
        /// <summary>
        /// 指示系统应通过任何中间缓存、直接写入磁盘。
        /// </summary>
        WriteThrough = unchecked((int)0x80000000),
        /// <summary>
        /// 指示文件可用于异步读取和写入。
        /// </summary>
        Asynchronous = unchecked((int)0x40000000), // FILE_FLAG_OVERLAPPED
        // NoBuffering = 0x20000000,
        /// <summary>
        /// 指示随机访问文件。系统可将此选项用作优化文件缓存的提示。
        /// </summary>
        RandomAccess = 0x10000000,
        /// <summary>
        /// 指示当不再使用某个文件时，自动删除该文件。
        /// </summary>
        DeleteOnClose = 0x04000000,
        /// <summary>
        /// 指示按从头到尾的顺序访问文件。系统可将此选项用作优化文件缓存的提示。
        /// 如果应用程序移动用于随机访问的文件指针，可能不发生优化缓存，但仍然保证操作的正确性。
        /// </summary>
        SequentialScan = 0x08000000,
        // AllowPosix = 0x01000000,  // FILE_FLAG_POSIX_SEMANTICS
        // BackupOrRestore,
        // DisallowReparsePoint = 0x00200000, // FILE_FLAG_OPEN_REPARSE_POINT
        // NoRemoteRecall = 0x00100000, // FILE_FLAG_OPEN_NO_RECALL
        // FirstPipeInstance = 0x00080000, // FILE_FLAG_FIRST_PIPE_INSTANCE
        /// <summary>
        /// 指示文件是加密的，只能通过用于加密的同一用户帐户来解密。
        /// </summary>
        Encrypted = 0x00004000, // FILE_ATTRIBUTE_ENCRYPTED
    }
}

