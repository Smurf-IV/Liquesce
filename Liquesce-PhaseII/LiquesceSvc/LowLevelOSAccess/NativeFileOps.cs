#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="NativeFileOps.cs" company="Smurf-IV">
//
//  Copyright (C) 2011-2014 Simon Coghlan (Aka Smurf-IV)
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

#endregion Copyright (C)

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using CBFS;
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

      public bool ForceUseAsReadOnly { get; private set; }

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

      public NativeFileOps(string fullName, bool forceUseAsReadOnly)
         : this(fullName, new SafeFileHandle(IntPtr.Zero, false), forceUseAsReadOnly)
      {
      }

      private NativeFileOps(string fullName, SafeFileHandle handle, bool forceUseAsReadOnly)
      {
         this.handle = handle;
         if (!handle.IsInvalid
            && !handle.IsClosed
            )
         {
            IncrementOpenCount();
         }
         FullName = GetFullPathName(fullName);
         ForceUseAsReadOnly = forceUseAsReadOnly;
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
         {
            return path1;
         }
         if (path1.Length == 0 || Path.IsPathRooted(path2))
         {
            return path2;
         }
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
         return new NativeFileOps(lpFileName, handle, false);
      }

      static public void CreateDirectory(string pathName)
      {
         DirectoryInfo dirInfo = new DirectoryInfo(pathName);
         if (!dirInfo.Exists)
         {
            dirInfo.Create();
            DirectorySecurity sec = dirInfo.GetAccessControl();
            // Using this instead of the "Everyone" string means we work on non-English systems.
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            // Aim for: Everyone | Modify, Synchronize | ContainerInherit, ObjectInherit | None | Allow
            sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize,
               InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            dirInfo.SetAccessControl(sec);
         }
      }

      public void CreateDirectory()
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         CreateDirectory(FullName);
      }

      static public void DeleteDirectory(string path)
      {
         if (!RemoveDirectory(path))
         {
            throw new Win32Exception();
         }
      }

      public void DeleteDirectory()
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         if (Exists)
         {
            DeleteDirectory(FullName);
            RemoveCachedFileInformation();
         }
      }

      static public void DeleteFile(string path)
      {
         if (!DeleteFileW(path))
         {
            throw new Win32Exception();
         }
      }

      public void DeleteFile()
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         if (Exists)
         {
            DeleteFile(FullName);
            RemoveCachedFileInformation();
         }
      }

      public void SetFilePointer(long offset, SeekOrigin origin)
      {
         if (!SetFilePointerEx(handle, offset, IntPtr.Zero, (int)origin))
         {
            throw new Win32Exception();
         }
         RemoveCachedFileInformation();
      }

      public bool ReadFile(byte[] bytes, UInt32 numBytesToRead, out UInt32 numBytesRead_mustBeZero)
      {
// ReSharper disable once RedundantAssignment
         numBytesRead_mustBeZero = 0;
         //NativeOverlapped overlapped = new NativeOverlapped();
         return ReadFile(handle, bytes, numBytesToRead, out numBytesRead_mustBeZero, IntPtr.Zero);
      }

      public void Close()
      {
         if (!handle.IsInvalid
             && !handle.IsClosed
            )
         {
            handle.Close();
         }
      }

      public bool WriteFile(byte[] buffer, UInt32 numBytesToWrite, out UInt32 numBytesWritten)
      {
         numBytesWritten = 0;
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         try
         {
            //NativeOverlapped overlapped = new NativeOverlapped();
            return WriteFile(handle, buffer, numBytesToWrite, out numBytesWritten, IntPtr.Zero);
         }
         finally
         {
            if (numBytesWritten != 0)
            {
               RemoveCachedFileInformation();
            }
         }
      }

      public void FlushFileBuffers()
      {
         if (!FlushFileBuffers(handle))
         {
            throw new Win32Exception();
         }
      }

      public BY_HANDLE_FILE_INFORMATION GetFileInformationByHandle()
      {
         BY_HANDLE_FILE_INFORMATION lpFileInformation = new BY_HANDLE_FILE_INFORMATION();
         BY_HANDLE_FILE_INFORMATION? local = cachedFileInformation;
         if (local.HasValue)
         {
            lpFileInformation = local.Value;
         }
         else if (!GetFileInformationByHandle(handle, ref lpFileInformation))
         {
            throw new Win32Exception();
         }
         else
         {
            // Ensure that the attributes stored are the Extended variety
            lpFileInformation.dwFileAttributes = Attributes;
            if (ForceUseAsReadOnly)
            {
               lpFileInformation.dwFileAttributes |= (uint)EFileAttributes.Readonly;
            }
            cachedFileInformation = lpFileInformation;
         }
         return lpFileInformation;
      }

      static public string GetRootOrMountFor(string path)
      {
         do
         {
            NativeFileOps dirInfo = new NativeFileOps(path, false);
            EFileAttributes attr = (EFileAttributes)dirInfo.Attributes;
            if ((attr & EFileAttributes.ReparsePoint) == EFileAttributes.ReparsePoint)
            {
               path = AddTrailingSeperator(path);
               const int MaxVolumeNameLength = 100;
               StringBuilder sb = new StringBuilder(MaxVolumeNameLength);
               if (GetVolumeNameForVolumeMountPointW(path, sb, MaxVolumeNameLength))
               {
                  return sb.ToString();
               }
            }
            string tmp = GetParentPathName(path);
            if ( string.IsNullOrEmpty(tmp))
            {
               return AddTrailingSeperator(path);
            }
            path = tmp;
         } while (!string.IsNullOrEmpty(path));
         return path;
      }

      private static string AddTrailingSeperator(string path)
      {
         char ch = path[path.Length - 1];
         if (!IsDirectorySeparator(ch))
         {
            path += Path.DirectorySeparatorChar;
         }
         return path;
      }

      private void RemoveCachedFileInformation()
      {
         cachedFileInformation = null;
         cachedAttributeData = null;
      }

      private static long ConvertDateTimeToFiletime(DateTime time)
      {
         return (time == DateTime.MinValue) ? 0 : time.ToFileTimeUtc();
      }

      public static DateTime ConvertFileTimeToDateTime(WIN32_FIND_FILETIME data)
      {
         long fileTime = ((long)data.dwHighDateTime << 32) | data.dwLowDateTime;
         return (fileTime != 0) ? DateTime.FromFileTimeUtc(fileTime) : DateTime.MinValue;
      }

      public void SetFileTime(DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         long lpCreationTime = ConvertDateTimeToFiletime(creationTime);
         long lpLastAccessTime = ConvertDateTimeToFiletime(lastAccessTime);
         long lpLastWriteTime = ConvertDateTimeToFiletime(lastWriteTime);
         if (!SetFileTime(handle, ref lpCreationTime, ref lpLastAccessTime, ref lpLastWriteTime))
         {
            throw new Win32Exception();
         }
         RemoveCachedFileInformation();
      }

      /// <summary>
      /// Will throw exceptions if it fails
      /// </summary>
      /// <param name="length"></param>
      public void SetLength(long length)
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         SetFilePointer(length, SeekOrigin.Begin);
         if (!SetEndOfFile(handle))
         {
            throw new Win32Exception();
         }
         RemoveCachedFileInformation();
      }

      private void CheckData()
      {
         WIN32_FILE_ATTRIBUTE_DATA? local = cachedAttributeData;
         if (!local.HasValue)
         {
            WIN32_FILE_ATTRIBUTE_DATA newData;
            if (GetFileAttributesEx(FullName, GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out newData))
            {
               if (ForceUseAsReadOnly)
               {
                  newData.dwFileAttributes |= (uint)EFileAttributes.Readonly;
               }
               cachedAttributeData = newData;
            }
         }
      }

      public uint Attributes
      {
         get
         {
            return (Exists) ? cachedAttributeData.Value.dwFileAttributes : 0;
         }
      }

      /// <summary>
      /// Returns the FileId
      /// </summary>
      /// <remarks>
      /// May return 0 if not found
      /// </remarks>
      public long FileId
      {
         get
         {
            if (Exists)
            {
               if (!cachedFileInformation.HasValue)
               {
                  GetFileInformationByHandle();
               }
               return (((long) cachedFileInformation.Value.nFileIndexHigh) << 32) + cachedFileInformation.Value.nFileIndexLow;
            }
            return 0;
         }
      }

      public string DirectoryPathOnly
      {
         get
         {
            // Stolen from Path.GetDirectoryName() then simplified
            int length = FullName.Length;
            char ch;
            do
            {
               ch = FullName[--length];
            } while (!IsDirectorySeparator(ch));
            return FullName.Substring(0, length);
         }
      }

      public string FileName
      {
         get
         {
            // Stolen from Path.GetFileName() then simplified
            int length = FullName.Length;
            int index = length;
            char ch;
            do
            {
               ch = FullName[--index];
            } while (!IsDirectorySeparator(ch));
            return FullName.Substring(index + 1, length - index - 1);
         }
      }

      public long Length
      {
         get
         {
            return (!Exists || IsDirectory) ? 0 : ((long)cachedAttributeData.Value.nFileSizeHigh << 32) | (cachedAttributeData.Value.nFileSizeLow & UInt32.MaxValue);
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

      public WIN32_FIND_DATA GetFindData()
      {
         WIN32_FIND_DATA findData = new WIN32_FIND_DATA();
         WIN32_FIND_DATA win32FindData = (NativeFileFind.FindFirstOnly(FullName, ref findData) ? findData : new WIN32_FIND_DATA());
         if (ForceUseAsReadOnly)
         {
            win32FindData.dwFileAttributes |= (uint)EFileAttributes.Readonly;
         }
         return win32FindData;
      }

      public void SetFileAttributes(uint attr)
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         if (!SetFileAttributesW(FullName, (FileAttributes)attr))
         {
            throw new Win32Exception();
         }
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

      private static int GetRootLength(string path)
      {
         int index = 0;
         int length = path.Length;
         if (length >= 1 && IsDirectorySeparator(path[0]))
         {
            index = 1;
            if (length >= 2 && IsDirectorySeparator(path[1]))
            {
               index = 2;
               int num = 2;
               while (index < length
                      && (IsDirectorySeparator(path[index])
                          || --num > 0)
                     )
               {
                  ++index;
               }
            }
         }
         else if (length >= 2 
            && path[1] == Path.VolumeSeparatorChar
            )
         {
            index = 2;
            if (length >= 3 
               && IsDirectorySeparator(path[2])
               )
               ++index;
         }
         return index;
      }

      public static bool IsDirectorySeparator(char c)
      {
         if (c != Path.DirectorySeparatorChar)
            return c == Path.AltDirectorySeparatorChar;
         return true;
      }

      public static string GetParentPathName(string startPathFileName)
      {
         int rootLength = GetRootLength(startPathFileName);
         int length = startPathFileName.Length;
         if (startPathFileName.Length > rootLength)
         {
            do
            {
               // Spin maddly
            } while (length > rootLength
                     && !IsDirectorySeparator(startPathFileName[--length])
                     );

            return startPathFileName.Substring(0, length);
         }

         return string.Empty;
      }

      public ReadOnlyCollection<AlternateNativeInfo> ListAlternateDataStreams()
      {
         return AlternativeStreamSupport.ListAlternateDataStreams(handle).AsReadOnly();
      }

      [DebuggerHidden] // Stop the "Buffer is too small" from breaking the debugger
      public void GetFileSecurity(uint /*SECURITY_INFORMATION*/ securityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ securityDescriptor, uint length, ref uint lengthNeeded)
      {
         SECURITY_INFORMATION rawRequestedInformation = (SECURITY_INFORMATION)securityInformation;
         // Following in an attempt to solve Win 7 and above share write access
         //if ( PID.CouldBeSMB(info.ProcessId ) )
         //   return ( dokanReturn = Dokan.ERROR_CALL_NOT_IMPLEMENTED);

         SECURITY_INFORMATION reqInfo = rawRequestedInformation;
         AccessControlSections includeSections = AccessControlSections.None;
         if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Owner;
         }
         if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Group;
         }
         if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Access;
         }
         if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Audit;
         }
         FileSystemSecurity pSD = (!IsDirectory)
                                     ? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                                     : Directory.GetAccessControl(FullName, includeSections);
         byte[] managedDescriptor = pSD.GetSecurityDescriptorBinaryForm();
         lengthNeeded = (uint)managedDescriptor.Length;
         if (lengthNeeded <= 32)
         {
            // Deal with FAT32 drives not being able to "Hold" ACL on files.
            // This in turn will prevent explorer from allowing tab to be constructed to attempt to "Set" ACL on FAT32 files.
            throw new Win32Exception(CBFSWinUtil.ERROR_NO_SECURITY_ON_OBJECT);
         }
         // if the buffer is not enough the we must pass the correct error
         // If the returned number of bytes is less than or equal to nLength, the entire security descriptor is returned in the output buffer; otherwise, none of the descriptor is returned.
         if (length < lengthNeeded)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_INSUFFICIENT_BUFFER);
         }
         Marshal.Copy(managedDescriptor, 0, securityDescriptor, managedDescriptor.Length);
      }

      public void SetFileSecurity(uint /*SECURITY_INFORMATION*/ securityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ securityDescriptor, uint length)
      {
         if (ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }
         SECURITY_INFORMATION rawSecurityInformation = (SECURITY_INFORMATION)securityInformation;
         SECURITY_INFORMATION reqInfo = rawSecurityInformation;
         AccessControlSections includeSections = AccessControlSections.None;
         if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Owner;
         }
         if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Group;
         }
         if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Access;
         }
         if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
         {
            includeSections |= AccessControlSections.Audit;
         }
         FileSystemSecurity pSD = (!IsDirectory)
                                     ? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                                     : Directory.GetAccessControl(FullName, includeSections);
         byte[] binaryForm = new byte[length];
         Marshal.Copy(securityDescriptor, binaryForm, 0, binaryForm.Length);
         pSD.SetAccessRuleProtection(
            (reqInfo & SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) ==
            SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
            (reqInfo & SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) ==
            SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
         pSD.SetAuditRuleProtection(
            (reqInfo & SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) ==
            SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION,
            (reqInfo & SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) ==
            SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
         pSD.SetSecurityDescriptorBinaryForm(binaryForm, includeSections);
         // Apply these changes.
         if (IsDirectory)
         {
            Directory.SetAccessControl(FullName, (DirectorySecurity)pSD);
         }
         else
         {
            File.SetAccessControl(FullName, (FileSecurity)pSD);
         }
      }

      #region DLL Imports

      #region SetFileSecurity

      /// <summary>
      /// Check http://msdn.microsoft.com/en-us/library/cc230369%28v=prot.13%29.aspx
      /// and usage http://msdn.microsoft.com/en-us/library/ff556635%28v=vs.85%29.aspx
      /// </summary>
      [Flags]
      // ReSharper disable UnusedMember.Global
      public enum SECURITY_INFORMATION : uint
      {
         /// <summary>
         /// Enums found @ http://msdn.microsoft.com/en-us/library/windows/desktop/aa379579(v=vs.85).aspx
         /// </summary>
         OWNER_SECURITY_INFORMATION = 0x00000001,

         GROUP_SECURITY_INFORMATION = 0x00000002,
         DACL_SECURITY_INFORMATION = 0x00000004,
         SACL_SECURITY_INFORMATION = 0x00000008,
         LABEL_SECURITY_INFORMATION = 0x00000010,
         ATTRIBUTE_SECURITY_INFORMATION = 0x00000020,
         SCOPE_SECURITY_INFORMATION = 0x00000040,
         UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
         UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
         PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
         PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000
         /*
   ATTRIBUTE_SECURITY_INFORMATION      The security property of the object being referenced.

   BACKUP_SECURITY_INFORMATION         The backup properties of the object being referenced.

   DACL_SECURITY_INFORMATION           The DACL of the object is being referenced.

   GROUP_SECURITY_INFORMATION          The primary group identifier of the object is being referenced.

   LABEL_SECURITY_INFORMATION          The mandatory integrity label is being referenced.
                                       The mandatory integrity label is an ACE in the SACL of the object.

   OWNER_SECURITY_INFORMATION          The owner identifier of the object is being referenced.

   PROTECTED_DACL_SECURITY_INFORMATION The DACL cannot inherit access control entries (ACEs).

   PROTECTED_SACL_SECURITY_INFORMATION The SACL cannot inherit ACEs.

   SACL_SECURITY_INFORMATION           The SACL of the object is being referenced.

   SCOPE_SECURITY_INFORMATION          The Central Access Policy (CAP) identifier applicable on the object that is being referenced. Each CAP identifier is stored in a SYSTEM_SCOPED_POLICY_ID_ACE type in the SACL of the SD.

   UNPROTECTED_DACL_SECURITY_INFORMATION  The DACL inherits ACEs from the parent object.

   UNPROTECTED_SACL_SECURITY_INFORMATION  The SACL inherits ACEs from the parent object.
          * */
      }
      // ReSharper restore UnusedMember.Global

      ///// <summary>
      ///// See http://www.pinvoke.net/search.aspx?search=SECURITY_DESCRIPTOR&namespace=[All]
      ///// </summary>
      // ReSharper disable FieldCanBeMadeReadOnly.Global
      // ReSharper disable MemberCanBePrivate.Global
      // ReSharper disable UnusedMember.Global
      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      public struct SECURITY_DESCRIPTOR
      {
         /// <summary>
         /// Structure taken from http://msdn.microsoft.com/en-us/library/ff556610%28v=vs.85%29.aspx
         /// </summary>
         public byte revision;

         public byte size;
         public short control;   // == SECURITY_DESCRIPTOR_CONTROL
         public IntPtr owner;    // == PSID
         public IntPtr group;    // == PSID
         public IntPtr sacl;     // == PACL
         public IntPtr dacl;     // == PACL
      }
      // ReSharper restore UnusedMember.Global
      // ReSharper restore MemberCanBePrivate.Global

      #endregion SetFileSecurity

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

      #endregion Create File

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
      private static extern bool SetFilePointerEx(SafeFileHandle Handle, Int64 i64DistanceToMove, IntPtr ptrNewFilePointer, int origin);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool WriteFile(SafeFileHandle handle, [In] byte[] lpBuffer,
                                          [In] UInt32 numBytesToWrite, out UInt32 lpNumBytesWritten, [In] IntPtr lpOverlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool ReadFile(SafeFileHandle handle, [Out] byte[] lpBuffer,
                                         [In] UInt32 numBytesToRead, out UInt32 lpNumBytesRead_mustBeZero, [In] IntPtr overlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileTime(SafeFileHandle hFile, ref long lpCreationTime, ref long lpLastAccessTime, ref long lpLastWriteTime);

      // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364946%28v=vs.85%29.aspx
      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetFileAttributesEx(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetVolumeNameForVolumeMountPointW(string lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName,
                                                                   uint cchBufferLength);

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileAttributesW(string name, FileAttributes attr);

      // Stolen from
      // ReSharper disable MemberCanBePrivate.Local
      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      private struct WIN32_FILE_ATTRIBUTE_DATA
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
            dwFileAttributes = findData.dwFileAttributes;
            ftCreationTime = findData.ftCreationTime;
            ftLastAccessTime = findData.ftLastAccessTime;
            ftLastWriteTime = findData.ftLastWriteTime;
            nFileSizeHigh = findData.nFileSizeHigh;
            nFileSizeLow = findData.nFileSizeLow;
         }
      }

      // ReSharper restore MemberCanBePrivate.Local

      // ReSharper disable UnusedMember.Local
      private enum GET_FILEEX_INFO_LEVELS
      {
         GetFileExInfoStandard,
         GetFileExMaxInfoLevel
      }
      // ReSharper restore UnusedMember.Local

      [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool PathIsDirectoryEmptyW([MarshalAs(UnmanagedType.LPWStr), In] string pszPath);

      private const int MAX_PATH = 260;

      // ReSharper disable UnusedMember.Local
#pragma warning disable 169
#pragma warning disable 414
      private static readonly int MaxLongPath = 32000;

      private static readonly string Prefix = "\\\\?\\";
#pragma warning restore 414
#pragma warning restore 169
      // ReSharper restore UnusedMember.Local

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool RemoveDirectory(string path);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr), In] string path);

      #endregion DLL Imports

      /// <summary>
      /// Stores the user side process ID
      /// </summary>
      public int ProcessID { get; set; }

      private int openCount;

      /// <summary>
      /// Increment the open counter (for internal usage)
      /// </summary>
      /// <returns></returns>
      public int IncrementOpenCount()
      {
         return Interlocked.Increment(ref openCount);
      }

      /// <summary>
      /// Decrement the counter (for internal usage)
      /// </summary>
      /// <returns>The value after the decrement</returns>
      public int DecrementOpenCount()
      {
         return Interlocked.Decrement(ref openCount);
      }

      [Flags]
      // ReSharper disable UnusedMember.Local
      private enum DuplicateOptions : uint
      {
         DUPLICATE_CLOSE_SOURCE = (0x00000001),// Closes the source handle. This occurs regardless of any error status returned.
         DUPLICATE_SAME_ACCESS = (0x00000002), //Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
      }
      // ReSharper restore UnusedMember.Local

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool DuplicateHandle([In] IntPtr hSourceProcessHandle, [In] SafeFileHandle hSourceHandle,
        [In] IntPtr hTargetProcessHandle, out SafeFileHandle lpTargetHandle,
        [In] uint dwDesiredAccess, [In]  [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, [In] DuplicateOptions dwOptions);

      public static NativeFileOps DuplicateHandle(NativeFileOps sourceHandle)
      {
         IntPtr currentProcess = Process.GetCurrentProcess().Handle;
         SafeFileHandle lpTargetHandle;
         if (!DuplicateHandle(currentProcess, sourceHandle.handle, currentProcess,
                  out lpTargetHandle, 0, false, DuplicateOptions.DUPLICATE_SAME_ACCESS))
         {
            throw new Win32Exception();
         }
         return new NativeFileOps(sourceHandle.FullName, lpTargetHandle, sourceHandle.ForceUseAsReadOnly);
      }

   }
}