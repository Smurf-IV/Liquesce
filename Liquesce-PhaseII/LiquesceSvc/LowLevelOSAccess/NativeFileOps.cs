#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="NativeFileOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011-2012 Simon Coghlan (Aka Smurf-IV)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using NLog;

namespace LiquesceSvc
{
   /// <summary>
   /// Some of this has been inspired by .Net code from System\IO\File.cs
   /// </summary>
   [SuppressUnmanagedCodeSecurity]
   [SecurityCritical]
   internal class NativeFileOps
   {
      public string FullName { get; private set; }
      private readonly SafeFileHandle handle;
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// Not all file systems can record creation and last access times, and not all file systems record them in the same manner. 
      /// For example, on the FAT file system, create time has a resolution of 10 milliseconds, write time has a resolution of 2 seconds,
      /// and access time has a resolution of 1 day. The NTFS file system delays updates to the last access time for a file by up to 
      /// 1 hour after the last access. For more information. See http://msdn.microsoft.com/en-us/library/windows/desktop/aa365740%28v=VS.85%29.aspx
      /// </summary>
      private BY_HANDLE_FILE_INFORMATION? cachedFileInformation;
      private WIN32_FILE_ATTRIBUTE_DATA? cachedAttributeData;

      public bool IsInvalid
      {
         get { return handle.IsInvalid; }
      }

      public NativeFileOps(string fullName)
         : this(fullName, new SafeFileHandle(IntPtr.Zero, false))
      {
      }

      public NativeFileOps(string fullName, SafeFileHandle handle)
      {
         this.handle = handle;
         FullName = GetFullPathName(fullName);
      }

      /// <summary>
      /// Taken from System\IO\Path.cs
      /// and then remove the checks to allow speedier combines
      /// </summary>
      /// <param name="path1"></param>
      /// <param name="path2"></param>
      /// <returns></returns>
      public static string CombineNoChecks(string path1, string path2)
      {
         if (path2.Length == 0)
            return path1;
         if (path1.Length == 0 || Path.IsPathRooted(path2))
            return path2;
         return AddTrailingSeperator(path1) + path2;
      }

      /// <summary>
      /// Call the native function to create an appropriate wrapper for the File Operations
      /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa363858%28v=vs.85%29.aspx
      /// </summary>
      /// <param name="lpFileName"></param>
      /// <param name="dwDesiredAccess"></param>
      /// <param name="dwShareMode"></param>
      /// <param name="dwCreationDisposition"></param>
      /// <param name="dwFlagsAndAttributes"></param>
      /// <returns>New class object or an exception</returns>
      public static NativeFileOps CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint dwCreationDisposition, uint dwFlagsAndAttributes)
      {
         SafeFileHandle handle = CreateFileW(lpFileName, dwDesiredAccess, dwShareMode, IntPtr.Zero,
                                                    dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
         if (handle.IsInvalid)
         {
            throw new Win32Exception();
         }
         return new NativeFileOps(lpFileName, handle);
      }

      static public void CreateDirectory(string pathName)
      {
         Directory.CreateDirectory(pathName);
      }

      public void CreateDirectory()
      {
         if (!Exists)
         {
            CreateDirectory(FullName);
         }
      }

      static public void DeleteDirectory(string path)
      {
         if (!RemoveDirectory(path))
            throw new Win32Exception();
      }

      public void DeleteDirectory()
      {
         if (Exists)
         {
            DeleteDirectory(FullName);
            RemoveCachedFileInformation();
         }
      }

      static public void DeleteFile(string path)
      {
         if (!DeleteFileW(path))
            throw new Win32Exception();
      }
      public void DeleteFile()
      {
         if (Exists)
         {
            DeleteFile(FullName);
            RemoveCachedFileInformation();
         }
      }

      public void SetFilePointer(long offset, SeekOrigin origin)
      {
         if (!SetFilePointerEx(handle, offset, IntPtr.Zero, (int)origin))
            throw new Win32Exception();
         RemoveCachedFileInformation();
      }

      public int ReadFile(byte[] bytes, int numBytesToRead, out int numBytesRead_mustBeZero)
      {
         numBytesRead_mustBeZero = 0;
         try
         {
            return ReadFile(handle, bytes, numBytesToRead, ref numBytesRead_mustBeZero, null);
         }
         finally
         {
            if (numBytesRead_mustBeZero != 0)
               RemoveCachedFileInformation();
         }
      }

      public void Close()
      {
         handle.Close();
      }

      public int WriteFile(byte[] buffer, int numBytesToWrite, out int numBytesWritten)
      {
         numBytesWritten = 0;
         try
         {
            return WriteFile(handle, buffer, numBytesToWrite, ref numBytesWritten, null);
         }
         finally
         {
            if (numBytesWritten != 0)
               RemoveCachedFileInformation();
         }
      }

      public void FlushFileBuffers()
      {
         if (!FlushFileBuffers(handle))
            throw new Win32Exception();
      }

      public void GetFileInformationByHandle(ref BY_HANDLE_FILE_INFORMATION lpFileInformation)
      {
         BY_HANDLE_FILE_INFORMATION? local = cachedFileInformation;
         if (local.HasValue)
            lpFileInformation = local.Value;
         else if (!GetFileInformationByHandle(handle, ref lpFileInformation))
            throw new Win32Exception();
         else
         {
            // Ensure that the attributes stored are the Extended variety
            lpFileInformation.dwFileAttributes = Attributes;
            cachedFileInformation = lpFileInformation;
         }
      }

      static public string GetRootOrMountFor(string path)
      {
         do
         {
            NativeFileOps dirInfo = new NativeFileOps(path);
            EFileAttributes attr = (EFileAttributes) dirInfo.Attributes;
            if ((attr & EFileAttributes.ReparsePoint) == EFileAttributes.ReparsePoint)
            {
               path = AddTrailingSeperator(path);
               const int MaxVolumeNameLength = 100;
               StringBuilder sb = new StringBuilder(MaxVolumeNameLength);
               if (GetVolumeNameForVolumeMountPointW(path, sb, MaxVolumeNameLength))
                  return sb.ToString();
            }
            DirectoryInfo tmp = Directory.GetParent(path);
            if (tmp == null)
               return AddTrailingSeperator(path);
            path = tmp.FullName;
         } while (!string.IsNullOrEmpty(path));
         return path;
      }

      private static string AddTrailingSeperator(string path)
      {
         char ch = path[path.Length - 1];
         if ((ch != Path.DirectorySeparatorChar)
            && (ch != Path.AltDirectorySeparatorChar)
            )
            path += Path.DirectorySeparatorChar;
         return path;
      }

      private void RemoveCachedFileInformation()
      {
         cachedFileInformation = null;
         cachedAttributeData = null;
      }

      public static WIN32_FIND_FILETIME ConvertDateTimeToFiletime(DateTime time)
      {
         WIN32_FIND_FILETIME ft;
         long hFT1 = time.ToFileTimeUtc();
         ft.dwLowDateTime = (uint)(hFT1 & 0xFFFFFFFF);
         ft.dwHighDateTime = (uint)(hFT1 >> 32);
         return ft;
      }

      public static DateTime ConvertFileTimeToDateTime(WIN32_FIND_FILETIME data)
      {
         return DateTime.FromFileTimeUtc((long)data.dwHighDateTime << 32 | data.dwLowDateTime);
      }

      public void SetFileTime(DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
      {
         WIN32_FIND_FILETIME lpCreationTime = ConvertDateTimeToFiletime(creationTime);
         WIN32_FIND_FILETIME lpLastAccessTime = ConvertDateTimeToFiletime(lastAccessTime);
         WIN32_FIND_FILETIME lpLastWriteTime = ConvertDateTimeToFiletime(lastWriteTime);
         if (!SetFileTime(handle, ref lpCreationTime, ref lpLastAccessTime, ref lpLastWriteTime))
            throw new Win32Exception();
         RemoveCachedFileInformation();
      }

      /// <summary>
      /// Will throw exceptions if it fails
      /// </summary>
      /// <param name="length"></param>
      public void SetLength(long length)
      {
         SetFilePointer(length, SeekOrigin.Begin);
         if (!SetEndOfFile(handle))
            throw new Win32Exception();
         RemoveCachedFileInformation();
      }

      public void LockFile(long offset, long length)
      {
         if (!LockFile(handle, (int)offset, (int)(offset >> 32), (int)length, (int)(length >> 32)))
            throw new Win32Exception();
      }

      public void UnlockFile(long offset, long length)
      {
         if (!UnlockFile(handle, (int)offset, (int)(offset >> 32), (int)length, (int)(length >> 32)))
            throw new Win32Exception();
      }

      private void CheckData()
      {
         WIN32_FILE_ATTRIBUTE_DATA? local = cachedAttributeData;
         if (!local.HasValue)
         {
            WIN32_FILE_ATTRIBUTE_DATA newData;
            if (GetFileAttributesEx(FullName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out newData))
               cachedAttributeData = newData;
         }
      }

      public uint Attributes
      {
         get
         {
            return (Exists) ? cachedAttributeData.Value.dwFileAttributes : 0;
         }
      }

      public string DirectoryPathOnly
      {
         get
         {
            // Stolen from Path.GetDirectoryName() then simplified
            int length = FullName.Length;
            do
            {
            } while ((FullName[--length] != Path.DirectorySeparatorChar)
               && (FullName[length] != Path.AltDirectorySeparatorChar)
               );
            return FullName.Substring(0, length);
         }
      }

      public long Length
      {
         get
         {
            return (!Exists || IsDirectory) ? 0 : (long)cachedAttributeData.Value.nFileSizeHigh << 32 | (long)cachedAttributeData.Value.nFileSizeLow & (long)UInt32.MaxValue;
         }
      }

      public bool Exists
      {
         get
         {
            try
            {
               CheckData();
               return cachedAttributeData.HasValue;
            }
            catch
            {
               return false;
            }
         }
      }

      public bool IsDirectory
      {
         get
         {
            try
            {
               return ((EFileAttributes)Attributes & EFileAttributes.Directory) == EFileAttributes.Directory;
            }
            catch (Exception)
            {
               return false;
            }
         }
      }

      public bool IsEmptyDirectory
      {
         get
         {
            return !String.IsNullOrEmpty(FullName) && IsDirectory &&
               (FullName.Length <= MAX_PATH ? PathIsDirectoryEmptyW(FullName) : NativeFileFind.IsDirEmpty(FullName));
         }
      }


      public void SetFileAttributes(uint attr)
      {
         if (!SetFileAttributesW(FullName, (FileAttributes)attr))
            throw new Win32Exception();
         RemoveCachedFileInformation();
      }

      private static string GetFullPathName(string startPath)
      {
         StringBuilder buffer = new StringBuilder(MAX_PATH + 1);
         int fullPathNameLength = GetFullPathNameW(startPath, MAX_PATH + 1, buffer, IntPtr.Zero);
         if (fullPathNameLength > MAX_PATH)
         {
            buffer.Length = fullPathNameLength;
            fullPathNameLength = GetFullPathNameW(startPath, fullPathNameLength, buffer, IntPtr.Zero);
         }
         if (fullPathNameLength == 0)
         {
            throw new Win32Exception();
         }
         return buffer.ToString();
      }

      #region DLL Imports
      // Stolen from http://www.123aspx.com/Rotor/RotorSrc.aspx?rot=42415

      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      public struct SECURITY_ATTRIBUTES
      {
         public int nLength;
         public IntPtr lpSecurityDescriptor;
         public int bInheritHandle;
      }

      #region Create File

      /// <summary>
      /// The CreateFile function creates or opens a file, file stream, directory, physical disk, volume, console buffer, tape drive,
      /// communications resource, mailslot, or named pipe. The function returns a handle that can be used to access an object.
      /// </summary>
      /// <param name="lpFileName"></param>
      /// <param name="dwDesiredAccess"> access to the object, which can be read, write, or both</param>
      /// <param name="dwShareMode">The sharing mode of an object, which can be read, write, both, or none</param>
      /// <param name="SecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether or not the returned handle can
      /// be inherited by child processes. Can be null</param>
      /// <param name="dwCreationDisposition">An action to take on files that exist and do not exist</param>
      /// <param name="dwFlagsAndAttributes">The file attributes and flags. </param>
      /// <param name="hTemplateFile">A handle to a template file with the GENERIC_READ access right. The template file supplies file attributes
      /// and extended attributes for the file that is being created. This parameter can be null</param>
      /// <returns>If the function succeeds, the return value is an open handle to a specified file. If a specified file exists before the function
      /// call and dwCreationDisposition is CREATE_ALWAYS or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function
      /// succeeds. If a file does not exist before the call, GetLastError returns 0 (zero).
      /// If the function fails, the return value is INVALID_HANDLE_VALUE. To get extended error information, call GetLastError.
      /// </returns>
      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall,
         SetLastError = true)]
      private static extern SafeFileHandle CreateFileW(
         string lpFileName,
         uint dwDesiredAccess,
         uint dwShareMode,
         IntPtr SecurityAttributes,
         uint dwCreationDisposition,
         uint dwFlagsAndAttributes,
         IntPtr hTemplateFile
         );

      // ReSharper disable UnusedMember.Global
      [Flags]
      public enum EFileAttributes : uint
      {
         Readonly = 0x00000001,
         Hidden = 0x00000002,
         System = 0x00000004,
         Directory = 0x00000010,
         Archive = 0x00000020,
         Device = 0x00000040,
         Normal = 0x00000080,
         Temporary = 0x00000100,
         SparseFile = 0x00000200,
         ReparsePoint = 0x00000400,
         Compressed = 0x00000800,
         Offline = 0x00001000,
         NotContentIndexed = 0x00002000,
         Encrypted = 0x00004000,
         Write_Through = 0x80000000,
         Overlapped = 0x40000000,
         NoBuffering = 0x20000000,
         RandomAccess = 0x10000000,
         SequentialScan = 0x08000000,
         DeleteOnClose = 0x04000000,
         BackupSemantics = 0x02000000,
         PosixSemantics = 0x01000000,
         OpenReparsePoint = 0x00200000,
         OpenNoRecall = 0x00100000,
         FirstPipeInstance = 0x00080000
      }

      [Flags]
      public enum EFileAccess : uint
      {
         //
         // Standard Section
         //

         AccessSystemSecurity = 0x1000000, // AccessSystemAcl access type
         MaximumAllowed = 0x2000000, // MaximumAllowed access type

         Delete = 0x10000,
         ReadControl = 0x20000,
         WriteDAC = 0x40000,
         WriteOwner = 0x80000,
         Synchronize = 0x100000,

         StandardRightsRequired = 0xF0000,
         StandardRightsRead = ReadControl,
         StandardRightsWrite = ReadControl,
         StandardRightsExecute = ReadControl,
         StandardRightsAll = 0x1F0000,
         SpecificRightsAll = 0xFFFF,

         FILE_READ_DATA = 0x0001, // file & pipe
         FILE_LIST_DIRECTORY = 0x0001, // directory
         FILE_WRITE_DATA = 0x0002, // file & pipe
         FILE_ADD_FILE = 0x0002, // directory
         FILE_APPEND_DATA = 0x0004, // file
         FILE_ADD_SUBDIRECTORY = 0x0004, // directory
         FILE_CREATE_PIPE_INSTANCE = 0x0004, // named pipe
         FILE_READ_EA = 0x0008, // file & directory
         FILE_WRITE_EA = 0x0010, // file & directory
         FILE_EXECUTE = 0x0020, // file
         FILE_TRAVERSE = 0x0020, // directory
         FILE_DELETE_CHILD = 0x0040, // directory
         FILE_READ_ATTRIBUTES = 0x0080, // all
         FILE_WRITE_ATTRIBUTES = 0x0100, // all

         //
         // Generic Section
         //

         GenericRead = 0x80000000,
         GenericWrite = 0x40000000,
         GenericExecute = 0x20000000,
         GenericAll = 0x10000000,

         SPECIFIC_RIGHTS_ALL = 0x00FFFF,

         FILE_ALL_ACCESS =
            StandardRightsRequired |
            Synchronize |
            0x1FF,

         FILE_GENERIC_READ =
            StandardRightsRead |
            FILE_READ_DATA |
            FILE_READ_ATTRIBUTES |
            FILE_READ_EA |
            Synchronize,

         FILE_GENERIC_WRITE =
            StandardRightsWrite |
            FILE_WRITE_DATA |
            FILE_WRITE_ATTRIBUTES |
            FILE_WRITE_EA |
            FILE_APPEND_DATA |
            Synchronize,

         FILE_GENERIC_EXECUTE =
            StandardRightsExecute |
            FILE_READ_ATTRIBUTES |
            FILE_EXECUTE |
            Synchronize
      }
      // ReSharper restore UnusedMember.Global

      #endregion

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CreateDirectoryW(string lpPathName, IntPtr lpSecurityAttributes);

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CloseHandle(SafeFileHandle handle);

      [DllImport("kernel32.dll")]
      private static extern int GetFileType(SafeFileHandle handle);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, [MarshalAs(UnmanagedType.Struct)] ref BY_HANDLE_FILE_INFORMATION lpFileInformation);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
      private static extern int GetFullPathNameW(string path, int numBufferChars, StringBuilder buffer, IntPtr mustBeZero);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetEndOfFile(SafeFileHandle hFile);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool FlushFileBuffers(SafeFileHandle hFile);

      [DllImport("Kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool SetFilePointerEx(SafeFileHandle Handle, Int64 i64DistanceToMove, IntPtr ptrNewFilePointer, int origin);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int WriteFile(SafeFileHandle handle, byte[] lpBuffer,
                                          int numBytesToWrite, ref int lpNumBytesWritten, NativeOverlapped? lpOverlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int ReadFile(SafeFileHandle handle, byte[] lpBuffer,
                                         int numBytesToRead, ref int lpNumBytesRead_mustBeZero, NativeOverlapped? overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileTime(SafeFileHandle hFile, ref WIN32_FIND_FILETIME lpCreationTime, ref WIN32_FIND_FILETIME lpLastAccessTime, ref WIN32_FIND_FILETIME lpLastWriteTime);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);

      // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364946%28v=vs.85%29.aspx
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool GetFileAttributesEx(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetVolumeNameForVolumeMountPointW(string lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName,
                                                                   uint cchBufferLength);

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileAttributesW(string name, FileAttributes attr);

      // Stolen from 
      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      public struct WIN32_FILE_ATTRIBUTE_DATA
      {
         public uint dwFileAttributes;
         public WIN32_FIND_FILETIME ftCreationTime;
         public WIN32_FIND_FILETIME ftLastAccessTime;
         public WIN32_FIND_FILETIME ftLastWriteTime;
         public uint nFileSizeHigh;
         public uint nFileSizeLow;

         [SecurityCritical]
         internal void PopulateFrom(BY_HANDLE_FILE_INFORMATION findData)
         {
            this.dwFileAttributes = findData.dwFileAttributes;
            this.ftCreationTime = findData.ftCreationTime;
            this.ftLastAccessTime = findData.ftLastAccessTime;
            this.ftLastWriteTime = findData.ftLastWriteTime;
            this.nFileSizeHigh = findData.nFileSizeHigh;
            this.nFileSizeLow = findData.nFileSizeLow;
         }
      }

      public enum GET_FILEEX_INFO_LEVELS
      {
         GetFileExInfoStandard,
         GetFileExMaxInfoLevel
      }

      [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool PathIsDirectoryEmptyW([MarshalAs(UnmanagedType.LPWStr), In] string pszPath);

      private const int MAX_PATH = 260;
      // ReSharper disable UnusedMember.Local
      private static readonly int MaxLongPath = 32000;
      private static readonly string Prefix = "\\\\?\\";
      // ReSharper restore UnusedMember.Local


      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool RemoveDirectory(string path);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr), In] string path);

      #endregion
   }


}
