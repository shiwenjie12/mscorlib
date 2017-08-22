// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:   FileShare
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Enum describing how to share files with other 
** processes - ie, whether two processes can simultaneously
** read from the same file.
**
**
===========================================================*/

using System;

namespace System.IO {
    // Contains constants for controlling file sharing options while
    // opening files.  You can specify what access other processes trying
    // to open the same file concurrently can have.
    //
    // Note these values currently match the values for FILE_SHARE_READ,
    // FILE_SHARE_WRITE, and FILE_SHARE_DELETE in winnt.h
    // 
    /// <summary>
    /// 包含用于控制其他 FileStream 对象对同一文件可以具有的访问类型的常数。
    /// </summary>
    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileShare
    {
        // No sharing. Any request to open the file (by this process or another
        // process) will fail until the file is closed.
        /// <summary>
        /// 谢绝共享当前文件。
        /// 文件关闭前，打开该文件的任何请求（由此进程或另一进程发出的请求）都将失败。
        /// </summary>
        None = 0,

        // Allows subsequent opening of the file for reading. If this flag is not
        // specified, any request to open the file for reading (by this process or
        // another process) will fail until the file is closed.
        /// <summary>
        /// 允许随后打开文件读取。
        /// 如果未指定此标志，则文件关闭前，任何打开该文件以进行读取的请求（由此进程或另一进程发出的请求）都将失败。
        /// 但是，即使指定了此标志，仍可能需要附加权限才能够访问该文件。
        /// </summary>
        Read = 1,

        // Allows subsequent opening of the file for writing. If this flag is not
        // specified, any request to open the file for writing (by this process or
        // another process) will fail until the file is closed.
        /// <summary>
        /// 允许随后打开文件写入。
        /// 如果未指定此标志，则文件关闭前，任何打开该文件以进行写入的请求（由此进程或另一进过程发出的请求）都将失败。
        /// 但是，即使指定了此标志，仍可能需要附加权限才能够访问该文件。
        /// </summary>
        Write = 2,

        // Allows subsequent opening of the file for writing or reading. If this flag
        // is not specified, any request to open the file for writing or reading (by
        // this process or another process) will fail until the file is closed.
        /// <summary>
        /// 允许随后打开文件读取或写入。
        /// 如果未指定此标志，则文件关闭前，任何打开该文件以进行读取或写入的请求（由此进程或另一进程发出）都将失败。
        /// 但是，即使指定了此标志，仍可能需要附加权限才能够访问该文件。
        /// </summary>
        ReadWrite = 3,

        // Open the file, but allow someone else to delete the file.
        /// <summary>
        /// 允许随后删除文件。
        /// </summary>
        Delete = 4,

        // Whether the file handle should be inheritable by child processes.
        // Note this is not directly supported like this by Win32.
        /// <summary>
        /// 使文件句柄可由子进程继承。Win32 不直接支持此功能。
        /// </summary>
        Inheritable = 0x10,
    }
}
