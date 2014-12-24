#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="CBFSWinUtil.cs" company="Smurf-IV">
//
//  Copyright (C) 2013-2014 Simon Coghlan (Aka Smurf-IV)
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
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion Copyright (C)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using CallbackFS;
using LiquesceSvc;
using NLog;

namespace CBFS
{
   internal static class CBFSWinUtil
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public static uint HiWord(uint number)
      {
         return (((number & 0x80000000) == 0x80000000) ? number >> 16 : (number >> 16) & 0xffff);
      }

      public static uint LoWord(uint number)
      {
         return number & 0xffff;
      }

      /// <summary>
      /// If your application needs to report some error when processing the callback, it should throw ECBFSError exception.
      /// The application must pass the error code with the exception by passing the error code as a parameter to
      /// ECBFSError constructor.
      /// Callback File System API will catch ECBFSError exception and extract the error code.
      /// The error code will be reported to the operating system.
      /// </summary>
      /// <param name="ex"></param>
      [DebuggerHidden]
      public static void BestAttemptToECBFSError(Exception ex)
      {
         Log.ErrorException("CBFSWinError", ex);
         Win32Exception win32Exception = ex as Win32Exception;
         if (win32Exception != null)
         {
            throw new ECBFSError(ex.Message, (uint)win32Exception.NativeErrorCode);
         }
         SocketException socketException = ex.InnerException as SocketException;
         if (socketException != null)
         {
            throw new ECBFSError(socketException.Message, (uint)socketException.ErrorCode);
         }
         uint HrForException = (uint)Marshal.GetHRForException(ex);
         throw new ECBFSError(ex.Message, (HiWord(HrForException) == 0x8007) ? LoWord(HrForException) : ERROR_EXCEPTION_IN_SERVICE);
      }

      /// <summary>
      /// Could use reflection to find out the API call name
      /// But then that would be used for every call so will probably be detremental to the string handling, and the .Net calls
      /// </summary>
      /// <param name="functionName">String to log out for this call</param>
      /// <param name="act">What to call from the derived class</param>
      [DebuggerHidden]
      public static void Invoke(string functionName, Action act)
      {
         Log.Trace("{0} IN", functionName);
         try
         {
            act();
         }
         catch (Exception ex)
         {
            BestAttemptToECBFSError(ex);
         }
         finally
         {
            Log.Trace("{0} OUT", functionName);
         }
      }

      // ReSharper disable InconsistentNaming
      // ReSharper disable MemberCanBePrivate.Global
#pragma warning disable 169

      #region File Operation Errors

      // ReSharper disable UnusedMember.Global

      // Check http://msdn.microsoft.com/en-us/library/ms819772.aspx (WinError.h) for error codes
      // From WinError.h -> http://msdn.microsoft.com/en-us/library/windows/desktop/ms681382%28v=vs.85%29.aspx
      public const int ERROR_FILE_NOT_FOUND = 2; // MessageText: The system cannot find the file specified.

      public const int ERROR_PATH_NOT_FOUND = 3; // MessageText: The system cannot find the path specified.
      public const int ERROR_ACCESS_DENIED = 5; // MessageText: Access is denied.
      public const int ERROR_SHARING_VIOLATION = 32;
      public const int ERROR_FILE_EXISTS = 80;
      public const int ERROR_CALL_NOT_IMPLEMENTED = 120;
      public const int ERROR_DISK_FULL = 112; // There is not enough space on the disk.
      public const int ERROR_INVALID_NAME = 123;
      public const int ERROR_DIR_NOT_EMPTY = 145; // MessageText: The directory is not empty.
      public const int ERROR_ALREADY_EXISTS = 183; // MessageText: Cannot create a file when that file already exists.

      public const int ERROR_EXCEPTION_IN_SERVICE = 1064;
      //  An exception occurred in the service when handling the control request.

      public const int ERROR_FILE_READ_ONLY = 6009; // The specified file is read only.

      public const int ERROR_SUCCESS = 0;
      public const int ERROR_NOACCESS = 998; // Invalid access to memory location.
      public const int ERROR_NOT_SUPPORTED = 50; // The request is not supported.

      public const int ERROR_INVALID_PARAMETER = 87;  // The parameter is incorrect.
      public const int ERROR_INVALID_HANDLE = 1609;   // Handle is in an invalid state.
      public const int ERROR_NOT_LOCKED = 158;        // The segment is already unlocked.
      public const int ERROR_NO_SYSTEM_RESOURCES = 1450;// Insufficient system resources exist to complete the requested service.
      public const int ERROR_NOT_ENOUGH_MEMORY = 8;   // Not enough storage is available to process this command.
      public const int ERROR_MORE_DATA = 234;         // More data is available.
      public const int ERROR_INSUFFICIENT_BUFFER = 122;// The data area passed to a system call is too small.
      public const int ERROR_NO_MORE_FILES = 18;      // There are no more files.
      public const int ERROR_INVALID_FUNCTION = 1;    // Incorrect function.
      public const int ERROR_HANDLE_EOF = 38;         // Reached the end of the file.
      public const int ERROR_DISK_CORRUPT = 1393;     // The disk structure is corrupted and unreadable.
      public const int ERROR_BAD_COMMAND = 22;        // The device does not recognize the command.
      public const int ERROR_CANNOT_MAKE = 82;        // The directory or file cannot be created.
      public const int ERROR_PROC_NOT_FOUND = 127;    // The specified procedure could not be found.
      public const int ERROR_OPERATION_ABORTED = 995; // The I/O operation has been aborted because of either a thread exit or an application request.
      public const int ERROR_IO_DEVICE = 1117;        // The request could not be performed because of an I/O device error.

      // public const uint TYPE_E_IOERROR = 0;
      public const int ERROR_BAD_UNIT = 20;           // The system cannot find the device specified.

      public const int ERROR_BAD_ARGUMENTS = 160;     // One or more arguments are not correct.
      public const int ERROR_BAD_EXE_FORMAT = 193;    // %1 is not a valid Win32 application.
      public const int ERROR_WAIT_NO_CHILDREN = 128;  // There are no child processes to wait for.
      public const int ERROR_RETRY = 1237;            // The operation could not be completed. A retry should be performed.
      public const int ERROR_INVALID_ADDRESS = 487;   // Attempt to access invalid address.
      public const int ERROR_BUSY = 170;              // The requested resource is in use.
      public const int ERROR_DIRECTORY = 267;         // The directory name is invalid.
      public const int ERROR_TOO_MANY_OPEN_FILES = 4; // The system cannot open the file.
      public const int ERROR_EA_TABLE_FULL = 277;     // The extended attribute table file is full.
      public const int ERROR_FILE_INVALID = 1006;     // The volume for a file has been externally altered so that the opened file is no longer valid.
      public const int ERROR_CONNECTION_UNAVAIL = 1201;// The device is not currently connected but it is a remembered connection.
      public const int ERROR_TOO_MANY_LINKS = 1142;   // An attempt was made to create more links on a file than the file system supports.
      public const int ERROR_BROKEN_PIPE = 109;       // The pipe has been ended.
      public const int ERROR_ARITHMETIC_OVERFLOW = 534;// Arithmetic result exceeded 32 bits.
      public const int ERROR_POSSIBLE_DEADLOCK = 1131;// A potential deadlock condition has been detected.
      public const int ERROR_BUFFER_OVERFLOW = 111;   // The file name is too long.
      public const int ERROR_TOO_MANY_SEMAPHORES = 100;// Cannot create another system semaphore.
      public const int ERROR_ARENA_TRASHED = 7;       // The storage control blocks were destroyed.
      public const int ERROR_INVALID_BLOCK = 9;       // The storage control block address is invalid.
      public const int ERROR_BAD_ENVIRONMENT = 10;    // The environment is incorrect.
      public const int ERROR_FILENAME_EXCED_RANGE = 206;// The filename or extension is too long.
      public const int ERROR_NOT_READY = 21;          // The device is not ready.
      public const int ERROR_FILE_OFFLINE = 4350;     // This file is currently not available for use on this computer.
      public const int ERROR_REMOTE_STORAGE_NOT_ACTIVE = 4351;// The remote storage service is not operational at this time.
      public const int ERROR_NO_SUCH_PRIVILEGE = 1313;// A specified privilege does not exist.
      public const int ERROR_PRIVILEGE_NOT_HELD = 1314;// A required privilege is not held by the client.
      public const int ERROR_CANNOT_IMPERSONATE = 1368;// Unable to impersonate using a named pipe until data has been read from that pipe.
      public const int ERROR_WRITE_PROTECT = 19;      // The media is write protected.
      public const int ERROR_LOGON_FAILURE = 1326;    // Logon failure: unknown user name or bad password.
      public const int ERROR_NO_SECURITY_ON_OBJECT = 1350; // Unable to perform a security operation on an object that has no associated security.

      #endregion File Operation Errors

      #region Win32 Constants for file controls

      public const uint FILE_SHARE_READ = 0x00000001;
      public const uint FILE_SHARE_WRITE = 0x00000002;
      public const uint FILE_SHARE_DELETE = 0x00000004;

      public const uint CREATE_NEW = 1;
      public const uint CREATE_ALWAYS = 2;
      public const uint OPEN_EXISTING = 3;
      public const uint OPEN_ALWAYS = 4;
      public const uint TRUNCATE_EXISTING = 5;

      #endregion Win32 Constants for file controls

      // ReSharper restore UnusedMember.Global
#pragma warning restore 169
      // ReSharper restore MemberCanBePrivate.Global
      // ReSharper restore InconsistentNaming

      [DebuggerHidden]
      public static void ThrowNotFound(uint attributes)
      {
         bool isDirectoy = IsDirectory(attributes);
         throw new ECBFSError((uint)(isDirectoy ? ERROR_PATH_NOT_FOUND : ERROR_FILE_NOT_FOUND));
      }

      public static bool IsDirectory(uint attributes)
      {
         return IsDirectory((NativeFileOps.EFileAttributes)attributes);
      }

      public static bool IsDirectory(NativeFileOps.EFileAttributes attributes)
      {
         return (attributes & NativeFileOps.EFileAttributes.Directory) == NativeFileOps.EFileAttributes.Directory;
      }
   }
}