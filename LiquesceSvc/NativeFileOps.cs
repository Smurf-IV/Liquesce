#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="NativeFileOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011 Smurf-IV
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
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using DokanNet;
using Microsoft.Win32.SafeHandles;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace LiquesceSvc
{
   [SuppressUnmanagedCodeSecurity]
   [SecurityCritical]
   internal class NativeFileOps
   {
      public string FullName { get; private set; }
      private SafeFileHandle handle { get; set; }

      public bool IsInvalid
      {
         get { return handle.IsInvalid; }
      }

      public NativeFileOps(string fullName, SafeFileHandle handle)
      {
         this.handle = handle;
         FullName = fullName;
      }

      /// <summary>
      /// Call the native function to create an appropriate wrapper for the File Operations
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
            throw new System.ComponentModel.Win32Exception();
         }
         return new NativeFileOps(lpFileName, handle);
      }

      public void SetFilePointer(long offset, SeekOrigin origin)
      {
         if (!SetFilePointerEx(handle, offset, IntPtr.Zero, (int)origin))
            throw new System.ComponentModel.Win32Exception();
      }

      public int ReadFile(IntPtr bytes, uint numBytesToRead, out uint numBytesRead_mustBeZero)
      {
         return ReadFile(handle, bytes, numBytesToRead, out numBytesRead_mustBeZero, IntPtr.Zero);
      }

      public void Close()
      {
         handle.Close();
      }

      public int WriteFile(IntPtr buffer, uint numBytesToWrite, out uint numBytesWritten)
      {
         return WriteFile(handle, buffer, numBytesToWrite, out numBytesWritten, IntPtr.Zero);
      }

      public void FlushFileBuffers()
      {
         if (!FlushFileBuffers(handle))
            throw new System.ComponentModel.Win32Exception();
      }

      public void GetFileInformationByHandle(ref BY_HANDLE_FILE_INFORMATION lpFileInformation)
      {
         if (!GetFileInformationByHandle(handle, ref lpFileInformation))
            throw new System.ComponentModel.Win32Exception();
      }

      public void SetFileTime(ref FILETIME lpCreationTime, ref FILETIME lpLastAccessTime, ref FILETIME lpLastWriteTime)
      {
         if (!SetFileTime(handle, ref lpCreationTime, ref lpLastAccessTime, ref lpLastWriteTime))
            throw new System.ComponentModel.Win32Exception();
      }

      /// <summary>
      /// Will throw exceptions if it fails
      /// </summary>
      /// <param name="length"></param>
      public void SetLength(long length)
      {
         SetFilePointer(length, SeekOrigin.Begin);
         if ( !SetEndOfFile(handle) )
            throw new System.ComponentModel.Win32Exception();
      }

      public void LockFile(long offset, long length)
      {
         if ( !LockFile(handle, (int) offset, (int) (offset >> 32), (int) length, (int) (length >> 32) ) )
            throw new System.ComponentModel.Win32Exception();
      }

      public void UnlockFile(long offset, long length)
      {
         if ( !UnlockFile(handle, (int)offset, (int)(offset >> 32), (int)length, (int)(length >> 32) ) )
            throw new System.ComponentModel.Win32Exception();
      }

      #region DLL Imports
      // Stolen from http://www.123aspx.com/Rotor/RotorSrc.aspx?rot=42415

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

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool CloseHandle(IntPtr handle);

      [DllImport("kernel32.dll")]
      private static extern int GetFileType(SafeFileHandle handle);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool SetEndOfFile(SafeFileHandle hFile);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool FlushFileBuffers(SafeFileHandle hFile);

      [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      static extern bool SetFilePointerEx(SafeFileHandle Handle, Int64 i64DistanceToMove, IntPtr ptrNewFilePointer, int origin);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int WriteFile(SafeFileHandle handle, IntPtr buffer,
                                          uint numBytesToWrite, out uint numBytesWritten, IntPtr /*NativeOverlapped* */ lpOverlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int ReadFile(SafeFileHandle handle, IntPtr bytes,
                                         uint numBytesToRead, out uint numBytesRead_mustBeZero, IntPtr /*NativeOverlapped* */ overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileTime(SafeFileHandle hFile, ref FILETIME lpCreationTime, ref FILETIME lpLastAccessTime, ref FILETIME lpLastWriteTime);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, [MarshalAs(UnmanagedType.Struct)] ref BY_HANDLE_FILE_INFORMATION lpFileInformation);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);


      #endregion

   }
}
