// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  File
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: A collection of methods for manipulating Files.
**
**        April 09,2000 (some design refactorization)
**
===========================================================*/

using System;
using System.Security.Permissions;
using PermissionSet = System.Security.PermissionSet;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Runtime.InteropServices;
using System.Security;
#if FEATURE_MACL
using System.Security.AccessControl;
#endif
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
    
namespace System.IO {
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    /// <summary>
    /// 提供用于创建、复制、删除、移动和打开单一文件的静态方法，并协助创建 FileStream 对象。
    /// </summary>
    [ComVisible(true)]
    public static class File
    {
        private const int GetFileExInfoStandard = 0;

        /// <summary>
        /// 打开现有 UTF-8 编码文本文件以进行读取。
        /// </summary>
        /// <param name="path">要打开以进行读取的文件。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static StreamReader OpenText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            return new StreamReader(path);
        }

        /// <summary>
        /// 在指定路径中创建或覆盖文件。
        /// </summary>
        /// <param name="path">要创建的文件的路径及名称。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static StreamWriter CreateText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            return new StreamWriter(path,false);
        }

        /// <summary>
        /// 创建 StreamWriter，将 utf-8 编码文本追加到现有文件，或一个新的文件，如果指定的文件不存在。
        /// </summary>
        /// <param name="path">要追加到文件的路径。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static StreamWriter AppendText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            return new StreamWriter(path,true);
        }


        // Copies an existing file to a new file. An exception is raised if the
        // destination file already exists. Use the 
        // Copy(String, String, boolean) method to allow 
        // overwriting an existing file.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName and Create
        // and Write permissions to destFileName.
        // 
        /// <summary>
        /// 将现有文件复制到新文件。不允许覆盖同名的文件。
        /// </summary>
        /// <param name="sourceFileName">要复制的文件。</param>
        /// <param name="destFileName">目标文件的名称。它不能是一个目录或现有文件。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Copy(String sourceFileName, String destFileName) {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, false, true);
        }

        // Copies an existing file to a new file. If overwrite is 
        // false, then an IOException is thrown if the destination file 
        // already exists.  If overwrite is true, the file is 
        // overwritten.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName 
        // and Write permissions to destFileName.
        // 
        /// <summary>
        /// 将现有文件复制到新文件。允许覆盖同名的文件。
        /// </summary>
        /// <param name="sourceFileName">要复制的文件。</param>
        /// <param name="destFileName">目标文件的名称。不能是目录。</param>
        /// <param name="overwrite">如果可以覆盖目标文件，则为 true；否则为 false。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Copy(String sourceFileName, String destFileName, bool overwrite) {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, overwrite, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        /// <param name="overwrite"></param>
        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void UnsafeCopy(String sourceFileName, String destFileName, bool overwrite) {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            InternalCopy(sourceFileName, destFileName, overwrite, false);
        }

        /// <summary>
        /// 内部文件复制
        /// </summary>
        /// <param name="sourceFileName">源文件</param>
        /// <param name="destFileName">目标文件</param>
        /// <param name="overwrite">是否支持重新</param>
        /// <param name="checkHost"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static String InternalCopy(String sourceFileName, String destFileName, bool overwrite, bool checkHost) {
            Contract.Requires(sourceFileName != null);
            Contract.Requires(destFileName != null);
            Contract.Requires(sourceFileName.Length > 0);
            Contract.Requires(destFileName.Length > 0);

            String fullSourceFileName = Path.GetFullPathInternal(sourceFileName);
            String fullDestFileName = Path.GetFullPathInternal(destFileName);
            
#if FEATURE_CORECLR
            if (checkHost) {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, sourceFileName, fullSourceFileName);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
                sourceState.EnsureState();
                destState.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullSourceFileName, false, false);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, fullDestFileName, false, false);
#endif

            bool r = Win32Native.CopyFile(fullSourceFileName, fullDestFileName, !overwrite);
            if (!r)
            {
                // Save Win32 error because subsequent checks will overwrite this HRESULT.
                int errorCode = Marshal.GetLastWin32Error();
                String fileName = destFileName;

                if (errorCode != Win32Native.ERROR_FILE_EXISTS)
                {
                    // For a number of error codes (sharing violation, path 
                    // not found, etc) we don't know if the problem was with
                    // the source or dest file.  Try reading the source file.
                    using (SafeFileHandle handle = Win32Native.UnsafeCreateFile(fullSourceFileName, FileStream.GENERIC_READ, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid)
                            fileName = sourceFileName;
                    }

                    if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                    {
                        if (Directory.InternalExists(fullDestFileName))
                            throw new IOException(Environment.GetResourceString("Arg_FileIsDirectory_Name", destFileName), Win32Native.ERROR_ACCESS_DENIED, fullDestFileName);
                    }
                }

                __Error.WinIOError(errorCode, fileName);
            }

            return fullDestFileName;
        }


        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite accessand cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        //
        // Your application must have Create, Read, and Write permissions to
        // the file.
        // 
        /// <summary>
        /// 在指定路径中创建或覆盖文件。
        /// </summary>
        /// <param name="path">要创建的文件的路径及名称。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Create(String path) {
            return Create(path, FileStream.DefaultBufferSize);
        }

        // Creates a file in a particular path.  If the file exists, it is replaced.
        // The file is opened with ReadWrite access and cannot be opened by another 
        // application until it has been closed.  An IOException is thrown if the 
        // directory specified doesn't exist.
        //
        // Your application must have Create, Read, and Write permissions to
        // the file.
        // 
        /// <summary>
        /// 创建或覆盖指定的文件。
        /// </summary>
        /// <param name="path">文件的名称。</param>
        /// <param name="bufferSize">用于读取和写入到文件的已放入缓冲区的字节数。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Create(String path, int bufferSize) {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        /// <summary>
        /// 创建或覆盖指定的文件，指定缓冲区大小和一个描述如何创建或覆盖该文件的 FileOptions 值。
        /// </summary>
        /// <param name="path">文件的名称。</param>
        /// <param name="bufferSize">用于读取和写入到文件的已放入缓冲区的字节数。</param>
        /// <param name="options">FileOptions 值之一，它描述如何创建或覆盖该文件。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Create(String path, int bufferSize, FileOptions options) {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite,
                                  FileShare.None, bufferSize, options);
        }

#if FEATURE_MACL
        /// <summary>
        /// 创建或覆盖具有指定的缓冲区大小、文件选项和文件安全性的指定文件。
        /// </summary>
        /// <param name="path">文件的名称。</param>
        /// <param name="bufferSize">用于读取和写入到文件的已放入缓冲区的字节数。</param>
        /// <param name="options">FileOptions 值之一，它描述如何创建或覆盖该文件。</param>
        /// <param name="fileSecurity">FileSecurity 值之一，它确定文件的访问控制和审核安全性。</param>
        /// <returns></returns>
        public static FileStream Create(String path, int bufferSize, FileOptions options, FileSecurity fileSecurity) {
            return new FileStream(path, FileMode.Create, FileSystemRights.Read | FileSystemRights.Write,
                                  FileShare.None, bufferSize, options, fileSecurity);
        }
#endif

        // Deletes a file. The file specified by the designated path is deleted.
        // If the file does not exist, Delete succeeds without throwing
        // an exception.
        // 
        // On NT, Delete will fail for a file that is open for normal I/O
        // or a file that is memory mapped.  
        // 
        // Your application must have Delete permission to the target file.
        // 
        /// <summary>
        /// 删除指定的文件。
        /// </summary>
        /// <param name="path">要删除的文件的名称。该指令不支持通配符。</param>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Delete(String path) {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

#if FEATURE_LEGACYNETCF
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if(callingAssembly != null && !callingAssembly.IsProfileAssembly) {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Arg_MethodAccessException_WithCaller"),
                        caller,
                        callee));
                }
            }
#endif // FEATURE_LEGACYNETCF
            
            InternalDelete(path, true);
        }

        [System.Security.SecurityCritical] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void UnsafeDelete(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

            InternalDelete(path, false);
        }

        [System.Security.SecurityCritical] 
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void InternalDelete(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);

#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, path, fullPath);
                state.EnsureState();
            }
#else
            // For security check, path should be resolved to an absolute path.
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, fullPath, false, false);

#endif
            bool r = Win32Native.DeleteFile(fullPath);
            if (!r) {
                int hr = Marshal.GetLastWin32Error();
                if (hr==Win32Native.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    __Error.WinIOError(hr, fullPath);
            }
        }

        /// <summary>
        /// 对由当前帐户使用的加密方法加密文件进行解密。
        /// </summary>
        /// <param name="path">描述要解密的文件的路径。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Decrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

#if FEATURE_PAL
            throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT"));
#else

            String fullPath = Path.GetFullPathInternal(path);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, fullPath, false, false);

            bool r = Win32Native.DecryptFile(fullPath, 0);
            if (!r) {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_ACCESS_DENIED) {
                    // Check to see if the file system is not NTFS.  If so,
                    // throw a different exception.
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(fullPath));
                    if (!String.Equals("NTFS", di.DriveFormat))
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                }
                __Error.WinIOError(errorCode, fullPath);
            }
#endif
        }

        /// <summary>
        /// 加密的文件，以便仅用于加密文件的帐户可以对其进行解密。
        /// </summary>
        /// <param name="path">描述要加密的文件的路径。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Encrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();

#if FEATURE_PAL
            throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_RequiresNT"));
#else

            String fullPath = Path.GetFullPathInternal(path);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, fullPath, false, false);

            bool r = Win32Native.EncryptFile(fullPath);
            if (!r) {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_ACCESS_DENIED) {
                    // Check to see if the file system is not NTFS.  If so,
                    // throw a different exception.
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(fullPath));
                    if (!String.Equals("NTFS", di.DriveFormat))
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                }
                __Error.WinIOError(errorCode, fullPath);
            }
#endif
        }

        // Tests if a file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  Note that if path describes a directory,
        // Exists will return true.
        //
        // Your application must have Read permission for the target directory.
        /// <summary>
        /// 确定指定的文件是否存在。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static bool Exists(String path)
        {
#if FEATURE_LEGACYNETCF
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if(callingAssembly != null && !callingAssembly.IsProfileAssembly) {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Arg_MethodAccessException_WithCaller"),
                        caller,
                        callee));
                }
            }
#endif // FEATURE_LEGACYNETCF
            return InternalExistsHelper(path, true);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static bool UnsafeExists(String path)
        {
            return InternalExistsHelper(path, false);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static bool InternalExistsHelper(String path, bool checkHost) 
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;

                path = Path.GetFullPathInternal(path);
                // After normalizing, check whether path ends in directory separator.
                // Otherwise, FillAttributeInfo removes it and we may return a false positive.
                // GetFullPathInternal should never return null
                Contract.Assert(path != null, "File.Exists: GetFullPathInternal returned null");
                if (path.Length > 0 && Path.IsDirectorySeparator(path[path.Length - 1]))
                {
                    return false;
                }

#if FEATURE_CORECLR
                if (checkHost)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, path);
                    state.EnsureState();
                }
#else
                FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, path, false, false);
#endif

                return InternalExists(path);
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { } // Security can throw this on ":"
            catch (SecurityException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static bool InternalExists(String path) {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(path, ref data, false, true);

            return (dataInitialised == 0) && (data.fileAttributes != -1) 
                    && ((data.fileAttributes  & Win32Native.FILE_ATTRIBUTE_DIRECTORY) == 0);
        }

        /// <summary>
        /// 以读/写访问权限打开指定路径上的 FileStream。
        /// </summary>
        /// <param name="path">要打开的文件。</param>
        /// <param name="mode">FileMode 值，用于指定在文件不存在时是否创建该文件，并确定是保留还是覆盖现有文件的内容。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Open(String path, FileMode mode) {
            return Open(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        /// <summary>
        /// 与指定的模式和访问权限打开文件流，为指定路径上。
        /// </summary>
        /// <param name="path">要打开的文件</param>
        /// <param name="mode">FileMode 值，用于指定在文件不存在时是否创建该文件，并确定是保留还是覆盖现有文件的内容。</param>
        /// <param name="access">指定可以对文件执行的操作的文件访问值。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Open(String path, FileMode mode, FileAccess access) {
            return Open(path,mode, access, FileShare.None);
        }

        /// <summary>
        /// 打开指定路径上的 FileStream，具有带读、写或读/写访问的指定模式和指定的共享选项。
        /// </summary>
        /// <param name="path">要打开的文件。</param>
        /// <param name="mode">FileMode 值，用于指定在文件不存在时是否创建该文件，并确定是保留还是覆盖现有文件的内容。</param>
        /// <param name="access">一个 FileAccess 值，它指定可以对文件执行的操作。</param>
        /// <param name="share">一个 FileShare 值，它指定其他线程所具有的对该文件的访问类型。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream Open(String path, FileMode mode, FileAccess access, FileShare share) {
            return new FileStream(path, mode, access, share);
        }

        /// <summary>
        /// 设置日期和时间创建该文件。
        /// </summary>
        /// <param name="path">要为其设置其创建日期和时间信息的文件。</param>
        /// <param name="creationTime">包含要设置为创建日期和时间的路径的值的日期时间。此值表示本地时间。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void SetCreationTime(String path, DateTime creationTime)
        {
            SetCreationTimeUtc(path, creationTime.ToUniversalTime());
        }

        /// <summary>
        /// 设置文件创建的日期和时间，其格式为协调通用时 (UTC)。
        /// </summary>
        /// <param name="path">要设置其创建日期和时间信息的文件。</param>
        /// <param name="creationTime">一个 DateTime，它包含要为 path 的创建日期和时间设置的值。该值用 UTC 时间表示。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe static void SetCreationTimeUtc(String path, DateTime creationTimeUtc)
        {
            SafeFileHandle handle;
            using(OpenFile(path, FileAccess.Write, out handle)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(creationTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, &fileTime, null, null);
                if (!r)
                {
                     int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        /// <summary>
        /// 返回创建日期和时间的指定的文件或目录。
        /// </summary>
        /// <param name="path">文件或目录以获取其创建日期和时间信息</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetCreationTime(String path)
        {
            return InternalGetCreationTimeUtc(path, true).ToLocalTime();
        }

        /// <summary>
        /// 返回指定的文件或目录的创建日期和时间，在协调通用时间 (UTC)。
        /// </summary>
        /// <param name="path">文件或目录要获取其创建日期和时间信息。</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetCreationTimeUtc(String path)
        {
            return InternalGetCreationTimeUtc(path, false); // this API isn't exposed in Silverlight
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static DateTime InternalGetCreationTimeUtc(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
#if FEATURE_CORECLR
            if (checkHost) 
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullPath, false, false);
#endif

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)(data.ftCreationTimeHigh) << 32) | ((long)data.ftCreationTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// 设置上次访问指定文件的日期和时间。
        /// </summary>
        /// <param name="path">要设置其访问日期和时间信息的文件。</param>
        /// <param name="lastAccessTime">一个 DateTime，它包含要为 path 的上次访问日期和时间设置的值。该值用本地时间表示。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]      
        public static void SetLastAccessTime(String path, DateTime lastAccessTime)
        {
            SetLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());
        }

        /// <summary>
        /// 设置上次访问指定的文件的日期和时间，其格式为协调通用时 (UTC)。
        /// </summary>
        /// <param name="path">要设置其访问日期和时间信息的文件。</param>
        /// <param name="lastAccessTimeUtc">一个 DateTime，它包含要为 path 的上次访问日期和时间设置的值。该值用 UTC 时间表示。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe static void SetLastAccessTimeUtc(String path, DateTime lastAccessTimeUtc)
        {
            SafeFileHandle handle;
            using(OpenFile(path, FileAccess.Write, out handle)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastAccessTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, null, &fileTime,  null);
                if (!r)
                {
                     int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        /// <summary>
        /// 返回上次访问指定文件或目录的日期和时间。
        /// </summary>
        /// <param name="path">要获取其访问日期和时间信息的文件或目录。</param>
        /// <param name="lastAccessTime">一个 DateTime 结构，它被设置为上次访问指定文件或目录的日期和时间。该值用本地时间表示。</param>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetLastAccessTime(String path)
        {
            return InternalGetLastAccessTimeUtc(path, true).ToLocalTime();
        }

        /// <summary>
        /// 返回的日期和时间，以协调通用时间 (UTC)，上次访问指定的文件或目录。
        /// </summary>
        /// <param name="path">文件或目录要获取其访问日期和时间信息。</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return InternalGetLastAccessTimeUtc(path, false); // this API isn't exposed in Silverlight
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static DateTime InternalGetLastAccessTimeUtc(String path, bool checkHost)
        {       
            String fullPath = Path.GetFullPathInternal(path);
#if FEATURE_CORECLR
            if (checkHost) 
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullPath, false, false);
#endif

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)(data.ftLastAccessTimeHigh) << 32) | ((long)data.ftLastAccessTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// 设置上次写入指定文件的日期和时间。
        /// </summary>
        /// <param name="path">要设置其日期和时间信息的文件。</param>
        /// <param name="lastWriteTime">一个 DateTime，它包含要为 path 的上次写入日期和时间设置的值。该值用本地时间表示。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void SetLastWriteTime(String path, DateTime lastWriteTime)
        {
            SetLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());
        }

        /// <summary>
        /// 设置日期和时间，以协调通用时间 (UTC)，上次写入指定的文件。
        /// </summary>
        /// <param name="path">要为其设置的日期和时间信息的文件</param>
        /// <param name="lastWriteTimeUtc">包含要设置为上次写入日期和时间的路径的值的日期时间。此值表示在 UTC 时间</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public unsafe static void SetLastWriteTimeUtc(String path, DateTime lastWriteTimeUtc)
        {
            SafeFileHandle handle;
            using(OpenFile(path, FileAccess.Write, out handle)) {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, null, null, &fileTime);
                if (!r)
                {
                     int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetLastWriteTime(String path)
        {
            return InternalGetLastWriteTimeUtc(path, true).ToLocalTime();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return InternalGetLastWriteTimeUtc(path, false); // this API isn't exposed in Silverlight
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static DateTime InternalGetLastWriteTimeUtc(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullPath, false, false);
#endif

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            long dt = ((long)data.ftLastWriteTimeHigh << 32) | ((long)data.ftLastWriteTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        /// <summary>
        /// 获取在此路径上的文件的 FileAttributes。
        /// </summary>
        /// <param name="path">文件的路径。</param>
        /// <returns>路径上文件的 FileAttributes。</returns>
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileAttributes GetAttributes(String path) 
        {
            String fullPath = Path.GetFullPathInternal(path);
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
            state.EnsureState();
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullPath, false, false);
#endif

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, true);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);

            return (FileAttributes) data.fileAttributes;
        }

        /// <summary>
        /// 获取指定路径上的文件的指定 FileAttributes。
        /// </summary>
        /// <param name="path">文件的路径。</param>
        /// <param name="fileAttributes">枚举值的按位组合。</param>
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] 
#else
        [System.Security.SecuritySafeCritical]
        #endif
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void SetAttributes(String path, FileAttributes fileAttributes) 
        {
            String fullPath = Path.GetFullPathInternal(path);
#if !FEATURE_CORECLR
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, fullPath, false, false);
#endif
            bool r = Win32Native.SetFileAttributes(fullPath, (int) fileAttributes);
            if (!r) {
                int hr = Marshal.GetLastWin32Error();
                if (hr==ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
                 __Error.WinIOError(hr, fullPath);
            }
        }
#if FEATURE_MACL
        /// <summary>
        /// 获取一个 FileSecurity 对象，它封装为一个指定的文件的访问控制列表 (ACL) 条目。
        /// </summary>
        /// <param name="path">指向一个包含一个 FileSecurity 对象，描述该文件的访问控制列表 (ACL) 信息的文件的路径。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileSecurity GetAccessControl(String path)
        {
            return GetAccessControl(path, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        /// <summary>
        /// 将 FileSecurity 对象描述的访问控制列表 (ACL) 项应用于指定的文件。
        /// </summary>
        /// <param name="path">从其中添加或移除访问控制列表 (ACL) 项的文件。</param>
        /// <param name="includeSections">一个 FileSecurity 对象，描述要应用于 path 参数所描述的文件的 ACL 项。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileSecurity GetAccessControl(String path, AccessControlSections includeSections)
        {
            // Appropriate security check should be done for us by FileSecurity.
            return new FileSecurity(path, includeSections);
        }

        /// <summary>
        /// 将 FileSecurity 对象描述的访问控制列表 (ACL) 项应用于指定的文件。
        /// </summary>
        /// <param name="path">从其中添加或移除访问控制列表 (ACL) 项的文件。</param>
        /// <param name="fileSecurity">一个 FileSecurity 对象，描述要应用于 path 参数所描述的文件的 ACL 项。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void SetAccessControl(String path, FileSecurity fileSecurity)
        {
            if (fileSecurity == null)
                throw new ArgumentNullException("fileSecurity");
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPathInternal(path);
            // Appropriate security check should be done for us by FileSecurity.
            fileSecurity.Persist(fullPath);
        }
#endif
        /// <summary>
        /// 打开现有文件以进行读取。
        /// </summary>
        /// <param name="path">要打开以进行读取的文件。</param>
        /// <returns>指定路径上的只读 FileStream。</returns>
#if FEATURE_LEGACYNETCF
        [System.Security.SecuritySafeCritical]
#endif // FEATURE_LEGACYNETCF
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream OpenRead(String path) {
#if FEATURE_LEGACYNETCF
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if(callingAssembly != null && !callingAssembly.IsProfileAssembly) {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Arg_MethodAccessException_WithCaller"),
                        caller,
                        callee));
                }
            }
#endif // FEATURE_LEGACYNETCF
                                                           return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        /// 打开一个现有文件或创建一个新文件以进行写入。
        /// </summary>
        /// <param name="path">要打开以进行写入的文件。</param>
        /// <returns>指定路径上具有 FileStream 访问权限的非共享的 Write 对象。</returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static FileStream OpenWrite(String path) {
            return new FileStream(path, FileMode.OpenOrCreate, 
                                  FileAccess.Write, FileShare.None);
        }

        /// <summary>
        /// 打开一个文本文件，读取该文件的所有行，然后关闭该文件。
        /// </summary>
        /// <param name="path">要打开以进行读取的文件。</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static String ReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllText(path, Encoding.UTF8, true);
        }

        /// <summary>
        /// 打开一个文件，使用指定的编码读取文件的所有行，然后关闭该文件。
        /// </summary>
        /// <param name="path">要打开以进行读取的文件。</param>
        /// <param name="encoding">应用到文件内容的编码。</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static String ReadAllText(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllText(path, encoding, true);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static String UnsafeReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllText(path, Encoding.UTF8, false);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static String InternalReadAllText(String path, Encoding encoding, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamReader sr = new StreamReader(path, encoding, true, StreamReader.DefaultBufferSize, checkHost))
                return sr.ReadToEnd();
        }

        /// <summary>
        /// 创建一个新文件，向其中写入指定的字符串，然后关闭文件。如果目标文件已存在，则覆盖该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入文件的字符串。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllText(path, contents, StreamWriter.UTF8NoBOM, true);
        }

        /// <summary>
        /// 创建一个新文件，使用指定编码向其中写入指定的字符串，然后关闭文件。如果目标文件已存在，则覆盖该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入文件的字符串。</param>
        /// <param name="encoding">应用于字符串的编码。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllText(path, contents, encoding, true);
        }
        
        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void UnsafeWriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllText(path, contents, StreamWriter.UTF8NoBOM, false);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalWriteAllText(String path, String contents, Encoding encoding, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamWriter sw = new StreamWriter(path, false, encoding, StreamWriter.DefaultBufferSize, checkHost))
                sw.Write(contents);
        }

        /// <summary>
        /// 打开一个二进制文件，将文件的内容读入一个字节数组，然后关闭该文件。 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static byte[] ReadAllBytes(String path)
        {
            return InternalReadAllBytes(path, true);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static byte[] UnsafeReadAllBytes(String path)
        {
            return InternalReadAllBytes(path, false);
        }

        
        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static byte[] InternalReadAllBytes(String path, bool checkHost)
        {
            byte[] bytes;
            using(FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 
                FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, checkHost)) {
                // Do a blocking read
                int index = 0;
                long fileLength = fs.Length;
                if (fileLength > Int32.MaxValue)
                    throw new IOException(Environment.GetResourceString("IO.IO_FileTooLong2GB"));
                int count = (int) fileLength;
                bytes = new byte[count];
                while(count > 0) {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        __Error.EndOfFile();
                    index += n;
                    count -= n;
                }
            }
            return bytes;
        }

        /// <summary>
        /// 创建一个新文件，在其中写入指定的字节数组，然后关闭该文件。如果目标文件已存在，则覆盖该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="bytes">要写入文件的字节。</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            Contract.EndContractBlock();

            InternalWriteAllBytes(path, bytes, true);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void UnsafeWriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            Contract.EndContractBlock();

            InternalWriteAllBytes(path, bytes, false);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalWriteAllBytes(String path, byte[] bytes, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(path.Length != 0);
            Contract.Requires(bytes != null);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read,
                    FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, checkHost))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// 读取文件的行。
        /// </summary>
        /// <param name="path">要读取的文件。</param>
        /// <returns>该文件的所有行或查询结果所示的行。</returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static String[] ReadAllLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllLines(path, Encoding.UTF8);
        }

        /// <summary>
        /// 读取具有指定编码的文件的行。
        /// </summary>
        /// <param name="path">要读取的文件。</param>
        /// <param name="encoding">应用到文件内容的编码。</param>
        /// <returns>该文件的所有行或查询结果所示的行。</returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static String[] ReadAllLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            return InternalReadAllLines(path, encoding);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static String[] InternalReadAllLines(String path, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length != 0);

            String line;
            List<String> lines = new List<String>();

            using (StreamReader sr = new StreamReader(path, encoding))
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);

            return lines.ToArray();
        }

        /// <summary>
        /// 读取文件的行。
        /// </summary>
        /// <param name="path">要读取的文件。</param>
        /// <returns>该文件的所有行或查询结果所示的行。</returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static IEnumerable<String> ReadLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            Contract.EndContractBlock();

            return ReadLinesIterator.CreateIterator(path, Encoding.UTF8);
        }

        /// <summary>
        /// 读取具有指定编码的文件的行。
        /// </summary>
        /// <param name="path">要读取的文件。</param>
        /// <param name="encoding">应用到文件内容的编码。</param>
        /// <returns></returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static IEnumerable<String> ReadLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            Contract.EndContractBlock();

            return ReadLinesIterator.CreateIterator(path, encoding);
        }

        /// <summary>
        /// 创建一个新文件，在其中写入指定的字节数组，然后关闭该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入文件的字符串数组。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllLines(String path, String[] contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
        }

        /// <summary>
        /// 创建一个新文件，使用指定编码在其中写入指定的字符串数组，然后关闭该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入文件的字符串数组。</param>
        /// <param name="encoding">一个 Encoding 对象，它表示应用于字符串数组的字符编码。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllLines(String path, String[] contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        /// <summary>
        /// 创建一个新文件，向其中写入一个字符串集合，然后关闭该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入到文件中的行。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
        }

        /// <summary>
        /// 使用指定的编码创建一个新文件，向其中写入一个字符串集合，然后关闭该文件。
        /// </summary>
        /// <param name="path">要写入的文件。</param>
        /// <param name="contents">要写入到文件中的行。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void WriteAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalWriteAllLines(TextWriter writer, IEnumerable<String> contents)
        {
            Contract.Requires(writer != null);
            Contract.Requires(contents != null);

            using (writer)
            {
                foreach (String line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// 打开一个文件，向其中追加指定的字符串，然后关闭该文件。如果文件不存在，此方法将创建一个文件，将指定的字符串写入文件，然后关闭该文件。
        /// </summary>
        /// <param name="path">要将指定的字符串追加到的文件。</param>
        /// <param name="contents">要追加到文件中的字符串。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void AppendAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalAppendAllText(path, contents, StreamWriter.UTF8NoBOM);
        }

        /// <summary>
        /// 使用指定的编码打开一个文件，向其中追加指定的字符串，然后关闭该文件。如果文件不存在，此方法将创建一个文件，将指定的字符串写入文件，然后关闭该文件。
        /// </summary>
        /// <param name="path">要将指定的字符串追加到的文件。</param>
        /// <param name="contents">要追加到文件中的字符串。</param>
        /// <param name="encoding">要使用的字符编码。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void AppendAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalAppendAllText(path, contents, encoding);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalAppendAllText(String path, String contents, Encoding encoding)
        {
            Contract.Requires(path != null);
            Contract.Requires(encoding != null);
            Contract.Requires(path.Length > 0);

            using (StreamWriter sw = new StreamWriter(path, true, encoding))
                sw.Write(contents);
        }

        /// <summary>
        /// 向一个文件中追加行，然后关闭该文件。 如果指定文件不存在，此方法会创建一个文件，向其中写入指定的行，然后关闭该文件。
        /// </summary>
        /// <param name="path">要向其中追加行的文件。 如果文件尚不存在，则创建该文件。</param>
        /// <param name="contents">要追加到文件中的行。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void AppendAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, true, StreamWriter.UTF8NoBOM), contents);
        }

        /// <summary>
        /// 使用指定的编码向一个文件中追加行，然后关闭该文件。 如果指定文件不存在，此方法会创建一个文件，向其中写入指定的行，然后关闭该文件。
        /// </summary>
        /// <param name="path">要向其中追加行的文件。 如果文件尚不存在，则创建该文件。</param>
        /// <param name="contents">要追加到文件中的行。</param>
        /// <param name="encoding">要使用的字符编码。 </param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void AppendAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            Contract.EndContractBlock();

            InternalWriteAllLines(new StreamWriter(path, true, encoding), contents);
        }

        // Moves a specified file to a new location and potentially a new file name.
        // This method does work across volumes.
        //
        // The caller must have certain FileIOPermissions.  The caller must
        // have Read and Write permission to 
        // sourceFileName and Write 
        // permissions to destFileName.
        //
        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Move(String sourceFileName, String destFileName) {
            InternalMove(sourceFileName, destFileName, true);
        }


        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static void UnsafeMove(String sourceFileName, String destFileName) {
            InternalMove(sourceFileName, destFileName, false);
        }

        [System.Security.SecurityCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalMove(String sourceFileName, String destFileName, bool checkHost) {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();
            
            String fullSourceFileName = Path.GetFullPathInternal(sourceFileName);
            String fullDestFileName = Path.GetFullPathInternal(destFileName);

#if FEATURE_CORECLR
            if (checkHost) {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, sourceFileName, fullSourceFileName);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
                sourceState.EnsureState();
                destState.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, fullSourceFileName, false, false);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, fullDestFileName, false, false);
#endif

            if (!InternalExists(fullSourceFileName))
                __Error.WinIOError(Win32Native.ERROR_FILE_NOT_FOUND, fullSourceFileName);
            
            if (!Win32Native.MoveFile(fullSourceFileName, fullDestFileName))
            {
                __Error.WinIOError();
            }
        }

        /// <summary>
        /// 使用其他文件的内容替换指定文件的内容，这一过程将删除原始文件，并创建被替换文件的备份。
        /// </summary>
        /// <param name="sourceFileName">替换由 destinationFileName 指定的文件的文件名。ss</param>
        /// <param name="destinationFileName">被替换文件的名称。</param>
        /// <param name="destinationBackupFileName">备份文件的名称。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
            Contract.EndContractBlock();

            InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, false);
        }

        /// <summary>
        /// 用其他文件的内容替换指定文件的内容，这一过程将删除原始文件，并创建被替换文件的备份，还可以忽略合并错误。
        /// </summary>
        /// <param name="sourceFileName">替换由 destinationFileName 指定的文件的文件名。</param>
        /// <param name="destinationFileName">被替换文件的名称。</param>
        /// <param name="destinationBackupFileName">备份文件的名称。</param>
        /// <param name="ignoreMetadataErrors">如果忽略从被替换文件到替换文件的合并错误（如特性和访问控制列表 (ACL)），则为 true，否则为 false。</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
            Contract.EndContractBlock();

            InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        }

        [System.Security.SecuritySafeCritical]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static void InternalReplace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            Contract.Requires(sourceFileName != null);
            Contract.Requires(destinationFileName != null);

            // Write permission to all three files, read permission to source 
            // and dest.
            String fullSrcPath = Path.GetFullPathInternal(sourceFileName);
            String fullDestPath = Path.GetFullPathInternal(destinationFileName);
            String fullBackupPath = null;
            if (destinationBackupFileName != null)
                fullBackupPath = Path.GetFullPathInternal(destinationBackupFileName);

#if FEATURE_CORECLR
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, sourceFileName, fullSrcPath);
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, destinationFileName, fullDestPath);
            FileSecurityState backupState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, destinationBackupFileName, fullBackupPath);
            sourceState.EnsureState();
            destState.EnsureState();
            backupState.EnsureState();
#else
            FileIOPermission perm = new FileIOPermission(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, new String[] { fullSrcPath, fullDestPath});
            if (destinationBackupFileName != null)
                perm.AddPathList(FileIOPermissionAccess.Write, fullBackupPath);
            perm.Demand();
#endif

            int flags = Win32Native.REPLACEFILE_WRITE_THROUGH;
            if (ignoreMetadataErrors)
                flags |= Win32Native.REPLACEFILE_IGNORE_MERGE_ERRORS;

            bool r = Win32Native.ReplaceFile(fullDestPath, fullSrcPath, fullBackupPath, flags, IntPtr.Zero, IntPtr.Zero);
            if (!r)
                __Error.WinIOError();
        }


        // Returns 0 on success, otherwise a Win32 error code.  Note that
        // classes should use -1 as the uninitialized state for dataInitialized.
        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        internal static int FillAttributeInfo(String path, ref Win32Native.WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain, bool returnErrorOnNotFound)
        {
            int dataInitialised = 0;
            if (tryagain) // someone has a handle to the file open, or other error
            {
                Win32Native.WIN32_FIND_DATA findData;
                findData =  new Win32Native.WIN32_FIND_DATA (); 
                
                // Remove trialing slash since this can cause grief to FindFirstFile. You will get an invalid argument error
                String tempPath = path.TrimEnd(new char [] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar});

                // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    bool error = false;
                    SafeFindHandle handle = Win32Native.FindFirstFile(tempPath,findData);
                    try {
                        if (handle.IsInvalid) {
                            error = true;
                            dataInitialised = Marshal.GetLastWin32Error();
                            
                            if (dataInitialised == Win32Native.ERROR_FILE_NOT_FOUND ||
                                dataInitialised == Win32Native.ERROR_PATH_NOT_FOUND ||
                                dataInitialised == Win32Native.ERROR_NOT_READY)  // floppy device not ready
                            {
                                if (!returnErrorOnNotFound) {
                                    // Return default value for backward compatibility
                                    dataInitialised = 0;
                                    data.fileAttributes = -1;
                                }
                            }
                            return dataInitialised;
                        }
                    }
                    finally {
                        // Close the Win32 handle
                        try {
                            handle.Close();
                        }
                        catch {
                            // if we're already returning an error, don't throw another one. 
                            if (!error) {
                                Contract.Assert(false, "File::FillAttributeInfo - FindClose failed!");
                                __Error.WinIOError();
                            }
                        }
                    }
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }

                // Copy the information to data
                data.PopulateFrom(findData);
            }
            else
            {   
                                  
                 // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                bool success = false;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try {
                    success = Win32Native.GetFileAttributesEx(path, GetFileExInfoStandard, ref data);
                }
                finally {
                    Win32Native.SetErrorMode(oldMode);
                }

                if (!success) {
                    dataInitialised = Marshal.GetLastWin32Error();
                    if (dataInitialised != Win32Native.ERROR_FILE_NOT_FOUND &&
                        dataInitialised != Win32Native.ERROR_PATH_NOT_FOUND &&
                        dataInitialised != Win32Native.ERROR_NOT_READY)  // floppy device not ready
                    {
                     // In case someone latched onto the file. Take the perf hit only for failure
                        return FillAttributeInfo(path, ref data, true, returnErrorOnNotFound);
                    }
                    else {
                        if (!returnErrorOnNotFound) {
                            // Return default value for backward compbatibility
                            dataInitialised = 0;
                            data.fileAttributes = -1;
                        }
                    }
                }
            }

            return dataInitialised;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        private static FileStream OpenFile(String path, FileAccess access, out SafeFileHandle handle)
        {
            FileStream fs = new FileStream(path, FileMode.Open, access, FileShare.ReadWrite, 1);
            handle = fs.SafeFileHandle;

            if (handle.IsInvalid) {
                // Return a meaningful error, using the RELATIVE path to
                // the file to avoid returning extra information to the caller.
            
                // NT5 oddity - when trying to open "C:\" as a FileStream,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                int hr = Marshal.GetLastWin32Error();
                String FullPath = Path.GetFullPathInternal(path);
                if (hr==__Error.ERROR_PATH_NOT_FOUND && FullPath.Equals(Directory.GetDirectoryRoot(FullPath)))
                    hr = __Error.ERROR_ACCESS_DENIED;


                __Error.WinIOError(hr, path);
            }
            return fs;
        }


         // Defined in WinError.h
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_ACCESS_DENIED = 0x5;     
    }
}
