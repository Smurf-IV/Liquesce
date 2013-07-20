#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="NativeFileOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011-2012 Smurf-IV
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
using System.Text;
using DokanNet;
using Microsoft.Win32.SafeHandles;

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

      public int ReadFile(IntPtr bytes, uint numBytesToRead, out uint numBytesRead_mustBeZero)
      {
         numBytesRead_mustBeZero = 0;
         try
         {
            return ReadFile(handle, bytes, numBytesToRead, out numBytesRead_mustBeZero, IntPtr.Zero);
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

      public int WriteFile(IntPtr buffer, uint numBytesToWrite, out uint numBytesWritten)
      {
         numBytesWritten = 0;
         try
         {
            return WriteFile(handle, buffer, numBytesToWrite, out numBytesWritten, IntPtr.Zero);
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
            FileAttributes attr = dirInfo.Attributes;
            if ((attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
               path = AddTrailingSeperator(path);
               const int MaxVolumeNameLength = 100;
               StringBuilder sb = new StringBuilder(MaxVolumeNameLength);
               if (GetVolumeNameForVolumeMountPointW(path, sb, (uint)MaxVolumeNameLength))
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

      public void SetFileTime(ref WIN32_FIND_FILETIME lpCreationTime, ref WIN32_FIND_FILETIME lpLastAccessTime, ref WIN32_FIND_FILETIME lpLastWriteTime)
      {
         if (!SetFileTime(handle, ref lpCreationTime, ref lpLastAccessTime, ref lpLastWriteTime))
            throw new Win32Exception();
         RemoveCachedFileInformation();
      }

      public static DateTime ConvertFileTimeToDateTime(WIN32_FIND_FILETIME data)
      {
         return DateTime.FromFileTimeUtc((long)data.dwHighDateTime << 32 | data.dwLowDateTime);
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

      public FileAttributes Attributes
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
               return (Attributes & FileAttributes.Directory) == FileAttributes.Directory;
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


      public void SetFileAttributes(FileAttributes attr)
      {
         if (!SetFileAttributesW(FullName, attr))
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
      /// all and dwCreationDisposition is CREATE_ALWAYS or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function
      /// succeeds. If a file does not exist before the call, GetLastError returns 0 (zero).
      /// If the function fails, the return value is INVALID_HANDLE_VALUE. To get extended error information, call GetLastError.
      /// </returns>
      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
      private static extern SafeFileHandle CreateFileW(
         string lpFileName,
         uint dwDesiredAccess,
         uint dwShareMode,
         IntPtr SecurityAttributes,
         uint dwCreationDisposition,
         uint dwFlagsAndAttributes,
         IntPtr hTemplateFile
         );

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
      private static extern int WriteFile(SafeFileHandle handle, IntPtr buffer,
                                          uint numBytesToWrite, out uint numBytesWritten, IntPtr /*NativeOverlapped* */ lpOverlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int ReadFile(SafeFileHandle handle, IntPtr bytes,
                                         uint numBytesToRead, out uint numBytesRead_mustBeZero, IntPtr /*NativeOverlapped* */ overlapped);

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
         public FileAttributes dwFileAttributes;
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

      private static readonly int MAX_PATH = 260;
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
