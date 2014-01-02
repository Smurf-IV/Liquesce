#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
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
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CallbackFS;
using CBFS;
using LiquesceFacade;
using PID = LiquesceSvc.ProcessIdentity;

namespace LiquesceSvc
{
   internal partial class LiquesceOps : CBFSHandlersAdvanced
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
         ulong freeBytesAvailable;
         ulong totalBytes;
         ulong totalFreeBytes;
         GetDiskFreeSpace(out freeBytesAvailable, out totalBytes, out totalFreeBytes);

         TotalNumberOfSectors = (long)(totalBytes / CbFs.SectorSize);
         NumberOfFreeSectors = (long)(freeBytesAvailable / CbFs.SectorSize);
      }


      public override string VolumeLabel
      {
         get { return mountDetail.VolumeLabel; }
         set { mountDetail.VolumeLabel = value; }
      }

      private static uint volumeSerialNumber = 0x20101112;
      public override uint VolumeId
      {
         get { return volumeSerialNumber; }
         set { volumeSerialNumber = value; }
      }

      private static string[] RestrictedDirectoryNames = { @"$RECYCLE.BIN", @"Recycle Bin", @"RECYCLER", @"Recycled" };

      public override void CreateFile(string filename, uint DesiredAccess, uint fileAttributes, uint ShareMode,
                                      CbFsFileInfo fileInfo,
                                      CbFsHandleInfo userContextInfo)
      {
         int processId = GetProcessId();

         NativeFileOps foundFileInfo = roots.GetPath(filename, mountDetail.HoldOffBufferBytes);
         if (foundFileInfo.ForceUseAsReadOnly)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
         }

         string fullName = foundFileInfo.FullName;

         NativeFileOps.EFileAttributes attributes = (NativeFileOps.EFileAttributes)fileAttributes;

         if (CBFSWinUtil.IsDirectoy(attributes))
         {
            // If a recycler is required then request usage of an existing one from a root drive.
            if (RestrictedDirectoryNames.Contains(foundFileInfo.FileName))
               throw new Win32Exception(CBFSWinUtil.ERROR_ACCESS_DENIED);
            PID.Invoke(processId, foundFileInfo.CreateDirectory);
            CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.OPEN_EXISTING, processId, fullName, userContextInfo);
            return;
         }

         if (!foundFileInfo.Exists)
         {
            Log.Trace("force it to be \"Looked up\" next time");
            roots.RemoveFromLookup(filename);
            PID.Invoke(processId, () => NativeFileOps.CreateDirectory(foundFileInfo.DirectoryPathOnly));
         }
         CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.OPEN_ALWAYS, processId, fullName, userContextInfo);
      }

      private void CallOpenCreateFile(uint DesiredAccess, NativeFileOps.EFileAttributes fileAttributes, uint ShareMode, CbFsFileInfo fileInfo,
                                           uint creation, int processId, string fullName, CbFsHandleInfo userContextInfo)
      {
         long openFileKey = fileInfo.UserContext.ToInt64();
         Log.Debug(
            "CallOpenCreateFile IN fullName [{0}], DesiredAccess[{1}], fileAttributes[{2}], ShareMode[{3}], creation [{4}], ProcessId[{5}], openFileKey [{6}]",
            fullName, (NativeFileOps.EFileAccess)DesiredAccess, fileAttributes, (FileShare)ShareMode, creation, processId, openFileKey);
         int lastError = 0;
         if (CBFSWinUtil.IsDirectoy(fileAttributes))
         {
            fileAttributes |= NativeFileOps.EFileAttributes.BackupSemantics;
         }
         NativeFileOps userFileStream = null;
         try
         {
            PID.Invoke(processId, () =>
            {
               // Turn off NoBuffering request because http://msdn.microsoft.com/en-us/library/windows/desktop/cc644950%28v=vs.85%29.aspx
               userFileStream = NativeFileOps.CreateFile(fullName, DesiredAccess, ShareMode, creation, (uint)(fileAttributes & ~NativeFileOps.EFileAttributes.NoBuffering));
               // If a specified file exists before the function call and dwCreationDisposition is CREATE_ALWAYS 
               // or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function succeeds.
               lastError = Marshal.GetLastWin32Error();
            });
            Log.Trace("It's not gone boom, so it must be okay..");
            userFileStream.Close();
         }
         catch (Win32Exception w32e)
         {
            if (w32e.NativeErrorCode == CBFSWinUtil.ERROR_SHARING_VIOLATION)
            {
               // For some reason when the dllhost (Photo viewer) attempts to open, it throws an "In-Use" error
               // Probably caused by the explorer already opening when performing the double click launch
               if (PID.GetProcessName(processId) != "dllhost")
                  throw;
            }
         }
         using (openFilesSync.UpgradableReadLock())
         {
            NativeFileOps fileStream;
            if (!openFiles.TryGetValue(openFileKey, out fileStream))
            {
               fileStream = OpenUnderTheRadarFileStream(fullName, fileAttributes);
               using (openFilesSync.WriteLock())
               {
                  fileInfo.UserContext = new IntPtr(++openFilesLastKey);
                  openFiles.Add(openFilesLastKey, fileStream);
                  Log.Debug("CallOpenCreateFile fileInfo openFilesLastKey[{0}]", openFilesLastKey);
               }
            }
            int currentOpenCount = fileStream.IncrementOpenCount();
            userFileStream = NativeFileOps.DuplicateHandle(fileStream);
            using (openFilesSync.WriteLock())
            {
               userContextInfo.UserContext = new IntPtr(++openFilesLastKey);
               openFiles.Add(openFilesLastKey, userFileStream);
               Log.Debug("CallOpenCreateFile userContextInfo openFilesLastKey[{0}]", openFilesLastKey);
               userFileStream.ProcessID = processId;
            }

            Log.Debug("CallOpenCreateFile fileInfo IncrementOpenCount[{0}]", currentOpenCount);

            if ((lastError != 0)
               //&& (lastError != CBFSWinUtil.ERROR_ALREADY_EXISTS)
               )
            {
               throw new Win32Exception(lastError);
            }
         }
      }

      private NativeFileOps OpenUnderTheRadarFileStream(string fileName, NativeFileOps.EFileAttributes fileAttributes)
      {
         const NativeFileOps.EFileAccess accessMode = NativeFileOps.EFileAccess.FILE_GENERIC_READ | NativeFileOps.EFileAccess.FILE_GENERIC_WRITE;
         NativeFileOps.EFileAttributes flagsAndAttributes;

         Log.Info("OpenUnderTheRadarFileStream for [{0}]", fileName);
         const uint share = CBFSWinUtil.FILE_SHARE_READ | CBFSWinUtil.FILE_SHARE_WRITE | CBFSWinUtil.FILE_SHARE_DELETE;

         const uint creationDisposition = CBFSWinUtil.OPEN_EXISTING;
         if (CBFSWinUtil.IsDirectoy(fileAttributes))
         {
            Log.Trace("Detected as a Directory");
            flagsAndAttributes = NativeFileOps.EFileAttributes.BackupSemantics ;
         }
         else
         {
            Log.Trace("Detected as a File");
            flagsAndAttributes = NativeFileOps.EFileAttributes.RandomAccess;
         }

         try
         {
            Log.Debug("Open with [{0}]", accessMode);
            return NativeFileOps.CreateFile(fileName, (uint)accessMode, share, creationDisposition, (uint)flagsAndAttributes);
         }
         catch (Win32Exception w32e)
         {
            if ( (w32e.NativeErrorCode == CBFSWinUtil.ERROR_SHARING_VIOLATION)
               || (w32e.NativeErrorCode == CBFSWinUtil.ERROR_ACCESS_DENIED)
               )
            {
               Log.Warn("ERROR_SHARING_VIOLATION: Open with [FILE_GENERIC_READ]");
               return NativeFileOps.CreateFile(fileName, (uint)NativeFileOps.EFileAccess.FILE_GENERIC_READ, share,
                  creationDisposition, (uint) flagsAndAttributes);
            }
            throw;
         }
      }

      public override void OpenFile(string filename, uint DesiredAccess, uint ShareMode, CbFsFileInfo fileInfo,
                                    CbFsHandleInfo userContextInfo)
      {
         NativeFileOps foundFileInfo = roots.GetPath(filename, 0);
         string fullName = foundFileInfo.FullName;

         if (!foundFileInfo.Exists)
         {
            throw new Win32Exception(CBFSWinUtil.ERROR_FILE_NOT_FOUND);
         }
         if (foundFileInfo.ForceUseAsReadOnly )
         {
            const NativeFileOps.EFileAccess writeOptions = NativeFileOps.EFileAccess.FILE_WRITE_DATA |
                                             NativeFileOps.EFileAccess.FILE_APPEND_DATA |
                                             NativeFileOps.EFileAccess.FILE_WRITE_ATTRIBUTES |
                                             NativeFileOps.EFileAccess.FILE_WRITE_EA |
                                             NativeFileOps.EFileAccess.FILE_DELETE_CHILD |
                                             NativeFileOps.EFileAccess.WriteDAC |
                                             NativeFileOps.EFileAccess.WriteOwner |
                                             NativeFileOps.EFileAccess.GenericWrite |
                                             NativeFileOps.EFileAccess.GenericAll;

            if ((((NativeFileOps.EFileAccess)DesiredAccess) & writeOptions) != 0)
            {
               throw new Win32Exception(CBFSWinUtil.ERROR_WRITE_PROTECT);
            }
         }

         NativeFileOps.EFileAttributes attributes = (NativeFileOps.EFileAttributes)fileInfo.Attributes;
         CallOpenCreateFile(DesiredAccess, attributes, ShareMode, fileInfo, CBFSWinUtil.OPEN_EXISTING, GetProcessId(), fullName, userContextInfo);
      }

      public override void CloseFile(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo)
      {
         long openFileKey = fileInfo.UserContext.ToInt64();
         Log.Debug("CloseFile Dummy filename [{0}], fileInfo[{1}], userContextInfo [{2}]",
            fileInfo.FileName, openFileKey, userContextInfo.UserContext.ToInt64());
         CloseFile(fileInfo);
      }
      private void CloseFile(CbFsFileInfo fileInfo)
      {
         long openFileKey = fileInfo.UserContext.ToInt64();
         Log.Debug("CloseFile IN filename [{0}], fileInfo[{1}]",
            fileInfo.FileName, openFileKey);

         if (openFileKey != 0)
         {
            using (openFilesSync.UpgradableReadLock())
            {
               // Decrement the internal open handle
               // https://www.eldos.com/documentation/cbfs/ref_gen_contexts.html
               NativeFileOps fileStream;
               if (openFiles.TryGetValue(openFileKey, out fileStream))
               {
                  int decrementOpenCount = fileStream.DecrementOpenCount();
                  if (decrementOpenCount <= 0)
                  {
                     Log.Debug("Close stream [{0}]", fileStream.FullName);
                     fileStream.Close();
                     using (openFilesSync.WriteLock())
                     {
                        openFiles.Remove(openFileKey);
                     }
                     fileInfo.UserContext = IntPtr.Zero;
                  }
                  else
                  {
                     Log.Debug("Remaining OpenCount [{0}]", decrementOpenCount);
                  }
               }
               else
               {
                  Log.Warn("Something has already removed refFileHandleContext [{0}]", openFileKey);
               }
            }
         }
      }

      public override void CleanupFile(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo)
      {
         long openFileKey = fileInfo.UserContext.ToInt64();
         long userOpenKey = userContextInfo.UserContext.ToInt64();
         Log.Debug("CleanupFile IN filename [{0}], fileInfo[{1}], userContextInfo [{2}]",
            fileInfo.FileName, openFileKey, userOpenKey);

         if (openFileKey != 0)
         {
            using (openFilesSync.UpgradableReadLock())
            {
               NativeFileOps userFileStream;
               if (openFiles.TryGetValue(userOpenKey, out userFileStream))
               {
                  Log.Debug("Close and removing user stream [{0}]", userFileStream.FullName);
                  userFileStream.Close();
                  using (openFilesSync.WriteLock())
                  {
                     openFiles.Remove(userOpenKey);
                  }
               }
               else
               {
                  Log.Warn("Something has already removed userOpenKey [{0}]", userOpenKey);
               }
            }
         }
         userContextInfo.UserContext = IntPtr.Zero;
      }


      public override void GetFileInfo(string FileName, ref bool FileExists, ref DateTime CreationTime,
                                       ref DateTime LastAccessTime,
                                       ref DateTime LastWriteTime, ref long lengthOfFile, ref long AllocationSize,
                                       ref CBFS_LARGE_INTEGER FileId,
                                       ref uint FileAttributes, ref string ShortFileName, ref string RealFileName)
      {
         FileExists = false;
         NativeFileOps nfo = roots.GetPath(FileName);
         if (nfo.Exists)
         {
            WIN32_FIND_DATA fileData = new WIN32_FIND_DATA();
            PID.Invoke(GetProcessId(), () => fileData = nfo.GetFindData());
            if (!string.IsNullOrEmpty(fileData.cFileName))
            {
               ConvertFoundToReturnParams(out CreationTime, out LastAccessTime, out LastWriteTime, out lengthOfFile, out AllocationSize,
                  ref FileId, out FileAttributes, out ShortFileName, out RealFileName, fileData);
               FileExists = true;
            }
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
               div++;
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
         int processId = GetProcessId();
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
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("SetAllocationSize [{0}] AllocationSize[{1}] refFileHandleContext[{2}]", fileInfo.FileName, AllocationSize, refFileHandleContext);

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
         long userOpenKey = userContextInfo.UserContext.ToInt64();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("CanFileBeDeleted [{0}] IN userOpenKey[{1}], refFileHandleContext[{2}]", fileInfo.FileName, userOpenKey, refFileHandleContext);
         NativeFileOps stream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(refFileHandleContext, out stream);
         }
         // TODO: Need to find out if this ever get called before IsDirectoryEmtpy
         // TODO: Need to check if any of the files are open within a directory.
         return( (stream != null)
            && !stream.ForceUseAsReadOnly
            );
      }

      public override void SetFileAttributes(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo,
                                             DateTime creationTime,
                                             DateTime lastAccessTime, DateTime lastWriteTime, uint fileAttributes)
      {
         long userFileKey = userContextInfo.UserContext.ToInt64();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("SetFileAttributes [{0}] IN userFileKey[{1}], refFileHandleContext[{2}], CreationTime [{3}] LastAccessTime[{4}], LastWriteTime[{5}], FileAttributes[{6}]",
            fileInfo.FileName, userFileKey, refFileHandleContext, creationTime, lastAccessTime, lastWriteTime, (NativeFileOps.EFileAttributes)fileAttributes);
         NativeFileOps stream = null;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(userFileKey, out stream);
            if ((stream == null)
                || stream.IsInvalid
               )
            {
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
            }
            else
            {
               if ((fileAttributes != 0) // Requestor is stating no change
                   && (stream.Attributes != fileAttributes)
                  )
               {
                  Log.Trace("changing from stream.Attributes [{0}] to fileAttributes [{1}]", (NativeFileOps.EFileAttributes)stream.Attributes, (NativeFileOps.EFileAttributes)fileAttributes);
                  PID.Invoke(stream.ProcessID, () => stream.SetFileAttributes(fileAttributes));
               }
               if ((creationTime != DateTime.MinValue) // Requestor is stating no change
                   || (lastAccessTime != DateTime.MinValue)
                   || (lastWriteTime != DateTime.MinValue)
                  )
               {
                  Log.Trace("changing Times");
                  PID.Invoke(stream.ProcessID, () => stream.SetFileTime(creationTime, lastAccessTime, lastWriteTime));
               }
            }
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
         PID.Invoke(GetProcessId(), () => XMoveFile.Move(roots, fileInfo.FileName, NewFileName, true, CBFSWinUtil.IsDirectoy(fileInfo.Attributes)));
      }

      public override void ReadFile(CbFsFileInfo fileInfo, long Position, byte[] Buffer, UInt32 BytesToRead, out UInt32 bytesRead)
      {
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("ReadFile [{0}] IN Position=[{1}] BytesToRead=[{2}] refFileHandleContext[{3}]", fileInfo.FileName, Position, BytesToRead, refFileHandleContext);

         using (openFilesSync.ReadLock())
         {
            NativeFileOps fileStream;
            openFiles.TryGetValue(refFileHandleContext, out fileStream);
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
            if (! fileStream.ReadFile(Buffer, BytesToRead, out bytesRead))
            {
               throw new Win32Exception();
            }
         }
         Log.Debug("ReadFile bytesRead [{0}]", bytesRead);
      }

      public override void WriteFile(CbFsFileInfo fileInfo, long Position, byte[] Buffer, UInt32 BytesToWrite, out UInt32 bytesWritten)
      {
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("ReadFile [{0}] IN Position=[{1}] BytesToWrite=[{2}] refFileHandleContext [{3}]", fileInfo.FileName, Position, BytesToWrite, refFileHandleContext);

         using (openFilesSync.ReadLock())
         {
            NativeFileOps fileStream;
            openFiles.TryGetValue(refFileHandleContext, out fileStream);
            if ((fileStream == null)
                || fileStream.IsInvalid
               )
            {
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
            }
            // Use the current offset as a check first to speed up access in large sequential file reads
            fileStream.SetFilePointer(Position, SeekOrigin.Begin);

            if (!fileStream.WriteFile(Buffer, BytesToWrite, out bytesWritten))
            {
               throw new Win32Exception();
            }
            //fileStream.FlushFileBuffers();
         }
         Log.Trace("WriteFile [{0}]", bytesWritten);
      }

      public override bool IsDirectoryEmpty(CbFsFileInfo directoryInfo, string DirectoryName)
      {
         NativeFileOps nfo = roots.GetPath(DirectoryName);
         if (!nfo.Exists)
            CBFSWinUtil.ThrowNotFound((uint)NativeFileOps.EFileAttributes.Directory);
         return nfo.IsEmptyDirectory;
      }

      #endregion

      #region CBFSHandlersAdvanced


      public override void SetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint securityInformation, IntPtr SecurityDescriptor, uint length)
      {
         long userFileKey = userContextInfo.UserContext.ToInt64();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("SetFileSecurityNative[{0}][{1}] with Length[{2}], refFileHandleContext[{3}], userFileKey[{4}]",
            fileInfo.FileName, (NativeFileOps.SECURITY_INFORMATION)securityInformation, length, refFileHandleContext, userFileKey);

         NativeFileOps stream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(userFileKey, out stream);
            if (stream != null)
            {
               PID.Invoke(stream.ProcessID, () => stream.SetFileSecurity(securityInformation, SecurityDescriptor, length));
            }
         }
         if (stream == null)
         {
            CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }
      }

      [DebuggerHidden] // Stop firing for the "Too small buffer" error
      public override void GetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint RequestedInformation, IntPtr SecurityDescriptor, uint Length, out uint lengthNeeded)
      {
         long userFileKey = userContextInfo.UserContext.ToInt64();
         long refFileHandleContext = fileInfo.UserContext.ToInt64();
         Log.Debug("GetFileSecurity[{0}][{1}] with Length[{2}], refFileHandleContext[{3}], userFileKey[{4}]",
            fileInfo.FileName, (NativeFileOps.SECURITY_INFORMATION)RequestedInformation, Length, refFileHandleContext, userFileKey);
         lengthNeeded = 0;

         NativeFileOps stream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(userFileKey, out stream);
            if (stream != null)
            {
               using (PID.InvokeHelper(stream.ProcessID))
               {
                  stream.GetFileSecurity(RequestedInformation, SecurityDescriptor, Length, ref lengthNeeded);
               }
            }
         }
         if (stream == null)
         {
            CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
         }
      }


      #region Stream Enumeration

      private readonly Dictionary<long, ReadOnlyCollection<AlternateNativeInfo>> EnumeratedStream = new Dictionary<long, ReadOnlyCollection<AlternateNativeInfo>>();

      public override void EnumerateNamedStreams(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo,
                                                 CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo,
                                                 ref string streamName, ref long streamSize, ref long streamAllocationSize,
                                                 out bool aNamedStreamFound)
      {
         long userOpenKey = userContextInfo.UserContext.ToInt64();
         Log.Debug("EnumerateNamedStreams [{0}] userOpenKey[{1}]", fileInfo.FileName, userOpenKey);

         ReadOnlyCollection<AlternateNativeInfo> alternates = null;
         int nextOffset = namedStreamsEnumerationInfo.UserContext.ToInt32();
         NativeFileOps fileStream;
         using (openFilesSync.ReadLock())
         {
            openFiles.TryGetValue(userOpenKey, out fileStream);
            if ((fileStream == null)
                || fileStream.IsInvalid
               )
            {
               CBFSWinUtil.ThrowNotFound(fileInfo.Attributes);
            }

            if (nextOffset > 0)
            {
               EnumeratedStream.TryGetValue(userOpenKey, out alternates);
            }
            else
            {
               // Nothing = find
               PID.Invoke(fileStream.ProcessID, () => alternates = fileStream.ListAlternateDataStreams());
               EnumeratedStream[userOpenKey] = alternates;
            }
         }

         if ((alternates == null)
            || !alternates.Any()
            || (nextOffset >= alternates.Count)
            )
         {
            aNamedStreamFound = false;
         }
         else
         {
            AlternateNativeInfo info = alternates[nextOffset++];
            streamName = info.StreamName;
            streamSize = info.StreamSize;
            {
               // The allocation size is in most cases a multiple of the allocation unit (cluster) size. 
               long remainder;
               long div = Math.DivRem(streamSize, CbFs.SectorSize, out remainder);
               if (remainder > 0)
                  div++;
               streamAllocationSize = div * CbFs.SectorSize;
            }
            namedStreamsEnumerationInfo.UserContext = new IntPtr(nextOffset);
            aNamedStreamFound = true;
         }
      }


      public override void CloseNamedStreamsEnumeration(CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo)
      {
         long refFileHandleContext = namedStreamsEnumerationInfo.UserContext.ToInt64();
         EnumeratedStream.Remove(refFileHandleContext);
      }

      #endregion

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
            long refFileHandleContext = fileInfo.UserContext.ToInt64();
            Log.Debug("FlushFileBuffers IN [{0}], refFileHandleContext[{1}]", fileInfo.FileName, refFileHandleContext);
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
               if (!fileStream.IsDirectory)
                  fileStream.FlushFileBuffers();
            }
         }
      }

      #endregion

   }

}
