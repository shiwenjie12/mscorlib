// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:   FileAccess
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Enum describing whether you want read and/or write
** permission to a file.
**
**
===========================================================*/

using System;

namespace System.IO {
    // Contains constants for specifying the access you want for a file.
    // You can have Read, Write or ReadWrite access. 

    /// <summary>
    /// 定义用于文件读取、写入或读取/写入访问权限的常数。
    /// </summary>
    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileAccess
    {
        // Specifies read access to the file. Data can be read from the file and
        // the file pointer can be moved. Combine with WRITE for read-write access.
        /// <summary>
        /// 对文件的读访问。可从文件中读取数据。与 Write 组合以进行读写访问。
        /// </summary>
        Read = 1,

        // Specifies write access to the file. Data can be written to the file and
        // the file pointer can be moved. Combine with READ for read-write access.
        /// <summary>
        /// 文件的写访问。可将数据写入文件。同 Read 组合即构成读/写访问权。
        /// </summary>
        Write = 2,

        // Specifies read and write access to the file. Data can be written to the
        // file and the file pointer can be moved. Data can also be read from the 
        // file.
        /// <summary>
        /// 对文件的读访问和写访问。可从文件读取数据和将数据写入文件。
        /// </summary>
        ReadWrite = 3,
    }
}
