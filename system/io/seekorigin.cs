// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Enum:   SeekOrigin
** 
** <OWNER>kimhamil</OWNER>
**
**
** Purpose: Enum describing locations in a stream you could
** seek relative to.
**
**
===========================================================*/

using System;

namespace System.IO {
    // Provides seek reference points.  To seek to the end of a stream,
    // call stream.Seek(0, SeekOrigin.End).
    /// <summary>
    /// 指定在流的位置以查找使用。
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum SeekOrigin
    {
        // These constants match Win32's FILE_BEGIN, FILE_CURRENT, and FILE_END
        /// <summary>
        /// 指定流的开头。
        /// </summary>
        Begin = 0,
        /// <summary>
        /// 指定流内的当前位置。
        /// </summary>
        Current = 1,
        /// <summary>
        /// 指定流的结尾。
        /// </summary>
        End = 2,
    }
}
