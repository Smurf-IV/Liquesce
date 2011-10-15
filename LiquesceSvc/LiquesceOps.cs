#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;

using DokanNet;
using LiquesceFacade;
using Microsoft.Win32.SafeHandles;
using NLog;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      // currently open files...
      // last key
      static private UInt64 openFilesLastKey;
      // lock
      static private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      // dictionary of all open files
      static private readonly Dictionary<UInt64, FileStreamName> openFiles = new Dictionary<UInt64, FileStreamName>();

      private readonly Roots roots;
      private readonly ConfigDetails configDetails;

      public LiquesceOps(ConfigDetails configDetails)
      {
         this.configDetails = configDetails;
         roots = new Roots(configDetails); // Already been trimmed in ReadConfigDetails()
      }

      #region IDokanOperations Implementation

      /// <summary>
      /// The information given in the Dokan info is a bit misleading about the return codes
      /// This is what the Win OS suystem is expecting http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx
      /// So.. Everything succeeds but the Return code is ERROR_ALREADY_EXISTS
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawFlagsAndAttributes"></param>
      /// <param name="info"></param>
      /// <param name="rawAccessMode"></param>
      /// <param name="rawShare"></param>
      /// <param name="rawCreationDisposition"></param>
      /// <returns></returns>
      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info)
      {
         int actualErrorCode = Dokan.DOKAN_SUCCESS;
         try
         {
            // TODO: Dump these out in hex to max it easier to decode :-)
            Log.Debug(
              "CreateFile IN filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}], ProcessId[{5}]",
              filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info.ProcessId);
            bool createNew = (rawCreationDisposition == Proxy.CREATE_NEW) || (rawCreationDisposition == Proxy.CREATE_ALWAYS);
            FileSystemInfo foundFileInfo = roots.GetPath(filename, createNew);

            bool fileExists = foundFileInfo.Exists;
            if (fileExists
               && (foundFileInfo is DirectoryInfo)
               )
            {
               actualErrorCode = OpenDirectory(filename, info);
               return actualErrorCode;
            }

            // Increment now in case there is an exception later
            ++openFilesLastKey; // never be Zero !
            // Stop using exceptions to throw ERROR_FILE_NOT_FOUND

            switch (rawCreationDisposition)
            {
               // *** NTh Change ***
               // Fix the parameter invalid when trying to replace an existing file
               case Proxy.CREATE_ALWAYS:
                  //Ignore the existing of the file, at this time the OS is trying to overwrite it                
                  break;
               case Proxy.CREATE_NEW:
                  if (fileExists)
                  {
                     Log.Debug("filename [{0}] CREATE_NEW File Exists !", filename);

                     // force it to be "Looked up" next time
                     roots.RemoveFromLookup(filename);
                     actualErrorCode = Dokan.ERROR_FILE_EXISTS;
                     return actualErrorCode;
                  }
                  break;
               case Proxy.OPEN_EXISTING:
               //case FileMode.Append:                    
               case Proxy.TRUNCATE_EXISTING:
                  if (!fileExists)
                  {
                     Log.Debug("filename [{0}] ERROR_FILE_NOT_FOUND", filename);
                     // Probably someone has removed this on the actual drive
                     roots.RemoveFromLookup(filename);
                     actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
                     return actualErrorCode;
                  }
                  break;
            }

            bool writeable = (((rawAccessMode & Proxy.FILE_WRITE_DATA) == Proxy.FILE_WRITE_DATA));
            // *** NTh Change ***
            // The Condition that eliminates the variable fileExists
            string fullName = foundFileInfo.FullName;
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (!fileExists
                   && (writeable
                       || createNew)
                  )
               {
                  Log.Trace("We want to create a new file in: [{0}]", fullName);

                  // Need to solve the issue with Synctoy performing moves into unknown / unused space
                  // MoveFile pathSource: [F:\_backup\Kylie Minogue\FSP01FA0CF932F74BF5AE5C217F4AE6626B.tmp] pathTarget: [G:\_backup\Kylie Minogue\(2010) Aphrodite\12 - Can't Beat The Feeling.mp3] 
                  // MoveFile threw:  System.IO.DirectoryNotFoundException: The system cannot find the path specified. (Exception from HRESULT: 0x80070003)
                  DirectoryInfo newDir = ((FileInfo)foundFileInfo).Directory;
                  Log.Trace("Check if directory exists: [{0}]", newDir.FullName);
                  if (!newDir.Exists)
                  {
                     Log.Trace("Have to create this directory.");
                     Directory.CreateDirectory(newDir.FullName);
                  }
               }

               // TODO: The DokanFileInfo structure has the following extra things that need to be mapped tothe file open
               //public bool PagingIo;
               //public bool SynchronousIo;
               //public bool Nocache;
               // See http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx#caching_behavior
               if (info.PagingIo)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_RANDOM_ACCESS;
               if (info.Nocache)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_WRITE_THROUGH; // | Proxy.FILE_FLAG_NO_BUFFERING;
               // FILE_FLAG_NO_BUFFERING flag requires that all I/O operations on the file handle be in multiples of the sector size, 
               // AND that the I/O buffers also be aligned on addresses which are multiples of the sector size

               if (info.SynchronousIo)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_SEQUENTIAL_SCAN;
               SafeFileHandle handle = CreateFileW(fullName, rawAccessMode, rawShare, IntPtr.Zero,
                                                   rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);

               if (handle.IsInvalid)
               {
                  Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
               }
               FileStreamName fs = new FileStreamName(fullName, handle,
                                                      writeable ? FileAccess.ReadWrite : FileAccess.Read,
                                                      (int)configDetails.BufferReadSize);
               // It's not gone boom, so it must be okay..
               // Remove the cached entry as it will have new access time etc.
               roots.RemoveTargetFromLookup(fullName);
               using (openFilesSync.WriteLock())
               {
                  info.refFileHandleContext = openFilesLastKey; // never be Zero !
                  openFiles.Add(openFilesLastKey, fs);
               }
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateFile threw: ", ex);
            actualErrorCode = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("CreateFile OUT actualErrorCode=[{0}] context[{1}]", actualErrorCode, openFilesLastKey);
         }
         return actualErrorCode;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("OpenDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            FileSystemInfo foundDirInfo = roots.GetPath(filename);
            if (foundDirInfo.Exists
               && (foundDirInfo is DirectoryInfo))
            {
               ProcessIdentity.Invoke(info.ProcessId, delegate
               {
                  info.IsDirectory = true;
                  // Attempt to get a list of security permissions from the folder. 
                  // This will raise an exception if the path is read only or do not have access to view the permissions. 
                  (foundDirInfo as DirectoryInfo).GetAccessControl();
               }
               );
               dokanError = Dokan.DOKAN_SUCCESS;
            }
            else
               dokanError = Dokan.ERROR_PATH_NOT_FOUND;
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenDirectory threw: ", ex);
            dokanError = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("OpenDirectory OUT. dokanError[{0}]", dokanError);
         }
         return dokanError;
      }


      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;

         try
         {
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               Log.Debug("CreateDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
               FileSystemInfo foundDirInfo = roots.GetPath(filename, true);
               if (!foundDirInfo.Exists)
               {
                  foundDirInfo = Directory.CreateDirectory(foundDirInfo.FullName);
               }
               Log.Debug("By the time it gets here the dir should exist, or have existed by another method / thread");
               info.IsDirectory = true;
               roots.TrimAndAddUnique(foundDirInfo);
               dokanError = Dokan.DOKAN_SUCCESS;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateDirectory threw: ", ex);
            dokanError = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("CreateDirectory OUT dokanError[{0}]", dokanError);
         }
         return dokanError;
      }

      /*
      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
      If the file system application stored file handle in the refFileHandleContext variable when the function CreateFile is invoked, 
      this should be closed in the Cleanup function, not in CloseFile function. If the user application calls CloseHandle
      and subsequently open the same file, the CloseFile function of the file system application may not be invoked 
      before the CreateFile API is called. This may cause sharing violation error. 
      Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      complete the I/O operations. The file system application should also properly work in this case.
      */
      /// <summary>
      /// When info->DeleteOnClose is true, you must delete the file in Cleanup.
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int Cleanup(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("Cleanup IN DokanProcessId[{0}] with filename [{1}]", info.ProcessId, filename);
            CloseAndRemove(info);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (info.DeleteOnClose)
               {
                  if (info.IsDirectory)
                  {
                     Log.Trace("DeleteOnClose Directory");
                     roots.DeleteDirectory(filename);
                  }
                  else
                  {
                     roots.DeleteFile(filename);
                  }
               }
               else
               {
                  // Remove the cached entry as it will have new access time etc.
                  roots.RemoveFromLookup(filename);
               }
            });

         }
         catch (Exception ex)
         {
            Log.ErrorException("Cleanup threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("Cleanup OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("CloseFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            //CloseAndRemove(info);
         }
         catch (Exception ex)
         {
            Log.ErrorException("CloseFile threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("CloseFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      /// <summary>
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawBuffer"></param>
      /// <param name="rawBufferLength"></param>
      /// <param name="rawReadLength"></param>
      /// <param name="rawOffset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      /// <remarks>
      // Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      // complete the I/O operations. The file system application should also properly work in this case.      
      /// </remarks>
      public int ReadFileNative(string filename, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile [{0}] IN offset=[{2}] DokanProcessId[{1}]", filename, info.ProcessId, rawOffset);
            rawReadLength = 0;
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
               {
                  fileStream = openFiles[info.refFileHandleContext];
                  roots.RemoveTargetFromLookup(openFiles[info.refFileHandleContext].FullName);
               }
            }
            if (fileStream == null)
            {
               ProcessIdentity.Invoke(info.ProcessId, delegate
                  {
                     FileSystemInfo foundFileInfo = roots.GetPath(filename);
                     Log.Warn("No context handle for [{0}]", foundFileInfo.FullName);
                     fileStream = new FileStream(foundFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, (int)configDetails.BufferReadSize);
                     closeOnReturn = true;
                  });
            }

            // Some programs the file offset to extend the file length to write past the end of the file
            // Commented the check of rawOffset being off the size of the file.
            //if (rawOffset > fileStream.Length)
            //{
            //   errorCode = Dokan.DOKAN_ERROR;
            //}
            //else
            //{
            // Use the current offset as a check first to speed up access in large sequential file reads
            if (fileStream.Position != rawOffset)
               fileStream.Seek(rawOffset, SeekOrigin.Begin);
            if (0 == ReadFile(fileStream.SafeFileHandle, rawBuffer, rawBufferLength, out rawReadLength, IntPtr.Zero))
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("ReadFile threw: ", ex);
            errorCode = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (closeOnReturn
                  && (fileStream != null)
                  )
                  fileStream.Close();
            }
            catch (Exception ex)
            {
               Log.ErrorException("closeOnReturn threw: ", ex);
            }
            Log.Debug("ReadFile OUT readBytes=[{0}], errorCode[{1}]", rawReadLength, errorCode);
         }
         return errorCode;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawBuffer"></param>
      /// <param name="rawNumberOfBytesToWrite"></param>
      /// <param name="rawNumberOfBytesWritten"></param>
      /// <param name="rawOffset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      /// <remarks>
      // Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      // complete the I/O operations. The file system application should also properly work in this case.      
      /// </remarks>
      public int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         rawNumberOfBytesWritten = 0;
         FileStream fileStream = null;
         bool closeOnReturn = false;
         try
         {
            Log.Debug("WriteFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
               {
                  fileStream = openFiles[info.refFileHandleContext];
                  roots.RemoveTargetFromLookup(openFiles[info.refFileHandleContext].FullName);
               }
            }
            if (fileStream == null)
            {
               ProcessIdentity.Invoke(info.ProcessId, delegate
                  {
                     FileSystemInfo foundFileInfo = roots.GetPath(filename);
                     Log.Warn("No context handle for [{0}]", foundFileInfo.FullName);
                     fileStream = new FileStream(foundFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, (int)configDetails.BufferReadSize);
                     closeOnReturn = true;
                  });
            }

            if (info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
               fileStream.Seek(0, SeekOrigin.End);
            else
               // Use the current offset as a check first to speed up access in large sequential file reads
               if (fileStream.Position != rawOffset)
                  fileStream.Seek(rawOffset, SeekOrigin.Begin);

            if (0 == WriteFile(fileStream.SafeFileHandle, rawBuffer, rawNumberOfBytesToWrite, out rawNumberOfBytesWritten, IntPtr.Zero))
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFile threw: ", ex);
            errorCode = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (closeOnReturn
                  && (fileStream != null)
                  )
                  fileStream.Close();
            }
            catch (Exception ex)
            {
               Log.ErrorException("closeOnReturn threw: ", ex);
            }
            Log.Trace("WriteFile OUT Written[{0}] errorCode[{1}]", rawNumberOfBytesWritten, errorCode);
         }
         return errorCode;
      }


      public int FlushFileBuffers(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Debug("FlushFileBuffers IN [{0}] DokanProcessId[{1}]", filename, info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
               {
                  openFiles[info.refFileHandleContext].Flush();
                  roots.RemoveTargetFromLookup(openFiles[info.refFileHandleContext].FullName);
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("FlushFileBuffers OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetFileInformation(string filename, ref FileInformation fileinfo, DokanFileInfo info)
      {
         int dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
         try
         {
            Log.Debug("GetFileInformation [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            using (openFilesSync.ReadLock())
            {
               if (info.refFileHandleContext != 0)
               {
                  // We have a context, Which means that the file is open so the information may be stale !
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  roots.RemoveTargetFromLookup(openFiles[info.refFileHandleContext].FullName);
               }
            }
            FileSystemInfo fsi = roots.GetPath(filename);
            if (fsi is DirectoryInfo)
            {
               info.IsDirectory = true;
            }
            if (fsi.Exists)
            {
               ProcessIdentity.Invoke(info.ProcessId, delegate
               {
                  if (info.IsDirectory)
                     (fsi as DirectoryInfo).GetAccessControl();
                  else
                     (fsi as FileInfo).GetAccessControl();
               });
               // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
               // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
               // http://ss64.com/nt/attrib.html
               fileinfo.Attributes = fsi.Attributes | FileAttributes.NotContentIndexed;
               /*if (Log.IsTraceEnabled)
               fileinfo.Attributes |= FileAttributes.Offline;*/
               fileinfo.CreationTime = fsi.CreationTime;
               fileinfo.LastAccessTime = fsi.LastAccessTime;
               fileinfo.LastWriteTime = fsi.LastWriteTime;
               fileinfo.FileName = fsi.Name;
               // <- this is not used in the structure that is passed back to Dokan !
               fileinfo.Length = info.IsDirectory ? 0L : ((FileInfo)fsi).Length;
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileInformation threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("GetFileInformation OUT Attributes[{0}] Length[{1}] dokanReturn[{2}]", fileinfo.Attributes, fileinfo.Length, dokanReturn);
         }
         return dokanReturn;
      }

      public int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files, pattern);
      }

      public int FindFiles(string filename, out FileInformation[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files);
      }

      private int FindFiles(string filename, uint processId, out FileInformation[] files, string pattern = "*")
      {
         files = null;
         try
         {
            Log.Debug("FindFiles IN [{0}], pattern[{1}]", filename, pattern);
            // TODO: Find over shares
            //if ((filename != PathDirectorySeparatorChar)
            //   && filename.EndsWith(PathDirectorySeparatorChar)
            //   )
            //{
            //   // Win 7 uses this to denote a remote connection over the share
            //   filename = filename.TrimEnd(Path.DirectorySeparatorChar);
            //   if (!configDetails.KnownSharePaths.Contains(filename))
            //   {
            //      Log.Debug("Adding a new share for path: {0}", filename);
            //      configDetails.KnownSharePaths.Add(filename);
            //      FileSystemInfo foundFileInfo = roots.GetPath(filename);
            //      if (!foundFileInfo.Exists)
            //      {
            //         Log.Info("Share has not been traversed (Might be command line add");
            //         int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);
            //         if (lastDir > 0)
            //         {
            //            Log.Trace("Perform search for path: {0}", filename);
            //            filename = filename.Substring(0, lastDir);
            //         }
            //         else
            //            filename = PathDirectorySeparatorChar;
            //      }
            //   }
            //   Log.Debug("Will attempt to find share details for [{0}]", filename);
            //}
            Dictionary<string, FileInformation> uniqueFiles = new Dictionary<string, FileInformation>();
            ProcessIdentity.Invoke(processId, delegate
            {
               // Do this in reverse, so that the preferred refreences overwrite the older files
               for (int i = configDetails.SourceLocations.Count - 1; i >= 0; i--)
               {
                  AddFiles(configDetails.SourceLocations[i] + filename, uniqueFiles, pattern);
               }
            });
            files = new FileInformation[uniqueFiles.Values.Count];
            uniqueFiles.Values.CopyTo(files, 0);
         }
         catch (Exception ex)
         {
            Log.ErrorException("FindFiles threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("FindFiles OUT [found {0}]", (files != null ? files.Length : 0));
            if (Log.IsTraceEnabled)
            {
               if (files != null)
               {
                  StringBuilder sb = new StringBuilder();
                  sb.AppendLine();
                  foreach (FileInformation fileInformation in files)
                  {
                     sb.AppendLine(fileInformation.FileName);
                  }
                  Log.Trace(sb.ToString());
               }
            }
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         try
         {
            Log.Debug("SetFileAttributes [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            FileSystemInfo foundFileInfo = roots.GetPath(filename);
            if (foundFileInfo.Exists)
            {
               ProcessIdentity.Invoke(info.ProcessId, delegate
                  {
                     roots.RemoveTargetFromLookup(foundFileInfo.FullName);
                     // This uses  if (!Win32Native.SetFileAttributes(fullPathInternal, (int) fileAttributes))
                     // And can throw PathTOOLong
                     foundFileInfo.Attributes = attr;
                  });
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileAttributes threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetFileAttributes OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileTimeNative(string filename, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
          ref ComTypes.FILETIME rawLastWriteTime, DokanFileInfo info)
      {
         SafeFileHandle safeFileHandle = null;
         bool needToClose = false;
         try
         {
            Log.Debug("SetFileTime [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            FileSystemInfo fsi = roots.GetPath(filename);
            if (fsi is DirectoryInfo)
               info.IsDirectory = true;

            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (info.IsDirectory)
                  (fsi as DirectoryInfo).GetAccessControl();
               else
                  (fsi as FileInfo).GetAccessControl();
            });
            using (openFilesSync.ReadLock())
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  safeFileHandle = openFiles[info.refFileHandleContext].SafeFileHandle;
                  roots.RemoveTargetFromLookup(openFiles[info.refFileHandleContext].FullName);
               }
               else
               {
                  // Workaround the dir set
                  // ERROR LiquesceSvc.LiquesceOps: SetFileTime threw:  System.UnauthorizedAccessException: Access to the path 'G:\_backup\Kylie Minogue\Dir1' is denied.
                  // To create a handle to a directory, you have to use FILE_FLAG_BACK_SEMANTICS.
                  const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = info.IsDirectory
                                                  ? Proxy.FILE_FLAG_BACKUP_SEMANTICS
                                                  : 0;
                  safeFileHandle = CreateFileW(fsi.FullName, rawAccessMode, rawShare, IntPtr.Zero,
                                               rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);
                  needToClose = true;
               }
               if ((safeFileHandle != null)
                   && !safeFileHandle.IsInvalid
                  )
               {
                  if (!SetFileTime(safeFileHandle, ref rawCreationTime, ref rawLastAccessTime, ref rawLastWriteTime))
                  {
                     Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                  }
               }

            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileTime threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (needToClose
                   && (safeFileHandle != null)
                   && !safeFileHandle.IsInvalid
                  )
               {
                  safeFileHandle.Close();
               }
            }
            catch
            {
            }
            Log.Trace("SetFileTime OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      /// <summary>
      /// Because of how Dokan works:
      /// You should not delete file on DeleteFile or DeleteDirectory.
      // When DeleteFile or DeleteDirectory, you must check whether
      // you can delete or not, and return 0 (when you can delete it)
      // or appropriate error codes such as -ERROR_DIR_NOT_EMPTY,
      // -ERROR_SHARING_VIOLATION.
      // When you return 0 (ERROR_SUCCESS), you get Cleanup with
      // FileInfo->DeleteOnClose set TRUE, you delete the file.
      //
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int DeleteFile(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("DeleteFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               FileSystemInfo foundFileInfo = roots.GetPath(filename);
               dokanReturn = (foundFileInfo.Exists && (foundFileInfo is FileInfo))
                                ? Dokan.DOKAN_SUCCESS
                                : Dokan.ERROR_FILE_NOT_FOUND;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("DeleteFile OUT dokanReturn[(0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("DeleteDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               FileSystemInfo dirInfo = roots.GetPath(filename);
               if (dirInfo.Exists
                   && (dirInfo is DirectoryInfo)
                  )
               {
                  FileSystemInfo[] fileInfos = (dirInfo as DirectoryInfo).GetFileSystemInfos();
                  dokanReturn = (fileInfos.Length > 0) ? Dokan.ERROR_DIR_NOT_EMPTY : Dokan.DOKAN_SUCCESS;
               }
               else
                  dokanReturn = Dokan.ERROR_PATH_NOT_FOUND;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteDirectory threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("DeleteDirectory OUT dokanReturn[(0}]", dokanReturn);
         }

         return dokanReturn;
      }

      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("MoveFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  FileStreamName fileHandle = openFiles[info.refFileHandleContext];
                  if (fileHandle != null)
                  {
                     fileHandle.Close();
                     openFiles.Remove(info.refFileHandleContext);
                  }
               }
               if (filename == newname) // This is some weirdness that SyncToy tries to pull !!
                  dokanReturn = Dokan.DOKAN_SUCCESS;
               else
               {
                  FileSystemInfo nfo = roots.GetPath(filename);
                  if (!nfo.Exists)
                     dokanReturn = (nfo is DirectoryInfo) ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
                  else
                  {
                     XMoveFile.Move(roots, filename, newname, replaceIfExisting, info.IsDirectory);
                  }
               }
            });
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("MoveFile OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("SetEndOfFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               dokanReturn = SetAllocationSize(filename, length, info);
               if (dokanReturn == Dokan.ERROR_FILE_NOT_FOUND)
               {
                  using (
                     Stream stream = File.Open(roots.GetPath(filename).FullName, FileMode.Open, FileAccess.ReadWrite,
                                               FileShare.None))
                  {
                     stream.SetLength(length);
                  }
                  dokanReturn = Dokan.DOKAN_SUCCESS;
               }
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetEndOfFile OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("SetAllocationSize [{0}] IN DokanProcessId[{0}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  using (openFilesSync.ReadLock())
                     openFiles[info.refFileHandleContext].SetLength(length);
               }
               else
               {
                  // Setting file pointers positions is done with open handles !
                  dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
               }
            });
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSize threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetAllocationSize OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("LockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            ProcessIdentity.Invoke(info.ProcessId, delegate
            {
               if (length < 0)
               {
                  Log.Warn("Resetting length to [0] from [{0}]", length);
                  length = 0;
               }
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  using (openFilesSync.ReadLock())
                     openFiles[info.refFileHandleContext].Lock(offset, length);
               }
               else
               {
                  dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
               }
            });
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("LockFile OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         try
         {
            Log.Debug("UnlockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            if (length < 0)
            {
               Log.Warn("Resetting length to [0] from [{0}]", length);
               length = 0;
            }
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  openFiles[info.refFileHandleContext].Unlock(offset, length);
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFile threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("UnlockFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         try
         {
            Log.Trace("GetDiskFreeSpace IN DokanProcessId[{0}]", info.ProcessId);
            ulong localFreeBytesAvailable = 0, localTotalBytes = 0, localTotalFreeBytes = 0;
            configDetails.SourceLocations.ForEach(str =>
                                                     {
                                                        ulong num;
                                                        ulong num2;
                                                        ulong num3;
                                                        if (GetDiskFreeSpaceExW(str, out num, out num2, out num3))
                                                        {
                                                           localFreeBytesAvailable += num;
                                                           localTotalBytes += num2;
                                                           localTotalFreeBytes += num3;
                                                        }
                                                     });
            freeBytesAvailable = localFreeBytesAvailable;
            totalBytes = localTotalBytes;
            totalFreeBytes = localTotalFreeBytes;
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFile threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("GetDiskFreeSpace OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Warn("Unmount IN DokanProcessId[{0}]", info.ProcessId);
         using (openFilesSync.WriteLock())
         {
            foreach (FileStreamName obj2 in openFiles.Values)
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
         Log.Trace("Unmount out");
         return Dokan.DOKAN_SUCCESS;
      }

      // http://groups.google.com/group/dokan/browse_thread/thread/15c3f3cfd84c5ac1?pli=1
      // see http://code.google.com/p/dokan/issues/detail?id=209
      public int GetFileSecurityNative(string filename, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         Log.Debug("GetFileSecurityNative [{0}] IN GetFileSecurity[{1}][{2}]", filename, info.ProcessId, rawRequestedInformation);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            FileSystemInfo foundFileInfo = roots.GetPath(filename);

            if (foundFileInfo.Exists)
            {
               SECURITY_INFORMATION reqInfo = rawRequestedInformation;
               byte[] managedDescriptor = null;
               ProcessIdentity.Invoke(info.ProcessId, delegate
                  {
                     AccessControlSections includeSections = AccessControlSections.None;
                     if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Owner;
                     if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Group;
                     if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Access;
                     if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Audit;
                     FileSystemSecurity pSD = (foundFileInfo is FileInfo) ? (FileSystemSecurity)((FileInfo)foundFileInfo).GetAccessControl(includeSections)
                                                 : ((DirectoryInfo)foundFileInfo).GetAccessControl(includeSections);
                     pSD.SetAccessRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
                     pSD.SetAuditRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
                     if (Log.IsTraceEnabled)
                     {
                        string getFileSecurityNative = pSD.GetSecurityDescriptorSddlForm(includeSections);
                        Log.Trace("GetFileSecurityNative on {0} Retrieved [{1}]", foundFileInfo.FullName, getFileSecurityNative);
                     }
                     managedDescriptor = pSD.GetSecurityDescriptorBinaryForm();
                  });
               if (managedDescriptor != null)
               {
                  rawSecurityDescriptorLengthNeeded = (uint)managedDescriptor.Length;
               }
               // if the buffer is not enough the we must pass the correct error
               if (rawSecurityDescriptorLength < rawSecurityDescriptorLengthNeeded)
               {
                  return Dokan.ERROR_INSUFFICIENT_BUFFER;
               }
               else
               {
                  Marshal.Copy(managedDescriptor, 0, rawSecurityDescriptor, managedDescriptor.Length);
               }
            }
            else
            {
               dokanReturn = info.IsDirectory ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Info("GetFileSecurity OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      // see http://code.google.com/p/dokan/issues/detail?id=209
      public int SetFileSecurityNative(string filename, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, DokanFileInfo info)
      {
         Log.Debug("SetFileSecurityNative IN [{0}] SetFileSecurity[{1}][{2}]", filename, info.ProcessId, rawSecurityInformation);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            FileSystemInfo foundFileInfo = roots.GetPath(filename);
            if (foundFileInfo.Exists)
            {
               SECURITY_INFORMATION reqInfo = rawSecurityInformation;
               ProcessIdentity.Invoke(info.ProcessId, delegate
                  {
                     AccessControlSections includeSections = AccessControlSections.None;
                     if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Owner;
                     if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Group;
                     if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Access;
                     if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Audit;
                     FileSystemSecurity pSD;
                     if (foundFileInfo is FileInfo)
                        pSD = ((FileInfo)foundFileInfo).GetAccessControl(includeSections);
                     else
                     {
                        info.IsDirectory = true;
                        pSD = ((DirectoryInfo)foundFileInfo).GetAccessControl(includeSections);
                     }
                     byte[] binaryForm = new byte[rawSecurityDescriptorLength];
                     Marshal.Copy(rawSecurityDescriptor, binaryForm, 0, binaryForm.Length);
                     pSD.SetAccessRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
                     pSD.SetAuditRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
                     pSD.SetSecurityDescriptorBinaryForm(binaryForm, includeSections);
                     // Apply these changes.
                     if (info.IsDirectory)
                        ((DirectoryInfo)foundFileInfo).SetAccessControl((DirectorySecurity)pSD);
                     else
                     {
                        ((FileInfo)foundFileInfo).SetAccessControl((FileSecurity)pSD);
                     }
                  });
            }
            else
            {
               dokanReturn = info.IsDirectory ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Info("SetFileSecurity OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      #endregion


      private void AddFiles(string path, Dictionary<string, FileInformation> files, string pattern)
      {
         Log.Trace("AddFiles IN path[{0}] pattern[{1}]", path, pattern);
         try
         {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
               foreach (FileSystemInfo info2 in fileSystemInfos)
               {
                  AddToUniqueLookup(info2, files);
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
         }
      }

      private void AddToUniqueLookup(FileSystemInfo info2, Dictionary<string, FileInformation> files)
      {
         bool isDirectoy = (info2.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
         FileInformation item = new FileInformation
                                   {
                                      // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
                                      // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
                                      Attributes = info2.Attributes | FileAttributes.NotContentIndexed,
                                      CreationTime = info2.CreationTime,
                                      LastAccessTime = info2.LastAccessTime,
                                      LastWriteTime = info2.LastWriteTime,
                                      Length = (isDirectoy) ? 0L : ((FileInfo)info2).Length,
                                      FileName = info2.Name
                                   };
         if (Log.IsTraceEnabled)
            item.Attributes |= FileAttributes.Offline;
         files[roots.TrimAndAddUnique(info2)] = item;
      }

      private void CloseAndRemove(DokanFileInfo info)
      {
         if (info.refFileHandleContext != 0)
         {
            Log.Trace("CloseAndRemove info.refFileHandleContext [{0}]", info.refFileHandleContext);
            using (openFilesSync.UpgradableReadLock())
            {
               // The File can be closed by the remote client via Delete (as it does not know to close first!)
               FileStreamName fileStream;
               if (openFiles.TryGetValue(info.refFileHandleContext, out fileStream))
               {
                  using (openFilesSync.WriteLock())
                  {
                     openFiles.Remove(info.refFileHandleContext);
                  }
                  Log.Trace("CloseAndRemove [{0}] info.refFileHandleContext[{1}]", fileStream.FullName, info.refFileHandleContext);
                  fileStream.Flush();
                  fileStream.Close();
               }
               else
               {
                  Log.Debug("Something has already closed info.refFileHandleContext [{0}]", info.refFileHandleContext);
               }
            }
            info.refFileHandleContext = 0;
         }
      }

      public void InitialiseShares(object state)
      {
         Log.Debug("InitialiseShares IN");
         try
         {
            Thread.Sleep(250); // Give the driver some time to mount
            // Now check (in 2 phases) the existence of the drive
            string path = configDetails.DriveLetter + ":" + PathDirectorySeparatorChar;
            while (!Directory.Exists(path))
            {
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares");
               Thread.Sleep(1000);
            }
            // 2nd phase as the above is supposed to be cheap but can return false +ves
            do
            {
               string[] drives = Environment.GetLogicalDrives();
               if (Array.Exists(drives, dr => dr.Remove(1) == configDetails.DriveLetter))
                  break;
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares (Phase 2)");
               Thread.Sleep(100);
            } while (ManagementLayer.Instance.State == LiquesceSvcState.Running);

            configDetails.KnownSharePaths = new List<string>(configDetails.SharesToRestore.Count);
            foreach (LanManShareDetails shareDetails in configDetails.SharesToRestore)
            {
               configDetails.KnownSharePaths.Add(shareDetails.Path);
               try
               {
                  Log.Info("Restore share for : [{0}] [{1} : {2}]", shareDetails.Path, shareDetails.Name, shareDetails.Description);
                  LanManShareHandler.SetLanManShare(shareDetails);
               }
               catch (Exception ex)
               {
                  Log.ErrorException("Unable to restore share for : " + shareDetails.Path, ex);
               }
            }
            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.Running, "Shares restored - good to go");
         }
         catch (Exception ex)
         {
            Log.ErrorException("Init shares threw: ", ex);
            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.InError, "Init shares reports: " + ex.Message);
         }
         finally
         {
            Log.Debug("InitialiseShares OUT");
         }
      }

      #region DLL Imports
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

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int WriteFile(SafeFileHandle handle, IntPtr buffer,
        uint numBytesToWrite, out uint numBytesWritten, IntPtr /*NativeOverlapped* */ lpOverlapped);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern int ReadFile(SafeFileHandle handle, IntPtr bytes,
         uint numBytesToRead, out uint numBytesRead_mustBeZero, IntPtr /*NativeOverlapped* */ overlapped);

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out ulong lpFreeBytesAvailable,
         out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileTime(SafeFileHandle hFile, ref ComTypes.FILETIME lpCreationTime, ref ComTypes.FILETIME lpLastAccessTime, ref ComTypes.FILETIME lpLastWriteTime);

      #endregion
   }

   // This is used for tracking what file is in the store
   // If the code never looks for name, then it might be jitted out
   internal class FileStreamName : FileStream
   {
      public string FullName { get; private set; }

      public FileStreamName(string fullName, SafeFileHandle handle, FileAccess access, int bufferSize)
         : base(handle, access, bufferSize)
      {
         FullName = fullName;
      }
   }



}