// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DokanNet;
using LiquesceFacade;
using LiquesceSvcMEF;
using Microsoft.Win32.SafeHandles;
using NLog;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly ConfigDetails configDetails;
      private readonly IServicePlugin plugin;

      // currently open files...
      // last key
      static private UInt64 openFilesLastKey;
      // lock
      static private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      // dictionary of all open files
      static private readonly Dictionary<UInt64, FileStreamName> openFiles = new Dictionary<UInt64, FileStreamName>();

      /// <summary>
      /// Constructor to manage this object
      /// </summary>
      /// <param name="configDetails"></param>
      /// <param name="plugin"></param>
      public LiquesceOps(ConfigDetails configDetails, IServicePlugin plugin)
      {
         this.configDetails = configDetails;
         this.plugin = plugin;
         plugin.KnownSharePaths = configDetails.KnownSharePaths;
         plugin.SourceLocations = configDetails.SourceLocations;
      }

      #region IDokanOperations Implementation

      /// <summary>
      /// The information given in the Dokan info is a bit misleading about the return codes
      /// This is what the Win OS suystem is expecting http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx
      /// So.. Everything succeeds but the Return code is ERROR_ALREADY_EXISTS
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="rawFlagsAndAttributes"></param>
      /// <param name="info"></param>
      /// <param name="rawAccessMode"></param>
      /// <param name="rawShare"></param>
      /// <param name="rawCreationDisposition"></param>
      /// <returns></returns>
      public int CreateFile(string dokanPath, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info)
      {
         int actualErrorCode = Dokan.DOKAN_SUCCESS;
         // Increment now in case there is an exception later, Used inthe finally operation to denote a CreateFile operation indexer
         ++openFilesLastKey; // never be Zero !
         try
         {
            Log.Debug(
               "CreateFile IN dokanPath [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}], ProcessId[{5}]",
               dokanPath, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info.ProcessId);
            bool isCreate = (rawCreationDisposition == Proxy.CREATE_NEW) || (rawCreationDisposition == Proxy.CREATE_ALWAYS);
            string actualLocation = isCreate ? plugin.CreateLocation(dokanPath) : plugin.OpenLocation(dokanPath);
            bool writeable = (((rawAccessMode & Proxy.FILE_WRITE_DATA) == Proxy.FILE_WRITE_DATA));

            // Need to solve the issue with Synctoy performing moves into unknown / unused space
            // MoveFile pathSource: [F:\_backup\Kylie Minogue\FSP01FA0CF932F74BF5AE5C217F4AE6626B.tmp] pathTarget: [G:\_backup\Kylie Minogue\(2010) Aphrodite\12 - Can't Beat The Feeling.mp3] 
            // MoveFile threw:  System.IO.DirectoryNotFoundException: The system cannot find the path specified. (Exception from HRESULT: 0x80070003)
            string newDir = Path.GetDirectoryName(actualLocation);
            if (!String.IsNullOrEmpty(newDir)
               && (isCreate || writeable)
               )
            {
               DirectoryInfo dirInfo = new DirectoryInfo(newDir);
               if (!dirInfo.Exists)
               {
                  Log.Trace("We want to create a new file in: [{0}]", newDir);
                  dirInfo.Create();
               }
            }

            if (Directory.Exists(actualLocation))
            {
               actualErrorCode = OpenDirectory(dokanPath, info);
               return actualErrorCode;
            }
            switch (rawCreationDisposition)
            {
               //case FileMode.Create:
               //case FileMode.OpenOrCreate:
               //   if (fileExists)
               //      actualErrorCode = Dokan.ERROR_ALREADY_EXISTS;
               //   break;
               //case FileMode.CreateNew:
               //   if (fileExists)
               //      return Dokan.ERROR_FILE_EXISTS;
               //   break;
               case Proxy.OPEN_EXISTING:
               //case FileMode.Append:
               case Proxy.TRUNCATE_EXISTING:
                  {
                     // Stop using exceptions to throw ERROR_FILE_NOT_FOUND
                     FileSystemInfo fileExits = plugin.GetInfo(dokanPath, false);
                     if ((fileExits == null)
                         || !fileExits.Exists
                        )
                     {
                        actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
                        return actualErrorCode;
                     }
                  }
                  break;
            }

            // See http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx#caching_behavior
            if (info.PagingIo)
               rawFlagsAndAttributes |= Proxy.FILE_FLAG_RANDOM_ACCESS;

            // FILE_FLAG_NO_BUFFERING flag requires that all I/O operations on the file handle be in multiples of the sector size, 
            // AND that the I/O buffers also be aligned on addresses which are multiples of the sector size
            if (info.Nocache)
               rawFlagsAndAttributes |= Proxy.FILE_FLAG_WRITE_THROUGH; // | Proxy.FILE_FLAG_NO_BUFFERING;
            if (info.SynchronousIo)
               rawFlagsAndAttributes |= Proxy.FILE_FLAG_SEQUENTIAL_SCAN;

            SafeFileHandle handle = CreateFile(actualLocation, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);

            if (handle.IsInvalid)
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
            FileStreamName fs = new FileStreamName(actualLocation, handle, writeable ? FileAccess.ReadWrite : FileAccess.Read, (int)configDetails.BufferReadSize);
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

      public int OpenDirectory(string dokanPath, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);
            List<string> currentMatchingDirs = plugin.OpenDirectoryLocations(dokanPath);
            if (currentMatchingDirs.Count > 0)
            {
               info.IsDirectory = true;
               dokanError = Dokan.DOKAN_SUCCESS;
            }
            else
            {
               Log.Warn("Probably someone has removed this from the actual mounts.");
               dokanError = Dokan.ERROR_PATH_NOT_FOUND;
            }
         }
         finally
         {
            Log.Trace("OpenDirectory OUT. dokanError[{0}]", dokanError);
         }
         return dokanError;
      }


      public int CreateDirectory(string dokanPath, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;

         try
         {
            // NORMAL mode
            Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
            string path = plugin.CreateLocation(dokanPath);
            if (!Directory.Exists(path))
            {
               Directory.CreateDirectory(path);
            }
            Log.Debug("By the time it gets here the dir should exist, or have existed by another method / thread");
            info.IsDirectory = true;
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
      /// <param name="dokanPath"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int Cleanup(string dokanPath, DokanFileInfo info)
      {
         try
         {
            Log.Trace("Cleanup IN DokanProcessId[{0}] with dokanPath [{1}]", info.ProcessId, dokanPath);
            CloseAndRemove(info);
            if (info.DeleteOnClose)
            {
               if (info.IsDirectory)
               {
                  Log.Trace("DeleteOnClose Directory");
                  List<string> targetDeletes = plugin.OpenDirectoryLocations(dokanPath);
                     // Only delete the directories that this knew about before the delete was called 
                     // (As the user may be moving files into the sources from the mount !!)
                  for (int index = 0; index < targetDeletes.Count; index++)
                  {
                     string fullPath = targetDeletes[index];
                     Log.Trace("Deleting matched dir [{0}]", fullPath);
                     Directory.Delete(fullPath, false);
                  }
                  plugin.DirectoryDeleted(targetDeletes);
                  plugin.DeleteLocation(dokanPath, true);

               }
               else
               {
                  Log.Trace("DeleteOnClose File");
                  File.Delete(plugin.OpenLocation(dokanPath));
                  plugin.FileDeleted(new List<string>( new [] {dokanPath} ) );
                  plugin.DeleteLocation(dokanPath, false);
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

      public int CloseFile(string dokanPath, DokanFileInfo info)
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


      public int ReadFileNative(string dokanPath, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, rawOffset);
            rawReadLength = 0;
            if (info.refFileHandleContext == 0)
            {
               string path = plugin.OpenLocation(dokanPath);
               Log.Warn("No context handle for [" + path + "]");
               fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
            }
            else
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  fileStream = openFiles[info.refFileHandleContext];
            }
            if (rawOffset > fileStream.Length)
            {
               errorCode = Dokan.DOKAN_ERROR;
            }
            else
            {
               fileStream.Seek(rawOffset, SeekOrigin.Begin);
               // readBytes = (uint)fileStream.Read(buffer, 0, buffer.Length);
               if (0 == ReadFile(fileStream.SafeFileHandle, rawBuffer, rawBufferLength, out rawReadLength, IntPtr.Zero))
               {
                  Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
               }
               //else if ( rawReadLength == 0 )
               //{
               //   // ERROR_HANDLE_EOF 38 (0x26)
               //   if (fileStream.Position == fileStream.Length)
               //      errorCode = -38;
               //}
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

      public int WriteFileNative(string dokanPath, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         rawNumberOfBytesWritten = 0;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               FileStreamName fileStream;
               using (openFilesSync.ReadLock())
                  fileStream = openFiles[info.refFileHandleContext];
               if (!info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
                  fileStream.Seek(rawOffset, SeekOrigin.Begin);
               else
                  fileStream.Seek(0, SeekOrigin.End);
               if (0 == WriteFile(fileStream.SafeFileHandle, rawBuffer, rawNumberOfBytesToWrite, out rawNumberOfBytesWritten, IntPtr.Zero))
               {
                  Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
               }
            }
            else
            {
               errorCode = Dokan.ERROR_FILE_NOT_FOUND;
            }
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


      public int FlushFileBuffers(string dokanPath, DokanFileInfo info)
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

      public int GetFileInformation(string dokanPath, ref FileInformation fileinfo, DokanFileInfo info)
      {
         int dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
         try
         {
            Log.Trace("GetFileInformation IN DokanProcessId[{0}]", info.ProcessId);
            FileSystemInfo fsi = plugin.GetInfo(dokanPath, (info.refFileHandleContext != 0));
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
               dokanReturn = Dokan.DOKAN_SUCCESS;
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

      public int FindFilesWithPattern(string dokanPath, string pattern, out FileInformation[] files, DokanFileInfo info)
      {
         return FindFiles(dokanPath, out files, pattern);
      }

      public int FindFiles(string dokanPath, out FileInformation[] files, DokanFileInfo info)
      {
         return FindFiles(dokanPath, out files);
      }

      private int FindFiles(string dokanPath, out FileInformation[] files, string pattern = "*")
      {
         files = null;
         try
         {
            Log.Debug("FindFiles IN [{0}], pattern[{1}]", dokanPath, pattern);
            files = plugin.FindFiles(dokanPath, pattern).Select(info => new FileInformation
                                                                          {
                                                                             // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
                                                                             Attributes = info.Attributes | FileAttributes.NotContentIndexed, 
                                                                             CreationTime = info.CreationTime, 
                                                                             LastAccessTime = info.LastAccessTime, 
                                                                             LastWriteTime = info.LastWriteTime, 
                                                                             Length = ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) ? 0L : ((FileInfo) info).Length, 
                                                                             FileName = info.Name
                                                                          }).ToArray();
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
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileAttributes(string dokanPath, FileAttributes attr, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetFileAttributes IN DokanProcessId[{0}]", info.ProcessId);
            string path = plugin.OpenLocation(dokanPath);
            // This uses  if (!Win32Native.SetFileAttributes(fullPathInternal, (int) fileAttributes))
            // And can throw PathTOOLong
            File.SetAttributes(path, attr);
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

      public int SetFileTimeNative(string dokanPath, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
          ref ComTypes.FILETIME rawLastWriteTime, DokanFileInfo info)
      {
         List<SafeFileHandle> handles = new List<SafeFileHandle>();
         bool needToClose = false;
         try
         {
            Log.Trace("SetFileTime IN DokanProcessId[{0}]", info.ProcessId);
            using (openFilesSync.ReadLock())
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  handles.Add( openFiles[info.refFileHandleContext].SafeFileHandle );
               }
               else
               {
                  // Workaround the dir set
                  // ERROR LiquesceSvc.LiquesceOps: SetFileTime threw:  System.UnauthorizedAccessException: Access to the path 'G:\_backup\Kylie Minogue\Dir1' is denied.
                  // To create a handle to a directory, you have to use FILE_FLAG_BACK_SEMANTICS.
                  string path = plugin.OpenLocation(dokanPath);
                  const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = 0;
                  if (Directory.Exists(path))
                  {
                     rawFlagsAndAttributes = Proxy.FILE_FLAG_BACKUP_SEMANTICS;
                     handles.AddRange(plugin.OpenDirectoryLocations(dokanPath).Select(
                        location => CreateFile(location, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero)
                        ));
                  }
                  else
                  {
                     handles.Add( CreateFile(path, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition,
                                                 rawFlagsAndAttributes, IntPtr.Zero) );
                  }
                  needToClose = true;
               }
               ComTypes.FILETIME lpCreationTime = rawCreationTime;
               ComTypes.FILETIME lpAccessTime = rawLastAccessTime;
               ComTypes.FILETIME lpWriteTime = rawLastWriteTime;
               for (int index = 0; index < handles.Count; index++)
               {
                  SafeFileHandle fileHandle = handles[index];
                  lpCreationTime = rawCreationTime;
                  lpAccessTime = rawLastAccessTime;
                  lpWriteTime = rawLastWriteTime;
                  if ((fileHandle != null)
                      && !fileHandle.IsInvalid
                     )
                  {
                     if (!SetFileTime(fileHandle, ref lpCreationTime, ref lpAccessTime, ref lpWriteTime))
                     {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
                     }
                  }
               }
               // They are passed as ref, so they might have been updated ?
               rawCreationTime = lpCreationTime;
               rawLastAccessTime = lpAccessTime;
               rawLastWriteTime = lpWriteTime;

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
               if (needToClose)
               {
                  foreach (SafeFileHandle fileHandle in
                     handles.Where(fileHandle => (fileHandle != null) && !fileHandle.IsInvalid))
                  {
                     fileHandle.Close();
                  }
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
      /// You should not delete file on DeleteFile or DeleteDirectory.
      // When DeleteFile or DeleteDirectory, you must check whether
      // you can delete or not, and return 0 (when you can delete it)
      // or appropriate error codes such as -ERROR_DIR_NOT_EMPTY,
      // -ERROR_SHARING_VIOLATION.
      // When you return 0 (ERROR_SUCCESS), you get Cleanup with
      // FileInfo->DeleteOnClose set TRUE, you delete the file.
      //
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int DeleteFile(string dokanPath, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            dokanReturn = (File.Exists(plugin.OpenLocation(dokanPath)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
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

      public int DeleteDirectory(string dokanPath, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         string path = plugin.OpenLocation(dokanPath);
         try
         {
            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();
               dokanReturn = (fileInfos.Length > 0) ? Dokan.ERROR_DIR_NOT_EMPTY : Dokan.DOKAN_SUCCESS;
            }
            else
               dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
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


      private void XMoveDirContents(string pathSource, string pathTarget, Dictionary<string, int> hasPathBeenUsed, bool replaceIfExisting)
      {
         Log.Info("XMoveDirContents pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
         DirectoryInfo currentDirectory = new DirectoryInfo(pathSource);
         if (!Directory.Exists(pathTarget))
            Directory.CreateDirectory(pathTarget);
         foreach (FileInfo filein in currentDirectory.GetFiles())
         {
            string fileTarget = pathTarget + Path.DirectorySeparatorChar + filein.Name;
            if (!hasPathBeenUsed.ContainsKey(fileTarget))
            {
               XMoveFile(filein.FullName, fileTarget, replaceIfExisting);
               hasPathBeenUsed[fileTarget] = 1;
            }
            else
            {
               filein.Delete();
            }
         }
         foreach (DirectoryInfo dr in currentDirectory.GetDirectories())
         {
            XMoveDirContents(dr.FullName, pathTarget + Path.DirectorySeparatorChar + dr.Name, hasPathBeenUsed, replaceIfExisting);
         }
         Directory.Delete(pathSource);
      }
      private void XMoveFile(string pathSource, string pathTarget, bool replaceIfExisting)
      {
         // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
         UInt32 dwFlags = (uint)(replaceIfExisting ? 1 : 0);
         // If the file is to be moved to a different volume, the function simulates the move by using the 
         // CopyFile and DeleteFile functions.
         dwFlags += 2; // MOVEFILE_COPY_ALLOWED 

         // The function does not return until the file is actually moved on the disk.
         // Setting this value guarantees that a move performed as a copy and delete operation 
         // is flushed to disk before the function returns. The flush occurs at the end of the copy operation.
         dwFlags += 8; // MOVEFILE_WRITE_THROUGH

         if (!MoveFileEx(pathSource, pathTarget, dwFlags))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
      }


      public int MoveFile(string dokanPath, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         try
         {
            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
            Log.Info("MoveFile replaceIfExisting [{0}] dokanPath: [{1}] newname: [{2}]", replaceIfExisting, dokanPath, newname);
            if (dokanPath == newname)   // This is some weirdness that SyncToy tries to pull !!
               return Dokan.DOKAN_SUCCESS;
            // Work out that if a location already exists then that is the new target and not to scatter it across the other drives !
            string pathTarget = plugin.OpenLocation(newname);
            if (!String.IsNullOrEmpty(pathTarget))
            {
               if (!replaceIfExisting )
                  return Dokan.ERROR_FILE_EXISTS;
            }
            else
               pathTarget = plugin.CreateLocation(newname);

            if (!info.IsDirectory)
            {
               if (info.refFileHandleContext != 0)
               {
                  Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
                  FileStreamName fileHandle = openFiles[info.refFileHandleContext];
                  if (fileHandle != null)
                     fileHandle.Close();
               }
               string pathSource = plugin.OpenLocation(dokanPath);
               if (String.IsNullOrEmpty(pathSource))
                  return Dokan.ERROR_FILE_NOT_FOUND;
               Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
               XMoveFile(pathSource, pathTarget, replaceIfExisting);
            }
            else
            {
               // getting all paths of the source location
               List<string> targetMoves = plugin.OpenDirectoryLocations(dokanPath);
               int count = targetMoves.Count;
               if (count <= 0)
               {
                  Log.Error("MoveFile: Could not find directory [{0}]", dokanPath);
                  return Dokan.ERROR_PATH_NOT_FOUND;
               }
               Dictionary<string, int> hasPathBeenUsed = new Dictionary<string, int>();
               for (int i = count - 1; i >= 0; i--)
               {
                  XMoveDirContents(targetMoves[i], pathTarget, hasPathBeenUsed, replaceIfExisting);
               }
            }
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

      public int SetEndOfFile(string dokanPath, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetEndOfFile IN DokanProcessId[{0}]", info.ProcessId);
            dokanReturn = SetAllocationSize(dokanPath, length, info);
            if (dokanReturn == Dokan.ERROR_FILE_NOT_FOUND)
            {
               string path = plugin.OpenLocation(dokanPath);
               using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
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
            Log.Trace("SetEndOfFile OUT", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetAllocationSize(string dokanPath, long length, DokanFileInfo info)
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

      public int LockFile(string dokanPath, long offset, long length, DokanFileInfo info)
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

      public int UnlockFile(string dokanPath, long offset, long length, DokanFileInfo info)
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
                                                        if (GetDiskFreeSpaceEx(str, out num, out num2, out num3))
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

      public int GetFileSecurityNative(string file, ref SECURITY_INFORMATION rawRequestedInformation, ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         Log.Trace("Unmount IN GetFileSecurity[{0}]", info.ProcessId);
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            string objectPath;
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  objectPath = openFiles[info.refFileHandleContext].Name;
            }
            else
            {
               objectPath = plugin.OpenLocation(file);
            }
            if ( !GetFileSecurity( objectPath, rawRequestedInformation, ref rawSecurityDescriptor, rawSecurityDescriptorLength, ref rawSecurityDescriptorLengthNeeded ) )
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("GetFileSecurity out");
         }
         return dokanReturn;
      }

      public int SetFileSecurityNative(string file, ref SECURITY_INFORMATION rawSecurityInformation, ref SECURITY_DESCRIPTOR rawSecurityDescriptor, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         Log.Trace("Unmount IN SetFileSecurity[{0}]", info.ProcessId);
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            string objectPath;
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  objectPath = openFiles[info.refFileHandleContext].Name;
            }
            else
            {
               objectPath = plugin.OpenLocation(file);
            }
            if ( !SetFileSecurity( objectPath, rawSecurityInformation, ref rawSecurityDescriptor))
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("SetFileSecurity out");
         }
         return dokanReturn;
      }

      #endregion


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
                  bool canWrite = fileStream.CanWrite;
                  using (openFilesSync.WriteLock())
                  {
                     openFiles.Remove(info.refFileHandleContext);
                  }
                  Log.Trace("CloseAndRemove [{0}] info.refFileHandleContext[{1}]", fileStream.Name,
                            info.refFileHandleContext);
                  fileStream.Flush();
                  fileStream.Close();
                  if (canWrite)
                     plugin.FileClosed(fileStream.Name);
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

      #region For the ShareEnabler

      /// <summary>
      /// Will only return tha actual readbytes array size, May be null or zero bytes long
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="buffer"></param>
      /// <param name="requestedReadLength"></param>
      /// <param name="actualReadLength"></param>
      /// <param name="offset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      internal int ReadFile(string dokanPath, out byte[] buffer, int requestedReadLength, out int actualReadLength, long offset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         actualReadLength = 0;
         buffer = null;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, offset);
            if (info.refFileHandleContext == 0)
            {
               string path = plugin.OpenLocation(dokanPath);
               Log.Warn("No context handle for [" + path + "]");
               fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
            }
            else
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  fileStream = openFiles[info.refFileHandleContext];
            }
            fileStream.Seek(offset, SeekOrigin.Begin);
            byte[] internalBuffer = new byte[requestedReadLength];
            actualReadLength = fileStream.Read(internalBuffer, 0, requestedReadLength);
            if (actualReadLength != requestedReadLength)
               Array.Resize(ref internalBuffer, actualReadLength);
            buffer = internalBuffer;
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

      public int WriteFile(string dokanPath, byte[] buffer, long offset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_ERROR;
         bool closeOnReturn = false;
         FileStream fileStream = null;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            if (info.refFileHandleContext == 0)
            {
               string path = plugin.OpenLocation(dokanPath);
               Log.Warn("No context handle for [" + path + "]");
               fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
            }
            else
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
                  fileStream = openFiles[info.refFileHandleContext];
            }
            if (!info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
               fileStream.Seek(offset, SeekOrigin.Begin);
            else
               fileStream.Seek(0, SeekOrigin.End);
            fileStream.Write(buffer, 0, buffer.Length);
            errorCode = Dokan.DOKAN_SUCCESS;
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


      #region DLL Imports
      /// <summary>
      /// The CreateFile function creates or opens a file, file stream, directory, physical disk, volume, console buffer, tape drive,
      /// communications resource, mailslot, or named pipe. The function returns a handle that can be used to access an object.
      /// </summary>
      /// <param name="lpdokanPath"></param>
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
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
      private static extern SafeFileHandle CreateFile(
              string lpdokanPath,
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

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable,
         out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool MoveFileEx(string lpExistingdokanPath, string lpNewdokanPath, UInt32 dwFlags);

      [DllImport("user32.dll", CharSet = CharSet.Auto)]
      private static extern int SendNotifyMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileTime(SafeFileHandle hFile, ref ComTypes.FILETIME lpCreationTime, ref ComTypes.FILETIME lpLastAccessTime, ref ComTypes.FILETIME lpLastWriteTime);

      private enum SE_OBJECT_TYPE
      {
         SE_UNKNOWN_OBJECT_TYPE = 0,
         SE_FILE_OBJECT,
         SE_SERVICE,
         SE_PRINTER,
         SE_REGISTRY_KEY,
         SE_LMSHARE,
         SE_KERNEL_OBJECT,
         SE_WINDOW_OBJECT,
         SE_DS_OBJECT,
         SE_DS_OBJECT_ALL,
         SE_PROVIDER_DEFINED_OBJECT,
         SE_WMIGUID_OBJECT,
         SE_REGISTRY_WOW64_32KEY
      }
      /// <summary>
      /// The GetFileSecurity function obtains specified information about the security of a file or directory. The information obtained is constrained by the caller's access rights and privileges.
      ///	The GetNamedSecurityInfo function provides functionality similar to GetFileSecurity for files as well as other types of objects.
      /// Windows NT 3.51 and earlier:  The GetNamedSecurityInfo function is not supported.
      /// </summary>
      /// <param name="lpdokanPath">[in] Pointer to a null-terminated string that specifies the file or directory for which security information is retrieved.</param>
      /// <param name="requestedInformation">[in] A SecurityInformation value that identifies the security information being requested. </param>
      /// <param name="securityDescriptor">[out] Pointer to a buffer that receives a copy of the security descriptor of the object specified by the lpdokanPath parameter. The calling process must have permission to view the specified aspects of the object's security status. The SECURITY_DESCRIPTOR structure is returned in self-relative format.</param>
      /// <param name="length">[in] Specifies the size, in bytes, of the buffer pointed to by the pSecurityDescriptor parameter.</param>
      /// <param name="lengthNeeded">[out] Pointer to the variable that receives the number of bytes necessary to store the complete security descriptor. If the returned number of bytes is less than or equal to nLength, the entire security descriptor is returned in the output buffer; otherwise, none of the descriptor is returned.</param>
      /// <returns></returns>
      [DllImport("AdvAPI32.DLL", CharSet = CharSet.Auto, SetLastError = true, CallingConvention=CallingConvention.Winapi )]
      private static extern bool GetFileSecurity(string lpdokanPath, SECURITY_INFORMATION requestedInformation, ref SECURITY_DESCRIPTOR pSecurityDescriptor, 
         uint length, ref uint lengthNeeded);

      [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true, CallingConvention=CallingConvention.Winapi )]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool SetFileSecurity( string pdokanPath, SECURITY_INFORMATION SecurityInformation, ref SECURITY_DESCRIPTOR pSecurityDescriptor );

      #endregion


   }

   // This is used for tracking what file is in the store
   // If the code never looks for name, then it might be jitted out
   internal class FileStreamName : FileStream
   {
      public new string Name 
      { 
         get; 
         private set; 
      }

      public FileStreamName(string name, SafeFileHandle handle, FileAccess access, int bufferSize)
         : base(handle, access, bufferSize)
      {
         Name = name;
      }
   }
}