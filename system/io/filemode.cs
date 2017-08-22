// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:   FileMode
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Enum describing whether to create a new file or 
** open an existing one.
**
**
===========================================================*/
    
using System;

namespace System.IO {
    // Contains constants for specifying how the OS should open a file.
    // These will control whether you overwrite a file, open an existing
    // file, or some combination thereof.
    // 
    // To append to a file, use Append (which maps to OpenOrCreate then we seek
    // to the end of the file).  To truncate a file or create it if it doesn't 
    // exist, use Create.
    /// <summary>
    /// 指定操作系统打开文件的方式。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileMode
    {
        // Creates a new file. An exception is raised if the file already exists.
        /// <summary>
        /// 指定操作系统应创建新文件。
        /// 这需要 FileIOPermissionAccess.Write 权限。如果文件已存在，则将引发 IOException异常。
        /// </summary>
        CreateNew = 1,

        // Creates a new file. If the file already exists, it is overwritten.
        /// <summary>
        /// 指定操作系统应创建新文件。如果文件已存在，它将被覆盖。这需要 FileIOPermissionAccess.Write 权限。
        /// FileMode.Create 等效于这样的请求：如果文件不存在，则使用 CreateNew；
        /// 否则使用 Truncate。如果该文件已存在但为隐藏文件，则将引发 UnauthorizedAccessException异常。
        /// </summary>
        Create = 2,

        // Opens an existing file. An exception is raised if the file does not exist.
        /// <summary>
        /// 指定操作系统应打开现有文件。打开文件的能力取决于 FileAccess 枚举所指定的值。
        /// 如果文件不存在，引发一个 System.IO.FileNotFoundException 异常。
        /// </summary>
        Open = 3,

        // Opens the file if it exists. Otherwise, creates a new file.
        /// <summary>
        /// 指定操作系统应打开文件（如果文件存在）；否则，应创建新文件。
        /// 如果用 FileAccess.Read 打开文件，则需要 FileIOPermissionAccess.Read权限。
        /// 如果文件访问为 FileAccess.Write，则需要 FileIOPermissionAccess.Write权限。
        /// 如果用 FileAccess.ReadWrite 打开文件，则同时需要 FileIOPermissionAccess.Read 和 FileIOPermissionAccess.Write权限。
        /// </summary>
        OpenOrCreate = 4,

        // Opens an existing file. Once opened, the file is truncated so that its
        // size is zero bytes. The calling process must open the file with at least
        // WRITE access. An exception is raised if the file does not exist.
        /// <summary>
        /// 指定操作系统应打开现有文件。该文件被打开时，将被截断为零字节大小。
        /// 这需要 FileIOPermissionAccess.Write 权限。
        /// 尝试从使用 FileMode.Truncate 打开的文件中进行读取将导致 ArgumentException 异常。
        /// </summary>
        Truncate = 5,

        // Opens the file if it exists and seeks to the end.  Otherwise, 
        // creates a new file.
        /// <summary>
        /// 若存在文件，则打开该文件并查找到文件尾，或者创建一个新文件。
        /// 这需要 FileIOPermissionAccess.Append 权限。 FileMode.Append 只能与 FileAccess.Write 一起使用。
        /// 试图查找文件尾之前的位置时会引发 IOException 异常，并且任何试图读取的操作都会失败并引发 NotSupportedException 异常。
        /// </summary>
        Append = 6,
    }
}
