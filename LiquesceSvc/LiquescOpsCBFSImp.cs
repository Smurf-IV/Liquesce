#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Simon Coghlan (Aka Smurf-IV)
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using CallbackFS;
using CBFS;
using LiquesceFacade;

using PID = LiquesceSvc.ProcessIdentity;

namespace LiquesceSvc
{
   internal partial class LiquesceOps : CBFSHandlersAdvancedSecurity
   {

      #region CBFS Implementation

      public override void Mount()
      {
      }

      public override void UnMount()
      {
         using (openFilesSync.WriteLock())
         {
            foreach (NativeFileOps obj2 in openFiles.Values)
            {
               try
               {
                  if (obj2 != null)
                  {
                     obj2.Close();
                  }
               }
               catch (Exception ex)
               {
                  Log.InfoException("Unmount closing files threw: ", ex);
               }
            }
            openFiles.Clear();
         }
      }

      public override void GetVolumeSize(out long TotalNumberOfSectors, out long NumberOfFreeSectors)
      {
         ulong freeBytesAvailable = 0;
         ulong totalBytes = 0;
         ulong totalFreeBytes = 0;
         GetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes);

         TotalNumberOfSectors = (long)(totalBytes / CbFs.SectorSize);
         NumberOfFreeSectors = (long)(totalFreeBytes / CbFs.SectorSize);
      }

      private string volumeLabel = "Liquesce";

      public override string VolumeLabel
      {
         get { return volumeLabel; }
         set { volumeLabel = value; }
      }

      private static uint volumeSerialNumber = 0x20101112;
      public override uint VolumeId
      {
         get { return volumeSerialNumber; }
         set { volumeSerialNumber = value; }
      }

      public override void CreateFile(string filename, uint DesiredAccess, uint fileAttributes, uint ShareMode,
                                      CbFsFileInfo fileInfo,
                                      CbFsHandleInfo userContextInfo)
      {
         int processId = GetProcessId();
         userContextInfo.UserContext = new IntPtr(processId);

         NativeFileOps foundFileInfo = roots.GetPath(filename, configDetails.HoldOffBufferBytes);
         string fullName = foundFileInfo.FullName;

         NativeFileOps.EFileAttributes attributes = (NativeFileOps.EFileAttributes)fileAttributes;

         if (CBFSWinUtil.IsDirectoy(attributes) )
         {
            PID.Invoke(processId, foundFileInfo.CreateDirectory);
            CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.OPEN_EXISTING, processId, fullName);
            return;
         }
         
         if (!foundFileInfo.Exists)
         {
            Log.Trace("force it to be \"Looked up\" next time");
            roots.RemoveFromLookup(filename);
            PID.Invoke(processId, () => NativeFileOps.CreateDirectory(foundFileInfo.DirectoryPathOnly));
         }
         CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.CREATE_ALWAYS, processId, fullName);
      }

      private void CallOpenCreateFile(uint DesiredAccess, NativeFileOps.EFileAttributes fileAttributes, uint ShareMode, CbFsFileInfo fileInfo,
                                           uint creation, int processId, string fullName)
      {
         Log.Debug(
            "CallOpenCreateFile IN fullName [{0}], DesiredAccess[{1}], fileAttributes[{2}], ShareMode[{3}], ProcessId[{4}]",
            fullName, (NativeFileOps.EFileAccess)DesiredAccess, fileAttributes, (FileShare)ShareMode, processId );
         if (CBFSWinUtil.IsDirectoy(fileAttributes))
         {
            fileAttributes |= NativeFileOps.EFileAttributes.BackupSemantics;
         }
         NativeFileOps fs = null;
         PID.Invoke(processId, () => fs = NativeFileOps.CreateFile(fullName, DesiredAccess, ShareMode, creation, (uint)fileAttributes));
         // If a specified file exists before the function call and dwCreationDisposition is CREATE_ALWAYS 
         // or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function succeeds.
         int lastError = Marshal.GetLastWin32Error();
         if (lastError != 0)
         {
            throw new Win32Exception(lastError);
         }
         Log.Trace("It's not gone boom, so it must be okay..");
         using (openFilesSync.WriteLock())
         {
            fileInfo.UserContext = new IntPtr(++openFilesLastKey);
            openFiles.Add(openFilesLastKey, fs);
            Log.Debug("CallOpenCreateFile openFilesLastKey[{0}]", openFilesLastKey);
         }
      }

      public override void OpenFile(string filename, uint DesiredAccess, uint ShareMode, CbFsFileInfo fileInfo,
                                    CbFsHandleInfo userContextInfo)
      {
         int processId = GetProcessId();
         userContextInfo.UserContext = new IntPtr(processId);
         NativeFileOps foundFileInfo = roots.GetPath(filename, 0);
         string fullName = foundFileInfo.FullName;
         
         if (!foundFileInfo.Exists)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_FILE_NOT_FOUND);
         }
         NativeFileOps.EFileAttributes attributes = (NativeFileOps.EFileAttributes)fileInfo.Attributes;
         CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.OPEN_EXISTING, processId, fullName);
      }

      public override void CloseFile(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo)
      {
         long openFileKey = fileInfo.UserContext.ToInt64();
         Log.Debug("CloseFile IN filename [{0}], fileInfo[{1}], userContextInfo [{2}]",
            fileInfo.FileName, openFileKey, userContextInfo.UserContext.ToInt32());

         if (openFileKey != 0)
         {
            using (openFilesSync.UpgradableReadLock())
            {
               // The File can be closed by the remote client via Delete (as it does not know to close first!)
               NativeFileOps fileStream;
               if (openFiles.TryGetValue(openFileKey, out fileStream))
               {
                  Log.Debug("Close and then Remove stream [{0}]", fileStream.FullName);
                  fileStream.Close();
                  using (openFilesSync.WriteLock())
                  {
                     openFiles.Remove(openFileKey);
                  }
               }
               else
               {
                  Log.Warn("Something has already closed info.refFileHandleContext [{0}]", openFileKey);
               }
            }
         }
         fileInfo.UserContext = IntPtr.Zero;
      }

      public override void GetFileInfo(string FileName, ref bool FileExists, ref DateTime CreationTime,
                                       ref DateTime LastAccessTime,
                                       ref DateTime LastWriteTime, ref long lengthOfFile, ref long AllocationSize,
                                       ref CBFS_LARGE_INTEGER FileId,
                                       ref uint FileAttributes, ref string ShortFileName, ref string RealFileName)
      {
         NativeFileOps nfo = roots.GetPath(FileName);
         if (!nfo.Exists)
         {
            FileExists = false;
            return;
         }
         FileExists = true;
         WIN32_FIND_DATA[] files;
         FindFiles(FileName, PID.systemProcessId, out files);
         if (files.Any())
         {
            ConvertFoundToReturnParams(out CreationTime, out LastAccessTime, out LastWriteTime, out lengthOfFile, out AllocationSize, ref FileId, out FileAttributes, out ShortFileName, out RealFileName, files[0]);
         }
      }

      private void ConvertFoundToReturnParams(out DateTime CreationTime, out DateTime LastAccessTime, out DateTime LastWriteTime, out long lengthOfFile, out long allocationSize, ref CBFS_LARGE_INTEGER FileId, out uint attributes, out string ShortFileName, out string RealFileName, WIN32_FIND_DATA file)
      {
         attributes = file.dwFileAttributes;
         CreationTime = NativeFileOps.ConvertFileTimeToDateTime(file.ftCreationTime);
         LastAccessTime = NativeFileOps.ConvertFileTimeToDateTime(file.ftLastAccessTime);
         LastWriteTime = NativeFileOps.ConvertFileTimeToDateTime(file.ftLastWriteTime);
         // public uint dwVolumeSerialNumber;
         lengthOfFile = (long)((ulong)file.nFileSizeHigh << 32);
         lengthOfFile += file.nFileSizeLow;
         {
            // The allocation size is in most cases a multiple of the allocation unit (cluster) size. 
            long remainder;
            long div = Math.DivRem(lengthOfFile, CbFs.SectorSize, out remainder);
            if (remainder > 0)
               div ++;
            allocationSize = div * CbFs.SectorSize;
         }
         // public uint dwNumberOfLinks;
         //FileId.HighPart = (int)file.nFileIndexHigh;
         //FileId.LowPart = file.nFileIndexLow;
         FileId.QuadPart = 0;
         ShortFileName = string.IsNullOrWhiteSpace(file.cAlternateFileName) ? file.cFileName : file.cAlternateFileName;
         RealFileName = file.cFileName;
      }

      private readonly Dictionary<long, WIN32_FIND_DATA[]> EnumeratedDirectories = new Dictionary<long, WIN32_FIND_DATA[]>();
 
      public override void EnumerateDirectory(CbFsFileInfo directoryInfo, CbFsHandleInfo userContextInfo,
                                              CbFsDirectoryEnumerationInfo DirectoryEnumerationInfo, string Mask, bool Restart,
                                              ref bool FileFound, ref string FileName, ref string ShortFileName,
                                              ref DateTime CreationTime, ref DateTime LastAccessTime, ref DateTime LastWriteTime,
                                              ref long lengthOfFile, ref long AllocationSize, ref CBFS_LARGE_INTEGER FileId,
                                              ref uint attributes)
      {
         int processId = userContextInfo.UserContext.ToInt32();
         Log.Debug("EnumerateDirectory [{0}] processId[{1}]", directoryInfo.FileName, processId);
         long refFileHandleContext = directoryInfo.UserContext.ToInt64();
         Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);
         int nextOffset = DirectoryEnumerationInfo.UserContext.ToInt32();
         if (Restart)
         {
            CloseDirectoryEnumeration(directoryInfo, DirectoryEnumerationInfo);
            nextOffset = 0;
         }
         WIN32_FIND_DATA[] files;
         if (nextOffset > 0)
         {
            EnumeratedDirectories.TryGetValue(refFileHandleContext, out files);
         }
         else
         {
            // Nothing or restart = find
            FindFiles(directoryInfo.FileName, processId, out files, Mask);
            EnumeratedDirectories[refFileHandleContext] = files;
         }
         if ((files == null)
             || !files.Any()
             || (nextOffset >= files.Length)
            )
         {
            FileFound = false;
         }
         else
         {
            ConvertFoundToReturnParams(out CreationTime, out LastAccessTime, out LastWriteTime, out lengthOfFile, out AllocationSize, ref FileId, out attributes, out ShortFileName, out FileName, files[nextOffset++]);
            DirectoryEnumerationInfo.UserContext = new IntPtr(nextOffset);
            FileFound = true;
         }

      }

      public override void CloseDirectoryEnumeration(CbFsFileInfo directoryInfo,
                                                     CbFsDirectoryEnumerationInfo directoryEnumerationInfo)
      {
         long refFileHandleContext = directoryInfo.UserContext.ToInt64();
         EnumeratedDirectories.Remove(refFileHandleContext);
      }

      /*
      This property specifies the number of bytes that the file consumes on disk. Its value may be less than the Size if the file is 'sparse'.
       * The following paragraph is taken from SSH File Transfer Protocol draft-ietf-secsh-filexfer-10, part 7.4:

       * When present during file creation, the file SHOULD be created and the specified number of bytes preallocated. 
       * If the preallocation fails, the file should be removed (if it was created) and an error returned.
       * If this field is present during a setstat operation, the file SHOULD be extended or truncated to the specified size. 
       * The 'size' of the file may be affected by this operation. If the operation succeeds, the 'size' should be the minimum 
       * of the 'size' before the operation and the new 'allocation-size'.
       * Querying the 'allocation-size' after setting it MUST return a value that is greater-than or equal to the value set, 
       * but it MAY not return the precise value set.
       * If both 'size' and 'allocation-size' are set during a setstat operation, and 'allocation-size' is less than 'size', 
       * the server MUST return SSH_FX_INVALID_PARAMETER.
      */
      public override void SetAllocationSize(CbFsFileInfo fileInfo, long AllocationSize)
      {
         Log.Debug("SetAllocationSize [{0}] AllocationSize[{1}]", fileInfo.FileName, AllocationSize);
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);

         using (openFilesSync.ReadLock())
         {
            NativeFileOps stream;
            if (openFiles.TryGetValue(refFileHandleContext, out stream))
            {
               BY_HANDLE_FILE_INFORMATION lpFileInformation = new BY_HANDLE_FILE_INFORMATION();
               stream.GetFileInformationByHandle(ref lpFileInformation);
               long thisFileSize = (lpFileInformation.nFileSizeHigh << 32) + lpFileInformation.nFileSizeLow;
               if (thisFileSize < AllocationSize)
               {
                  // Need to check that the source FullName drive has enough free space for this "Potential" allocation
                  NativeFileOps confirmSpace = roots.GetPath(fileInfo.FileName, (ulong)(AllocationSize - thisFileSize));
                  if (confirmSpace.FullName != stream.FullName)
                  {
                     Log.Warn(
                        "There is a problem, in the amount of space required to fullfill this request with respect to the amount of space originally allocated");
                     // TODO: Move the file, or return not enough space ??
                     throw new ECBFSError(CBFSWinUtil.ERROR_NO_SYSTEM_RESOURCES);

                  }
               }
               stream.SetLength(AllocationSize);
            }
            else
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }
      }

      public override void SetEndOfFile(CbFsFileInfo fileInfo, long EndOfFile)
      {
         Log.Debug("SetEndOfFile [{0}] EndOfFile[{1}]", fileInfo.FileName, EndOfFile);
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);

         using (openFilesSync.ReadLock())
         {
            NativeFileOps stream;
            if (openFiles.TryGetValue(refFileHandleContext, out stream))
            {
               stream.SetLength(EndOfFile);
            }
            else
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }
      }

      /// You should not delete file on DeleteFile or DeleteDirectory.
      // When DeleteFile or DeleteDirectory, you must check whether
      // you can delete or not, and return 0 (when you can delete it)
      // or appropriate error codes such as -ERROR_DIR_NOT_EMPTY,
      // -ERROR_SHARING_VIOLATION.
      public override bool CanFileBeDeleted(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo)
      {
         int processId = userContextInfo.UserContext.ToInt32();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("CanFileBeDeleted [{0}] IN ProcessId[{1}], refFileHandleContext[{2}]", fileInfo.FileName, processId, refFileHandleContext);
         NativeFileOps stream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(refFileHandleContext, out stream);
         }
         // TODO: Need to find out if this ever get called before IsDirectoryEmtpy
         // TODO: Need to check if any of the files are open within a directory.
         return (stream != null);
      }

      public override void SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo,
                                             DateTime creationTime,
                                             DateTime lastAccessTime, DateTime lastWriteTime, uint fileAttributes)
      {
         int processId = userContextInfo.UserContext.ToInt32();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("SetFileAttributes [{0}] IN ProcessId[{1}], refFileHandleContext[{2}], CreationTime [{3}] LastAccessTime[{4}], LastWriteTime[{5}], FileAttributes[{6}]",
            fileInfo.FileName, processId, refFileHandleContext, creationTime, lastAccessTime, lastWriteTime, (NativeFileOps.EFileAttributes)fileAttributes);
         NativeFileOps stream = null;
         using (openFilesSync.ReadLock())
         {
            Log.Trace("info.refFileHandleContext [{0}]", refFileHandleContext);
            openFiles.TryGetValue(refFileHandleContext, out stream);
         }
         if ((stream == null)
            || stream.IsInvalid
            )
         {
            CBFSWinUtil.ThrowNotFound(fileAttributes);
         }
         else
         {
            PID.Invoke(processId, () => stream.SetFileAttributes(fileAttributes) );
            PID.Invoke(processId, () => stream.SetFileTime(creationTime, lastAccessTime, lastWriteTime) );
         }
      }

      public override void DeleteFile(CbFsFileInfo fileInfo)
      {
         NativeFileOps nfo = roots.GetPath(fileInfo.FileName);
         if (nfo.IsDirectory)
            PID.Invoke(GetProcessId(), nfo.DeleteDirectory);
         else
         {
            PID.Invoke(GetProcessId(), nfo.DeleteFile);
         }
         roots.RemoveFromLookup(fileInfo.FileName);
      }

      public override void RenameOrMoveFile(CbFsFileInfo fileInfo, string NewFileName)
      {
         Log.Debug("RenameOrMoveFile [{0}] to [{1}]", fileInfo.FileName, NewFileName);
         if (fileInfo.FileName == NewFileName) // This is some weirdness that SyncToy tries to pull !!
         {
            return;
         }
         // Cbfs has handled the closing and replaceIfExists checks, so it needs to be set always here.
         // https://www.eldos.com/forum/read.php?FID=13&TID=2015
         PID.Invoke(GetProcessId(), () => XMoveFile.Move(roots, fileInfo.FileName, NewFileName, true, CBFSWinUtil.IsDirectoy(fileInfo.Attributes) ) );
      }

      public override int ReadFile(CbFsFileInfo fileInfo, long Position, byte[] Buffer, int BytesToRead)
      {
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("ReadFile [{0}] IN Position=[{1}] BytesToRead=[{2}]", fileInfo.FileName, Position, BytesToRead);
         Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);

         NativeFileOps fileStream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(refFileHandleContext, out fileStream);
         }
         if ((fileStream == null)
            || fileStream.IsInvalid
            )
         {
            CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }

         // Some programs the file offset to extend the file length to write past the end of the file
         // Commented the check of rawOffset being off the size of the file.
         //if (Position >= fileStream.Length) 
         //   throw(ECBFSError(ERROR_HANDLE_EOF));
         //else
         //{
         // Use the current offset as a check first to speed up access in large sequential file reads

         fileStream.SetFilePointer(Position, SeekOrigin.Begin);
         int bytesRead;
         if (0 == fileStream.ReadFile(Buffer, BytesToRead, out bytesRead))
         {
            throw new Win32Exception();
         }
         return bytesRead;
      }

      public override int WriteFile(CbFsFileInfo fileInfo, long Position, byte[] Buffer, int BytesToWrite)
      {
         int BytesWritten = 0;
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("ReadFile [{0}] IN Position=[{1}] BytesToWrite=[{2}]", fileInfo.FileName, Position, BytesToWrite);
         Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);

         NativeFileOps fileStream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(refFileHandleContext, out fileStream);
         }
         if ((fileStream == null)
            || fileStream.IsInvalid
            )
         {
            CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }
         // Use the current offset as a check first to speed up access in large sequential file reads
         fileStream.SetFilePointer(Position, SeekOrigin.Begin);

         if (0 == fileStream.WriteFile(Buffer, BytesToWrite, out BytesWritten))
         {
            throw new Win32Exception();
         }
         return BytesWritten;
      }

      public override bool IsDirectoryEmpty(CbFsFileInfo directoryInfo, string DirectoryName)
      {
         NativeFileOps nfo = roots.GetPath(DirectoryName);
         if (!nfo.Exists)
            CBFSWinUtil.ThrowNotFound((uint) NativeFileOps.EFileAttributes.Directory);
         return nfo.IsEmptyDirectory;
      }

      #endregion

      #region CBFSHandlersAdvanced


      public override void SetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, SECURITY_INFORMATION securityInformation, IntPtr SecurityDescriptor, uint Length)
      {
         int processId = userContextInfo.UserContext.ToInt32();

         Log.Debug("SetFileSecurityNative IN [{0}] SetFileSecurity[{1}][{2}]", fileInfo.FileName, processId, securityInformation);
         NativeFileOps foundFileInfo = roots.GetPath(fileInfo.FileName);
         if (foundFileInfo.Exists)
         {
            PID.Invoke(processId, delegate
            {
               AccessControlSections includeSections = AccessControlSections.None;
               if ((securityInformation & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) ==
                   SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
                  includeSections |= AccessControlSections.Owner;
               if ((securityInformation & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) ==
                   SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
                  includeSections |= AccessControlSections.Group;
               if ((securityInformation & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) ==
                   SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
                  includeSections |= AccessControlSections.Access;
               if ((securityInformation & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) ==
                   SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
                  includeSections |= AccessControlSections.Audit;
               string FullName = foundFileInfo.FullName;
               FileSystemSecurity pSD = (!foundFileInfo.IsDirectory)
                                           ? (FileSystemSecurity) File.GetAccessControl(FullName, includeSections)
                                           : Directory.GetAccessControl(FullName, includeSections);
               byte[] binaryForm = new byte[Length];
               Marshal.Copy(SecurityDescriptor, binaryForm, 0, binaryForm.Length);
               pSD.SetAccessRuleProtection(
                  (securityInformation & SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) ==
                  SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
                  (securityInformation & SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) ==
                  SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
               pSD.SetAuditRuleProtection(
                  (securityInformation & SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) ==
                  SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION,
                  (securityInformation & SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) ==
                  SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
               pSD.SetSecurityDescriptorBinaryForm(binaryForm, includeSections);
               // Apply these changes.
               if (foundFileInfo.IsDirectory)
                  Directory.SetAccessControl(FullName, (DirectorySecurity) pSD);
               else
               {
                  File.SetAccessControl(FullName, (FileSecurity) pSD);
               }
            });
         }
         else
            CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);

      }

      public override uint GetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo,
         SECURITY_INFORMATION RequestedInformation, IntPtr SecurityDescriptor, uint Length)
      {
         Log.Debug("GetFileSecurity[{0}][{1}] with Length[{2}]", fileInfo.FileName, RequestedInformation, Length);

         SECURITY_INFORMATION reqInfo = RequestedInformation;
         byte[] managedDescriptor = null;
         AccessControlSections includeSections = AccessControlSections.None;
         if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) ==
             SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
            includeSections |= AccessControlSections.Owner;
         if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) ==
             SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
            includeSections |= AccessControlSections.Group;
         if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) ==
             SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
            includeSections |= AccessControlSections.Access;
         if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) ==
             SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
            includeSections |= AccessControlSections.Audit;
         FileSystemSecurity pSD = null;
         NativeFileOps stream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(fileInfo.UserContext.ToInt64(), out stream);
         }
         string FullName;
         if (stream != null)
         {
            FullName = stream.FullName;
            pSD = (!stream.IsDirectory)
                     ? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                     : Directory.GetAccessControl(FullName, includeSections);
         }
         else
         {
            NativeFileOps foundFileInfo = roots.GetPath(fileInfo.FileName);
            FullName = foundFileInfo.FullName;
            if (foundFileInfo.Exists)
            {
               PID.Invoke(userContextInfo.UserContext.ToInt32(), () => pSD = (!foundFileInfo.IsDirectory)
                                                         ? (FileSystemSecurity)
                                                           File.GetAccessControl(FullName, includeSections)
                                                         : Directory.GetAccessControl(FullName, includeSections)
                  );
            }
            else
            {
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
            }
         }
         if (pSD != null)
         {
            if (Log.IsTraceEnabled)
            {
               string getFileSecurityNative = pSD.GetSecurityDescriptorSddlForm(includeSections);
               Log.Trace("GetFileSecurityNative on {0} Retrieved [{1}]", FullName, getFileSecurityNative);
            }
            managedDescriptor = pSD.GetSecurityDescriptorBinaryForm();
         }
         if (managedDescriptor != null)
         {
            // If the returned number of bytes is less than or equal to nLength, 
            // the entire security descriptor is returned in the output buffer; otherwise, none of the descriptor is returned.
            if (Length >= managedDescriptor.Length)
            {
               Marshal.Copy(managedDescriptor, 0, SecurityDescriptor, managedDescriptor.Length);
            }
            return (uint)managedDescriptor.Length;
         }
         return 0;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="fileInfo">
      /// Contains information about the file. Can be null.
      /// If FileInfo is empty, your code should attempt to flush everything, related to the disk. 
      /// </param>
      public override void FlushFile(CbFsFileInfo fileInfo)
      {
         if (fileInfo != null)
         {
            Log.Debug("FlushFileBuffers IN [{0}]", fileInfo.FileName);
            long refFileHandleContext = fileInfo.UserContext.ToInt64();
            Log.Trace("refFileHandleContext [{0}]", refFileHandleContext);
            using (openFilesSync.ReadLock())
            {
               NativeFileOps fileStream;
               if (openFiles.TryGetValue(refFileHandleContext, out fileStream))
               {
                  if (!fileStream.IsDirectory)
                     fileStream.FlushFileBuffers();
               }
               else
               {
                  CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
               }
            }
         }
         else
         {
            Log.Debug("FlushFileBuffers IN with null");
            foreach (NativeFileOps fileStream in openFiles.Values)
            {
               if ( ! fileStream.IsDirectory )
                  fileStream.FlushFileBuffers();
            }
         }
      }

      #endregion

   }
}
