﻿// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DokanNet;
using Microsoft.Win32.SafeHandles;
using NLog;
using Starksoft.Net.Ftp;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace ClientLiquesceFTPTray.Dokan
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly ClientShareDetail configDetails;
      private readonly FtpClient ftpInstance;

      // currently open files...
      // last key
      static private UInt64 openFilesLastKey;
      // lock
      static private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      // dictionary of all open files
      static private readonly Dictionary<UInt64, FileStreamName> openFiles = new Dictionary<UInt64, FileStreamName>();

      private readonly Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
      private readonly ReaderWriterLockSlim foundDirectoriesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

      public LiquesceOps(ClientShareDetail csd, FtpClient ftpInstance)
      {
         configDetails = csd;
         this.ftpInstance = ftpInstance;
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
         int actualErrorCode = DokanNet.Dokan.DOKAN_SUCCESS;
         try
         {
            Log.Debug(
               "CreateFile IN filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}], ProcessId[{5}]",
               filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //string path = roots.GetPath(filename, (rawCreationDisposition == Proxy.CREATE_NEW) || (rawCreationDisposition == Proxy.CREATE_ALWAYS));

            //if (Directory.Exists(path))
            //{
            //   actualErrorCode = OpenDirectory(filename, info);
            //   return actualErrorCode;
            //}
            //// Increment now in case there is an exception later
            //++openFilesLastKey; // never be Zero !
            //// Stop using exceptions to throw ERROR_FILE_NOT_FOUND
            //bool fileExists = File.Exists(path);
            //switch (rawCreationDisposition)
            //{
            //   //case FileMode.Create:
            //   //case FileMode.OpenOrCreate:
            //   //   if (fileExists)
            //   //      actualErrorCode = Dokan.ERROR_ALREADY_EXISTS;
            //   //   break;
            //   //case FileMode.CreateNew:
            //   //   if (fileExists)
            //   //      return Dokan.ERROR_FILE_EXISTS;
            //   //   break;
            //   case Proxy.OPEN_EXISTING:
            //   //case FileMode.Append:
            //   case Proxy.TRUNCATE_EXISTING:
            //      if (!fileExists)
            //      {
            //         Log.Debug("filename [{0}] ERROR_FILE_NOT_FOUND", filename);
            //         // Probably someone has removed this on the actual drive
            //         roots.RemoveFromLookup(filename);
            //         actualErrorCode = DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
            //         return actualErrorCode;
            //      }
            //      break;
            //}
            ////if (!fileExists)
            ////{
            ////   if (fileAccess == FileAccess.Read)
            ////   {
            ////      actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
            ////   }
            ////}

            //bool writeable = (((rawAccessMode & Proxy.FILE_WRITE_DATA) == Proxy.FILE_WRITE_DATA));

            //if (!fileExists && writeable)
            //{
            //   Log.Trace("We want to create a new file in: [{0}]", path);

            //   if (String.IsNullOrWhiteSpace(path))
            //   {
            //      actualErrorCode = DokanNet.Dokan.ERROR_ACCESS_DENIED;
            //      Log.Trace("Got no path!!!");
            //      return actualErrorCode;
            //   }

            //   Log.Trace("Check if directory exists: [{0}]", FileManager.GetLocationFromFilePath(path));
            //   if (!Directory.Exists(FileManager.GetLocationFromFilePath(path)))
            //   {
            //      Log.Trace("Have to create this directory.");
            //      Directory.CreateDirectory(FileManager.GetLocationFromFilePath(path));
            //   }
            //}
            //// TODO: The DokanFileInfo structure has the following extra things that need to be mapped tothe file open
            ////public bool PagingIo;
            ////public bool SynchronousIo;
            ////public bool Nocache;
            //// See http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx#caching_behavior
            //if (info.PagingIo)
            //   rawFlagsAndAttributes |= Proxy.FILE_FLAG_RANDOM_ACCESS;
            //if (info.Nocache)
            //   rawFlagsAndAttributes |= Proxy.FILE_FLAG_WRITE_THROUGH; // | Proxy.FILE_FLAG_NO_BUFFERING;
            //// FILE_FLAG_NO_BUFFERING flag requires that all I/O operations on the file handle be in multiples of the sector size, 
            //// AND that the I/O buffers also be aligned on addresses which are multiples of the sector size

            //if (info.SynchronousIo)
            //   rawFlagsAndAttributes |= Proxy.FILE_FLAG_SEQUENTIAL_SCAN;
            //SafeFileHandle handle = CreateFile(path, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);

            //if (handle.IsInvalid)
            //{
            //   Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            //}
            //FileStreamName fs = new FileStreamName(path, handle, writeable ? FileAccess.ReadWrite : FileAccess.Read, (int)configDetails.BufferReadSize);
            //using (openFilesSync.WriteLock())
            //{
            //   info.refFileHandleContext = openFilesLastKey; // never be Zero !
            //   openFiles.Add(openFilesLastKey, fs);
            //}
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
         int dokanError = DokanNet.Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            ////string path = roots.GetPath(filename);
            ////int start = path.IndexOf(Path.DirectorySeparatorChar);
            ////path = start > 0 ? path.Substring(start) : PathDirectorySeparatorChar;

            //List<string> currentMatchingDirs = new List<string>(configDetails.SourceLocations.Count);
            //foreach (string newTarget in
            //           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
            //{
            //   Log.Trace("Try and OpenDirectory from [{0}]", newTarget);
            //   if (Directory.Exists(newTarget))
            //   {
            //      Log.Trace("Directory.Exists[{0}] Adding details", newTarget);
            //      currentMatchingDirs.Add(newTarget);
            //   }
            //}
            //if (currentMatchingDirs.Count > 0)
            //{
            //   info.IsDirectory = true;
            //   using (foundDirectoriesSync.WriteLock())
            //      foundDirectories[filename] = currentMatchingDirs;
            //   dokanError = DokanNet.Dokan.DOKAN_SUCCESS;
            //}
            //else
            //{
            //   Log.Warn("Probably someone has removed this from the actual mounts.");
            //   roots.RemoveFromLookup(filename);
            //   dokanError = DokanNet.Dokan.ERROR_PATH_NOT_FOUND;
            //}

         }
         finally
         {
            Log.Trace("OpenDirectory OUT. dokanError[{0}]", dokanError);
         }
         return dokanError;
      }


      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = DokanNet.Dokan.DOKAN_ERROR;

         try
         {
            string path;
            throw new NotImplementedException("WriteFile");
            //Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
            //path = roots.GetPath(filename, true);
            //if (!Directory.Exists(path))
            //{
            //   Directory.CreateDirectory(path);
            //}
            //Log.Debug("By the time it gets here the dir should exist, or have existed by another method / thread");
            //info.IsDirectory = true;
            //roots.TrimAndAddUnique(path);
            ////if (configDetails.AllocationMode == ConfigDetails.AllocationModes.mirror)
            ////{
            ////   string mirrorpath = Roots.GetNewRoot(Roots.GetRoot(path), 0, filename) + Path.DirectorySeparatorChar + Roots.HIDDEN_MIRROR_FOLDER + filename;
            ////   MirrorToDo todo = new MirrorToDo();
            ////   FileManager.AddMirrorToDo(todo.CreateFolderCreate(filename, path, mirrorpath));
            ////}
            //dokanError = DokanNet.Dokan.DOKAN_SUCCESS;
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
            throw new NotImplementedException("WriteFile");
            //if (info.DeleteOnClose)
            //{
            //   if (info.IsDirectory)
            //   {
            //      Log.Trace("DeleteOnClose Directory");
            //      using (foundDirectoriesSync.UpgradableReadLock())
            //      {
            //         // Only delete the directories that this knew about before the delet was called 
            //         // (As the user may be moving files into the sources from the mount !!)
            //         List<string> targetDeletes = foundDirectories[filename];
            //         if (targetDeletes != null)
            //            for (int index = 0; index < targetDeletes.Count; index++)
            //            {
            //               // Use an index for speed (It all counts !)
            //               string fullPath = targetDeletes[index];
            //               Log.Trace("Deleting matched dir [{0}]", fullPath);
            //               Directory.Delete(fullPath, false);
            //            }
            //         using (foundDirectoriesSync.WriteLock())
            //            foundDirectories.Remove(filename);
            //      }
            //   }
            //   else
            //   {
            //      Log.Trace("DeleteOnClose File");
            //      string path = roots.GetPath(filename);
            //      File.Delete(path);
            //   }
            //   roots.RemoveFromLookup(filename);
            //}
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("CloseFile IN DokanProcessId[{0}]", info.ProcessId);
            CloseAndRemove(info);
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }


      public int ReadFileNative(string filename, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo info)
      {
         int errorCode = DokanNet.Dokan.DOKAN_SUCCESS;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, rawOffset);
            throw new NotImplementedException("WriteFile");
            //rawReadLength = 0;
            //if (info.refFileHandleContext == 0)
            //{
            //   string path = roots.GetPath(filename);
            //   Log.Warn("No context handle for [" + path + "]");
            //   fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (int)configDetails.BufferReadSize);
            //   closeOnReturn = true;
            //}
            //else
            //{
            //   Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //   using (openFilesSync.ReadLock())
            //      fileStream = openFiles[info.refFileHandleContext];
            //}
            //if (rawOffset > fileStream.Length)
            //{
            //   errorCode = DokanNet.Dokan.DOKAN_ERROR;
            //}
            //else
            //{
            //   fileStream.Seek(rawOffset, SeekOrigin.Begin);
            //   // readBytes = (uint)fileStream.Read(buffer, 0, buffer.Length);
            //   if (0 == ReadFile(fileStream.SafeFileHandle, rawBuffer, rawBufferLength, out rawReadLength, IntPtr.Zero))
            //   {
            //      Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            //   }
            //   //else if ( rawReadLength == 0 )
            //   //{
            //   //   // ERROR_HANDLE_EOF 38 (0x26)
            //   //   if (fileStream.Position == fileStream.Length)
            //   //      errorCode = -38;
            //   //}
            //}
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

      public int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info)
      {
         int errorCode = DokanNet.Dokan.DOKAN_SUCCESS;
         rawNumberOfBytesWritten = 0;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //if (info.refFileHandleContext != 0)
            //{
            //   Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //   FileStreamName fileStream;
            //   using (openFilesSync.ReadLock())
            //      fileStream = openFiles[info.refFileHandleContext];
            //   fileStream.fsi = null;
            //   if (!info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
            //      fileStream.Seek(rawOffset, SeekOrigin.Begin);
            //   else
            //      fileStream.Seek(0, SeekOrigin.End);
            //   if (0 == WriteFile(fileStream.SafeFileHandle, rawBuffer, rawNumberOfBytesToWrite, out rawNumberOfBytesWritten, IntPtr.Zero))
            //   {
            //      Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            //   }
            //}
            //else
            //{
            //   errorCode = DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
            //}
         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFile threw: ", ex);
            errorCode = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("WriteFile OUT Written[{0}] errorCode[{1}]", rawNumberOfBytesWritten, errorCode);
         }
         return errorCode;
      }


      public int FlushFileBuffers(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("FlushFileBuffers IN DokanProcessId[{0}]", info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  openFiles[info.refFileHandleContext].Flush();
            }
            else
            {
               return DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int GetFileInformation(string filename, ref FileInformation fileinfo, DokanFileInfo info)
      {
         int dokanReturn = DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
         try
         {
            Log.Trace("GetFileInformation IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo fsi = null;
            using (openFilesSync.ReadLock())
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  fsi = openFiles[info.refFileHandleContext].fsi;
               }
               if (fsi == null)
               {
                  throw new NotImplementedException("WriteFile");
                  //string path = roots.GetPath(filename);
                  //if (File.Exists(path))
                  //{
                  //   fsi = new FileInfo(path);
                  //}
                  //else if (Directory.Exists(path))
                  //{
                  //   fsi = new DirectoryInfo(path);
                  //   info.IsDirectory = true;
                  //}
                  //// Store the fsi away, as ALL calls for _any_ information will be routed through this API.
                  //if ((fsi != null)
                  //    && (info.refFileHandleContext != 0)
                  //   )
                  //{
                  //   openFiles[info.refFileHandleContext].fsi = fsi;
                  //}
               }
            }
            if (fsi != null)
            {
               // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
               // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
               fileinfo.Attributes = fsi.Attributes | FileAttributes.NotContentIndexed;
               if (Log.IsTraceEnabled)
                  fileinfo.Attributes |= FileAttributes.Offline;
               fileinfo.CreationTime = fsi.CreationTime;
               fileinfo.LastAccessTime = fsi.LastAccessTime;
               fileinfo.LastWriteTime = fsi.LastWriteTime;
               fileinfo.FileName = fsi.Name; // <- this is not used in the structure that is passed back to Dokan !
               fileinfo.Length = info.IsDirectory ? 0L : ((FileInfo)fsi).Length;
               dokanReturn = DokanNet.Dokan.DOKAN_SUCCESS;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffers threw: ", ex);
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
         return FindFiles(filename, out files, pattern);
      }

      public int FindFiles(string filename, out FileInformation[] files, DokanFileInfo info)
      {
         return FindFiles(filename, out files);
      }

      private int FindFiles(string filename, out FileInformation[] files, string pattern = "*")
      {
         files = null;
         try
         {
            Log.Debug("FindFiles IN [{0}], pattern[{1}]", filename, pattern);
            if ((filename != PathDirectorySeparatorChar)
               && filename.EndsWith(PathDirectorySeparatorChar)
               )
            {
               // Win 7 uses this to denote a remote connection over the share
               filename = filename.TrimEnd(Path.DirectorySeparatorChar);
               Log.Debug("Will attempt to find share details for [{0}]", filename);
            }
            Dictionary<string, FileInformation> uniqueFiles = new Dictionary<string, FileInformation>();
            // Do this in reverse, so that the preferred refreences overwrite the older files
            //for (int i = configDetails.SourceLocations.Count - 1; i >= 0; i--)
            //{
            //   AddFiles(configDetails.SourceLocations[i] + filename, uniqueFiles, pattern);
            //}

            files = new FileInformation[uniqueFiles.Values.Count];
            uniqueFiles.Values.CopyTo(files, 0);
            throw new NotImplementedException("WriteFile");
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
                  for (int index = 0; index < files.Length; index++)
                  {
                     FileInformation fileInformation = files[index];
                     sb.AppendLine(fileInformation.FileName);
                  }
                  Log.Trace(sb.ToString());
               }
            }
         }
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetFileAttributes IN DokanProcessId[{0}]", info.ProcessId);
               throw new NotImplementedException("WriteFile");
               //string path = roots.GetPath(filename);
               //// This uses  if (!Win32Native.SetFileAttributes(fullPathInternal, (int) fileAttributes))
               //// And can throw PathTOOLong
               //File.SetAttributes(path, attr);
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int SetFileTimeNative(string filename, ref FILETIME rawCreationTime, ref FILETIME rawLastAccessTime,
          ref FILETIME rawLastWriteTime, DokanFileInfo info)
      {
         SafeFileHandle safeFileHandle = null;
         bool needToClose = false;
         try
         {
            Log.Trace("SetFileTime IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //using (openFilesSync.ReadLock())
            //{
            //   if (info.refFileHandleContext != 0)
            //   {
            //      Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //      safeFileHandle = openFiles[info.refFileHandleContext].SafeFileHandle;
            //   }
            //   else
            //   {
            //      // Workaround the dir set
            //      // ERROR LiquesceSvc.LiquesceOps: SetFileTime threw:  System.UnauthorizedAccessException: Access to the path 'G:\_backup\Kylie Minogue\Dir1' is denied.
            //      // To create a handle to a directory, you have to use FILE_FLAG_BACK_SEMANTICS.
            //      string path = roots.GetPath(filename);
            //      const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
            //      const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
            //      const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
            //      uint rawFlagsAndAttributes = Directory.Exists(path) ? Proxy.FILE_FLAG_BACKUP_SEMANTICS : 0;
            //      safeFileHandle = CreateFile(path, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);
            //      needToClose = true;
            //   }
            //   if ((safeFileHandle != null)
            //      && !safeFileHandle.IsInvalid
            //      )
            //   {
            //      if (!SetFileTime(safeFileHandle, ref rawCreationTime, ref rawLastAccessTime, ref rawLastWriteTime))
            //      {
            //         Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            //      }
            //   }
            //}
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      /// <summary>
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
         int dokanReturn = DokanNet.Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            // dokanReturn = (File.Exists(roots.GetPath(filename)) ? DokanNet.Dokan.DOKAN_SUCCESS : DokanNet.Dokan.ERROR_FILE_NOT_FOUND);
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
         int dokanReturn = DokanNet.Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //string path = roots.GetPath(filename);
            //DirectoryInfo dirInfo = new DirectoryInfo(path);
            //if (dirInfo.Exists)
            //{
            //   FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();
            //   dokanReturn = (fileInfos.Length > 0) ? DokanNet.Dokan.ERROR_DIR_NOT_EMPTY : DokanNet.Dokan.DOKAN_SUCCESS;
            //}
            //else
            //   dokanReturn = DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
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
         try
         {
            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);
            //if (filename == newname)   // This is some weirdness that SyncToy tries to pull !!
            //   return DokanNet.Dokan.DOKAN_SUCCESS;

            //string pathTarget = roots.GetPath(newname, true);

            //if (!info.IsDirectory)
            //{
            //   if (info.refFileHandleContext != 0)
            //   {
            //      Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //      FileStreamName fileHandle = openFiles[info.refFileHandleContext];
            //      if (fileHandle != null)
            //         fileHandle.Close();
            //   }
            //   string pathSource = roots.GetPath(filename);
            //   Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
            //   XMoveFile(pathSource, pathTarget, replaceIfExisting);
            //   roots.RemoveTargetFromLookup(pathSource);
            //}
            //else
            //{

            //   // getting all paths of the source location
            //   List<string> allDirSources = roots.GetAllPaths(filename);
            //   if (allDirSources.Count == 0)
            //   {
            //      Log.Error("MoveFile: Could not find directory [{0}]", filename);
            //      return DokanNet.Dokan.DOKAN_ERROR;
            //   }
            //   // rename every 
            //   foreach (string dirSource in allDirSources)
            //   {
            //      string dirTarget = Roots.GetRoot(dirSource) + newname;
            //      filemanager.XMoveDirectory(dirSource, dirTarget, replaceIfExisting);
            //   }
            //}
         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFile threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("MoveFile OUT");
         }
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = DokanNet.Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetEndOfFile IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");

            //dokanReturn = SetAllocationSize(filename, length, info);
            //if (dokanReturn == DokanNet.Dokan.ERROR_FILE_NOT_FOUND)
            //{
            //   string path = roots.GetPath(filename);
            //   using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            //   {
            //      stream.SetLength(length);
            //   }
            //   dokanReturn = DokanNet.Dokan.DOKAN_SUCCESS;
            //}
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetEndOfFile OUT", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetAllocationSize IN DokanProcessId[{0}]", info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  openFiles[info.refFileHandleContext].SetLength(length);
            }
            else
            {
               // Setting file pointers positions is done with open handles !
               return DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSize threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetAllocationSize OUT");
         }
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         try
         {
            Log.Trace("LockFile IN DokanProcessId[{0}]", info.ProcessId);
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
               return DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFile threw: ", ex);
            return Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("LockFile OUT");
         }
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         try
         {
            Log.Trace("UnlockFile IN DokanProcessId[{0}]", info.ProcessId);
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
               return DokanNet.Dokan.ERROR_FILE_NOT_FOUND;
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         try
         {
            Log.Trace("GetDiskFreeSpace IN DokanProcessId[{0}]", info.ProcessId);
            ulong localFreeBytesAvailable = 0, localTotalBytes = 0, localTotalFreeBytes = 0;
            throw new NotImplementedException("WriteFile");
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Trace("Unmount IN DokanProcessId[{0}]", info.ProcessId);
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
         return DokanNet.Dokan.DOKAN_SUCCESS;
      }

      public int GetFileSecurityNative(string file, ref SECURITY_INFORMATION rawRequestedInformation, ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int SetFileSecurityNative(string file, ref SECURITY_INFORMATION rawSecurityInformation, ref SECURITY_DESCRIPTOR rawSecurityDescriptor, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         throw new NotImplementedException();
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
         throw new NotImplementedException("WriteFile");
         //files[roots.TrimAndAddUnique(info2.FullName)] = item;
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
                  Log.Trace("CloseAndRemove [{0}] info.refFileHandleContext[{1}]", fileStream.Name,
                            info.refFileHandleContext);
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


      #region For the ShareEnabler

      /// <summary>
      /// Will only return tha actual readbytes array size, May be null or zero bytes long
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="buffer"></param>
      /// <param name="requestedReadLength"></param>
      /// <param name="actualReadLength"></param>
      /// <param name="offset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      internal int ReadFile(string filename, out byte[] buffer, int requestedReadLength, out int actualReadLength, long offset, DokanFileInfo info)
      {
         int errorCode = DokanNet.Dokan.DOKAN_SUCCESS;
         actualReadLength = 0;
         buffer = null;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, offset);
            throw new NotImplementedException("WriteFile");

            //if (info.refFileHandleContext == 0)
            //{
            //   string path = roots.GetPath(filename);
            //   Log.Warn("No context handle for [" + path + "]");
            //   fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (int)configDetails.BufferReadSize);
            //   closeOnReturn = true;
            //}
            //else
            //{
            //   Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //   using (openFilesSync.ReadLock())
            //      fileStream = openFiles[info.refFileHandleContext];
            //}
            //fileStream.Seek(offset, SeekOrigin.Begin);
            //byte[] internalBuffer = new byte[requestedReadLength];
            //actualReadLength = fileStream.Read(internalBuffer, 0, requestedReadLength);
            //if (actualReadLength != requestedReadLength)
            //   Array.Resize(ref internalBuffer, actualReadLength);
            //buffer = internalBuffer;
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
               Log.ErrorException("ReadFile closing filestream threw: ", ex);
            }
            Log.Debug("ReadFile OUT readBytes=[{0}], errorCode[{1}]", actualReadLength, errorCode);
         }
         return errorCode;
      }

      public int WriteFile(string filename, byte[] buffer, long offset, DokanFileInfo info)
      {
         int errorCode = DokanNet.Dokan.DOKAN_ERROR;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            throw new NotImplementedException("WriteFile");
            //if (info.refFileHandleContext == 0)
            //{
            //   string path = roots.GetPath(filename);
            //   Log.Warn("No context handle for [" + path + "]");
            //   fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, (int)configDetails.BufferReadSize);
            //   closeOnReturn = true;
            //}
            //else
            //{
            //   Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            //   using (openFilesSync.ReadLock())
            //      fileStream = openFiles[info.refFileHandleContext];
            //   ((FileStreamName)fileStream).fsi = null;
            //}
            //if (!info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
            //   fileStream.Seek(offset, SeekOrigin.Begin);
            //else
            //   fileStream.Seek(0, SeekOrigin.End);
            //fileStream.Write(buffer, 0, buffer.Length);
            //errorCode = DokanNet.Dokan.DOKAN_SUCCESS;
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
               Log.ErrorException("WriteFile closing filestream threw: ", ex);
            }
            Log.Trace("WriteFile OUT errorCode[{0}]", errorCode);
         }
         return errorCode;
      }

      #endregion


   }

   // This is used for tracking what file is in the store
   // If the code never looks for name, then it might be jitted out
   internal class FileStreamName : FileStream
   {
      public new string Name { get; private set; }

      public FileSystemInfo fsi { get; set; }

      public FileStreamName(string name, SafeFileHandle handle, FileAccess access, int bufferSize)
         : base(handle, access, bufferSize)
      {
         Name = name;
      }
   }
}