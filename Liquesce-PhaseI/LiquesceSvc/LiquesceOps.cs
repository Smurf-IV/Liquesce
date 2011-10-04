// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

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
            if (!fileExists
               && (writeable
                  || createNew )
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
            SafeFileHandle handle = CreateFileW(fullName, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);

            if (handle.IsInvalid)
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
            FileStreamName fs = new FileStreamName(fullName, handle, writeable ? FileAccess.ReadWrite : FileAccess.Read, (int)configDetails.BufferReadSize);
            using (openFilesSync.WriteLock())
            {
               info.refFileHandleContext = openFilesLastKey; // never be Zero !
               openFiles.Add(openFilesLastKey, fs);
            }
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
            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo foundDirInfo = roots.GetPath(filename);
            if (foundDirInfo.Exists
               && (foundDirInfo is DirectoryInfo))
            {
               info.IsDirectory = true;
               dokanError = Dokan.DOKAN_SUCCESS;
            }
            else
               dokanError = Dokan.ERROR_PATH_NOT_FOUND;
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
            Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo foundDirInfo = roots.GetPath(filename, true);
            if (!foundDirInfo.Exists)
            {
               foundDirInfo = Directory.CreateDirectory(foundDirInfo.FullName);
            }
            Log.Debug("By the time it gets here the dir should exist, or have existed by another method / thread");
            info.IsDirectory = true;
            roots.TrimAndAddUnique(foundDirInfo);
            dokanError = Dokan.DOKAN_SUCCESS;
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
            FileSystemInfo foundFileInfo = roots.GetPath(filename);
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
         return Dokan.DOKAN_SUCCESS;
      }


      public int ReadFileNative(string filename, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, rawOffset);
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
               FileSystemInfo foundFileInfo = roots.GetPath(filename);
               Log.Warn("No context handle for [{0}]", foundFileInfo.FullName);
               fileStream = new FileStream(foundFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
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

      public int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         rawNumberOfBytesWritten = 0;
         FileStream fileStream = null;
         bool closeOnReturn = false;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
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
               FileSystemInfo foundFileInfo = roots.GetPath(filename);
               Log.Warn("No context handle for [{0}]", foundFileInfo.FullName);
               fileStream = new FileStream(foundFileInfo.FullName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
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
            Log.Trace("FlushFileBuffers IN DokanProcessId[{0}]", info.ProcessId);
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
            Log.Trace("GetFileInformation IN DokanProcessId[{0}]", info.ProcessId);
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
               // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
               // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
               // http://ss64.com/nt/attrib.html
               fileinfo.Attributes = fsi.Attributes | FileAttributes.NotContentIndexed;
               /*if (Log.IsTraceEnabled)
                  fileinfo.Attributes |= FileAttributes.Offline;*/
               fileinfo.CreationTime = fsi.CreationTime;
               fileinfo.LastAccessTime = fsi.LastAccessTime;
               fileinfo.LastWriteTime = fsi.LastWriteTime;
               fileinfo.FileName = fsi.Name; // <- this is not used in the structure that is passed back to Dokan !
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
            // Do this in reverse, so that the preferred refreences overwrite the older files
            for (int i = configDetails.SourceLocations.Count - 1; i >= 0; i--)
            {
               AddFiles(configDetails.SourceLocations[i] + filename, uniqueFiles, pattern);
            }

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
            Log.Trace("SetFileAttributes IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo foundFileInfo = roots.GetPath(filename);
            if (foundFileInfo.Exists)
            {
               roots.RemoveTargetFromLookup(foundFileInfo.FullName);
               // This uses  if (!Win32Native.SetFileAttributes(fullPathInternal, (int) fileAttributes))
               // And can throw PathTOOLong
               foundFileInfo.Attributes = attr;
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
            Log.Trace("SetFileTime IN DokanProcessId[{0}]", info.ProcessId);
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
                  FileSystemInfo foundFileInfo = roots.GetPath(filename);
                  uint rawFlagsAndAttributes = (foundFileInfo is DirectoryInfo) ? Proxy.FILE_FLAG_BACKUP_SEMANTICS : 0;
                  safeFileHandle = CreateFileW(foundFileInfo.FullName, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);
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
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo foundFileInfo = roots.GetPath(filename);
            dokanReturn = (foundFileInfo.Exists && (foundFileInfo is FileInfo)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND;
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
            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
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
            if (filename == newname)   // This is some weirdness that SyncToy tries to pull !!
               return Dokan.DOKAN_SUCCESS;
            else
            {
               FileSystemInfo nfo = roots.GetPath(filename);
               if (!nfo.Exists)
                  return (nfo is DirectoryInfo) ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
            }
            XMoveFile.Move(roots, filename, newname, replaceIfExisting, info.IsDirectory);
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
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetEndOfFile IN DokanProcessId[{0}]", info.ProcessId);
            dokanReturn = SetAllocationSize(filename, length, info);
            if (dokanReturn == Dokan.ERROR_FILE_NOT_FOUND)
            {
               using (Stream stream = File.Open(roots.GetPath(filename).FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
               {
                  stream.SetLength(length);
               }
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
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
               return Dokan.ERROR_FILE_NOT_FOUND;
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
         return Dokan.DOKAN_SUCCESS;
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
               return Dokan.ERROR_FILE_NOT_FOUND;
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
         return Dokan.DOKAN_SUCCESS;
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
         return Dokan.DOKAN_SUCCESS;
      }

      // http://groups.google.com/group/dokan/browse_thread/thread/15c3f3cfd84c5ac1?pli=1
      //
      public int GetFileSecurityNative(string file, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         Log.Trace("Unmount IN GetFileSecurity[{0}]", info.ProcessId);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            FileSystemInfo foundFileInfo = roots.GetPath(file);

            if (foundFileInfo.Exists)
            {
               // TODO: Really this should adopt the callers ACL and then impersonate them for the retrieval of these perms
               // In the meantime the following needs to be removed
               // SACL_SECURITY_INFORMATION: Requires access @ ACCESS_SYSTEM_SECURITY; The SACL of the object is being referenced.
               // see http://code.google.com/p/dokan/issues/detail?id=209
               rawRequestedInformation &= ~SECURITY_INFORMATION.SACL_SECURITY_INFORMATION;
               if (!GetFileSecurityW(foundFileInfo.FullName, rawRequestedInformation, /*ref*/ rawSecurityDescriptor, rawSecurityDescriptorLength, ref rawSecurityDescriptorLengthNeeded))
               {
                  // if the buffer is not enough the we must pass the correct error
                  if (rawSecurityDescriptorLength < rawSecurityDescriptorLengthNeeded)
                  {
                     return Dokan.ERROR_INSUFFICIENT_BUFFER;
                  }
                  else
                  {
                     Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                  }
               }
               else if (Log.IsDebugEnabled)
               {
                  string h = string.Empty;
                  try
                  {
                     IntPtr pStringSD;
                     int stringSDLen;
                     ConvertSecurityDescriptorToStringSecurityDescriptor(rawSecurityDescriptor, 1, rawRequestedInformation, out pStringSD, out stringSDLen);
                     h = Marshal.PtrToStringAuto(pStringSD, stringSDLen);
                     if (pStringSD != IntPtr.Zero)
                        Marshal.FreeHGlobal(pStringSD);
                  }
                  finally
                  {
                     Log.Trace("GetFileSecurityNative on {0} Retrieved [{1}]", foundFileInfo.FullName, h);
                  }
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

      public int SetFileSecurityNative(string file, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, DokanFileInfo info)
      {
         Log.Trace("Unmount IN SetFileSecurity[{0}]", info.ProcessId);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            FileSystemInfo foundFileInfo = roots.GetPath(file);
            if (foundFileInfo.Exists)
            {
               // TODO: Really this should adopt the callers ACL and then impersonate them for the setting of these perms
               // In the meantime the following needs to be removed
               // SACL_SECURITY_INFORMATION: Requires access @ ACCESS_SYSTEM_SECURITY; The SACL of the object is being referenced.
               // see http://code.google.com/p/dokan/issues/detail?id=209
               rawSecurityInformation &= ~SECURITY_INFORMATION.SACL_SECURITY_INFORMATION;
               if (!SetFileSecurityW(foundFileInfo.FullName, rawSecurityInformation, /*ref*/ rawSecurityDescriptor))
               {
                  Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
               }
               else if (Log.IsDebugEnabled)
               {
                  string h = string.Empty;
                  try
                  {
                     IntPtr pStringSD;
                     int stringSDLen;
                     ConvertSecurityDescriptorToStringSecurityDescriptor(rawSecurityDescriptor, 1, rawSecurityInformation, out pStringSD, out stringSDLen);
                     h = Marshal.PtrToStringAuto(pStringSD, stringSDLen);
                     if (pStringSD != IntPtr.Zero)
                        Marshal.FreeHGlobal(pStringSD);
                  }
                  finally
                  {
                     Log.Trace("SetFileSecurityNative on {0} Retrieved [{1}]", foundFileInfo.FullName, h);
                  }
               }
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
                  // Got to force the file to be found so, that the share has somewhere to attach to
                  string connectSearch = shareDetails.Path.Replace(Path.GetPathRoot(shareDetails.Path), PathDirectorySeparatorChar);
                  OpenDirectory(connectSearch, new DokanFileInfo());
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

      /// <summary>
      /// The GetFileSecurity function obtains specified information about the security of a file or directory. The information obtained is constrained by the caller's access rights and privileges.
      ///	The GetNamedSecurityInfo function provides functionality similar to GetFileSecurity for files as well as other types of objects.
      /// Windows NT 3.51 and earlier:  The GetNamedSecurityInfo function is not supported.
      /// </summary>
      /// <param name="lpFileName">[in] Pointer to a null-terminated string that specifies the file or directory for which security information is retrieved.</param>
      /// <param name="requestedInformation">[in] A SecurityInformation value that identifies the security information being requested. </param>
      /// <param name="pSecurityDescriptor">[out] Pointer to a buffer that receives a copy of the security descriptor of the object specified by the lpFileName parameter. The calling process must have permission to view the specified aspects of the object's security status. The SECURITY_DESCRIPTOR structure is returned in self-relative format.</param>
      /// <param name="length">[in] Specifies the size, in bytes, of the buffer pointed to by the pSecurityDescriptor parameter.</param>
      /// <param name="lengthNeeded">[out] Pointer to the variable that receives the number of bytes necessary to store the complete security descriptor. If the returned number of bytes is less than or equal to nLength, the entire security descriptor is returned in the output buffer; otherwise, none of the descriptor is returned.</param>
      /// <returns></returns>
      [DllImport("AdvAPI32.DLL", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetFileSecurityW(string lpFileName, SECURITY_INFORMATION requestedInformation,
         IntPtr /*[MarshalAs(UnmanagedType.Struct)] ref SECURITY_DESCRIPTOR*/ pSecurityDescriptor, uint length, ref uint lengthNeeded);

      /// <summary>
      /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa376397%28v=vs.85%29.aspx
      /// </summary>
      /// <param name="pSelfRelativeSD"></param>
      /// <param name="RequestedStringSDRevision"></param>
      /// <param name="SecurityInformation"></param>
      /// <param name="StringSecurityDescriptor"></param>
      /// <param name="StringSecurityDescriptorLen"></param>
      /// <returns></returns>
      [DllImport("AdvAPI32.DLL", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Auto)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor([In()] IntPtr pSelfRelativeSD, int RequestedStringSDRevision,
      SECURITY_INFORMATION SecurityInformation, out IntPtr StringSecurityDescriptor, out int StringSecurityDescriptorLen);

      /// <summary>
      /// http://msdn.microsoft.com/en-us/library/aa379577%28v=VS.85%29.aspx
      /// </summary>
      /// <param name="pFileName"></param>
      /// <param name="pSIRequested"></param>
      /// <param name="pSD"></param>
      /// <returns>If the function succeeds, the function returns nonzero.
      /// If the function fails, it returns zero. To get extended error information, call GetLastError.
      /// </returns>
      [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileSecurityW(string pFileName, /*[In]*/ SECURITY_INFORMATION pSIRequested, [In()] IntPtr /*ref SECURITY_DESCRIPTOR*/ pSD);

      #endregion
   }

   // This is used for tracking what file is in the store
   // If the code never looks for name, then it might be jitted out
   internal class FileStreamName : FileStream
   {
      public string FullName { get; private set; }

      //public FileSystemInfo fsi { get; set; }

      public FileStreamName(string fullName, SafeFileHandle handle, FileAccess access, int bufferSize)
         : base(handle, access, bufferSize)
      {
         FullName = fullName;
      }
   }
}