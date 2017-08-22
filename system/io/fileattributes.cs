// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  FileAttributes
** 
** Purpose: File attribute flags corresponding to NT's flags.
**
** 
===========================================================*/
using System;

namespace System.IO {
    // File attributes for use with the FileEnumerator class.
    // These constants correspond to the constants in WinNT.h.
    // 
    /// <summary>
    /// 提供文件和目录的属性。
    /// </summary>
    [Serializable]
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileAttributes
    {
        // From WinNT.h (FILE_ATTRIBUTE_XXX)
        /// <summary>
        /// 此文件是只读的。
        /// </summary>
        ReadOnly = 0x1,
        /// <summary>
        /// 文件是隐藏的，因此没有包括在普通的目录列表中。
        /// </summary>
        Hidden = 0x2,
        /// <summary>
        /// 此文件是系统文件。即，该文件是操作系统的一部分或者由操作系统以独占方式使用。
        /// </summary>
        System = 0x4,
        /// <summary>
        /// 此文件是一个目录。
        /// </summary>
        Directory = 0x10,
        /// <summary>
        /// 该文件是备份或移除的候选文件。
        /// </summary>
        Archive = 0x20,
        /// <summary>
        /// 保留供将来使用。
        /// </summary>
        Device = 0x40,
        /// <summary>
        /// 该文件是没有特殊属性的标准文件。仅当其单独使用时，此特性才有效。
        /// </summary>
        Normal = 0x80,
        /// <summary>
        /// 文件是临时文件。临时文件包含当执行应用程序时需要的，但当应用程序完成后不需要的数据。
        /// 文件系统尝试将所有数据保存在内存中，而不是将数据刷新回大容量存储，以便可以快速访问。
        /// 当临时文件不再需要时，应用程序应立即删除它。
        /// </summary>
        Temporary = 0x100,
        /// <summary>
        /// 此文件是稀疏文件。稀疏文件一般是数据通常为零的大文件。
        /// </summary>
        SparseFile = 0x200,
        /// <summary>
        /// 文件包含一个重新分析点，它是一个与文件或目录关联的用户定义的数据块。
        /// </summary>
        ReparsePoint = 0x400,
        /// <summary>
        /// 此文件是压缩文件。
        /// </summary>
        Compressed = 0x800,
        /// <summary>
        /// 此文件处于脱机状态，文件数据不能立即供使用。
        /// </summary>
        Offline = 0x1000,
        /// <summary>
        /// 将不会通过操作系统的内容索引服务来索引此文件。
        /// </summary>
        NotContentIndexed = 0x2000,
        /// <summary>
        /// 此文件或目录已加密。对于文件来说，表示文件中的所有数据都是加密的。
        /// 对于目录来说，表示新创建的文件和目录在默认情况下是加密的。
        /// </summary>
        Encrypted = 0x4000,

#if !FEATURE_CORECLR
#if FEATURE_COMINTEROP
        /// <summary>
        /// 文件或目录包括完整性支持数据。在此值适用于文件时，文件中的所有数据流具有完整性支持。
        /// 此值将应用于一个目录时，所有新文件和子目录在该目录中和默认情况下应包括完整性支持。
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
        IntegrityStream = 0x8000,

#if FEATURE_COMINTEROP
        /// <summary>
        /// 文件或目录从完整性扫描数据中排除。
        /// 此值将应用于一个目录时，所有新文件和子目录在该目录中和默认情况下应不包括数据完整性。
        /// </summary>
        [System.Runtime.InteropServices.ComVisible(false)]        
#endif // FEATURE_COMINTEROP
        NoScrubData = 0x20000,
#endif
    }
}
