// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using DokanNet;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly ConfigDetails configDetails;
      private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      private readonly Dictionary<UInt64, FileStream> openFiles = new Dictionary<UInt64, FileStream>();

      private readonly Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
      private readonly ReaderWriterLockSlim foundDirectoriesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

      private UInt64 openFilesLastKey;
      private readonly string root;
      // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
      private readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

      public LiquesceOps(ConfigDetails configDetails)
      {
         root = configDetails.SourceLocations[0]; // Already been trimmed in ReadConfigDetails()
         this.configDetails = configDetails;
      }

      #region IDokanOperations Implementation

      /// <summary>
      /// The information given in the Dokan info is a bit misleading about the return codes
      /// This is what the Win OS suystem is expecting http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx
      /// So.. Everything succeeds but the Return code is ERROR_ALREADY_EXISTS
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="fileMode"></param>
      /// <param name="fileAccess"></param>
      /// <param name="fileShare"></param>
      /// <param name="fileOptions"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int CreateFile(string filename, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, FileOptions fileOptions, DokanFileInfo info)
      {
         int actualErrorCode = Dokan.DOKAN_SUCCESS;
         try
         {
            Log.Debug("CreateFile IN filename [{0}], fileMode[{1}], fileAccess[{2}], fileShare[{3}], fileOptions[{4}] DokanProcessId[{5}]",
                        filename, fileMode, fileAccess, fileShare, fileOptions, info.ProcessId);
            string path = GetPath(filename, (fileMode == FileMode.CreateNew) || (fileMode == FileMode.Create));
            if (Directory.Exists(path))
            {
               actualErrorCode = OpenDirectory(filename, info);
               return actualErrorCode;
            }

            // Stop using exceptions to throw ERROR_FILE_NOT_FOUND
            bool fileExists = File.Exists(path);
            switch (fileMode)
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
               case FileMode.Open:
               case FileMode.Append:
               case FileMode.Truncate:
                  if (!fileExists)
                  {
                     Log.Debug("filename [{0}] ERROR_FILE_NOT_FOUND", filename);
                     // Probably someone has removed this on the actual drive
                     RemoveFromLookup(filename);
                     return Dokan.ERROR_FILE_NOT_FOUND;
                  }
                  break;
            }
            //if (!fileExists)
            //{
            //   if (fileAccess == FileAccess.Read)
            //   {
            //      actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
            //   }
            //}

            if (!fileExists
               && (fileAccess != FileAccess.Read)
               )
            {
               // Find Quota
               string newDir = Path.GetDirectoryName(path);
               ulong lpFreeBytesAvailable, lpTotalNumberOfBytes, lpTotalNumberOfFreeBytes;
               // Check to see if the location has enough space 
               if (GetDiskFreeSpaceEx(newDir, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes)
                  && (lpFreeBytesAvailable < configDetails.HoldOffBufferBytes))
               {
                  string newDirLocation = configDetails.SourceLocations.Find(str =>
                    (GetDiskFreeSpaceEx(str, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes)
                           && (lpFreeBytesAvailable > configDetails.HoldOffBufferBytes))
                 );
                  if (!String.IsNullOrEmpty(newDirLocation))
                  {
                     path = newDirLocation + filename;
                     newDir = Path.GetDirectoryName(path);
                  }
                  else
                  {
                     // MessageText: Not enough quota is available to process this command.
                     // #define ERROR_NOT_ENOUGH_QUOTA           1816L
                     Marshal.ThrowExceptionForHR(-1816);

                     //                     return -1816;
                  }
               }
               if (!String.IsNullOrWhiteSpace(newDir))
                  Directory.CreateDirectory(newDir);
            }

            FileStream fs = new FileStream(path, fileMode, fileAccess, fileShare, (int)configDetails.BufferReadSize, fileOptions);
            info.Context = ++openFilesLastKey; // never be Zero !
            try
            {
               openFilesSync.EnterWriteLock();
               openFiles.Add(openFilesLastKey, fs);
            }
            finally
            {
               openFilesSync.ExitWriteLock();
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CreateFile threw: ", ex);
            actualErrorCode = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateFile OUT actualErrorCode=[{0}] context[{1}]", actualErrorCode, openFilesLastKey);
         }
         return actualErrorCode;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);

            List<string> currentMatchingDirs = new List<string>(configDetails.SourceLocations.Count);
            foreach (string newTarget in
                           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
            {
               Log.Trace("Try and find from [{0}]", newTarget);
               if (Directory.Exists(newTarget))
               {
                  currentMatchingDirs.Add(newTarget);
               }
            }
            if (currentMatchingDirs.Count > 0)
            {
               info.IsDirectory = true;
               try
               {
                  foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
                  foundDirectories[filename] = currentMatchingDirs;
               }
               finally
               {
                  foundDirectoriesSync.ExitWriteLock();
               }
               return Dokan.DOKAN_SUCCESS;
            }
            // Probably someone has removed this fromthe actual mounts
            RemoveFromLookup(filename);
            return Dokan.ERROR_PATH_NOT_FOUND;

         }
         finally
         {
            Log.Trace("OpenDirectory OUT");
         }
      }


      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
            string path = GetPath(filename, true);
            if (Directory.Exists(path))
            {
               info.IsDirectory = true;
               dokanError = Dokan.ERROR_ALREADY_EXISTS;
            }
            else if (Directory.CreateDirectory(path).Exists)
            {
               info.IsDirectory = true;
               TrimAndAddUnique(path);
               dokanError = Dokan.DOKAN_SUCCESS;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CreateDirectory threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateDirectory OUT dokanError[{0}]", dokanError);
         }
         return dokanError;
      }
      static bool IsNullOrDefault<T>(T value)
      {
         return object.Equals(value, default(T));
      }

      /*
      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
      If the file system application stored file handle in the Context variable when the function CreateFile is invoked, 
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
            Log.Trace("Cleanup IN DokanProcessId[{0}]", info.ProcessId);
            CloseAndRemove(info);
            if (info.DeleteOnClose)
            {
               if (info.IsDirectory)
               {
                  Log.Trace("DeleteOnClose Directory");
                  try
                  {
                     // Only delete the directories that this knew about before the delet was called 
                     // (As the user may be moving files into the sources from the mount !!)
                     foundDirectoriesSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
                     List<string> targetDeletes = foundDirectories[filename];
                     if (targetDeletes != null)
                        for (int index = 0; index < targetDeletes.Count; index++)
                        {
                           // Use an index for speed (It all counts !)
                           string fullPath = targetDeletes[index];
                           Directory.Delete(fullPath, false);
                        }
                     foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
                     foundDirectories.Remove(filename);
                  }
                  finally
                  {
                     foundDirectoriesSync.ExitUpgradeableReadLock();
                  }
               }
               else
               {
                  Log.Trace("DeleteOnClose File");
                  string path = GetPath(filename);
                  File.Delete(path);
               }
               RemoveFromLookup(filename);
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("Cleanup threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("Cleanup OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      private void RemoveTargetFromLookup(string realFilename)
      {
         try
         {
            rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
            string key = string.Empty;
            foreach (KeyValuePair<string, string> kvp in rootPaths.Where(kvp => kvp.Value == realFilename))
            {
               key = kvp.Key;
               break;
            }
            if (!String.IsNullOrEmpty(key))
            {
               rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
               rootPaths.Remove(key);
            }
         }
         finally
         {
            rootPathsSync.ExitUpgradeableReadLock();
         }
      }

      private void RemoveFromLookup(string filename)
      {
         try
         {
            rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
            rootPaths.Remove(filename);
         }
         finally
         {
            rootPathsSync.ExitWriteLock();
         }
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
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CloseFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CloseFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, offset);
            bool closeOnReturn = false;
            FileStream fileStream;
            UInt64 context = Convert.ToUInt64(info.Context);
            if (IsNullOrDefault(context))
            {
               string path = GetPath(filename);
               Log.Warn("No context handle for [" + path + "]");
               fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, (int)configDetails.BufferReadSize);
               closeOnReturn = true;
            }
            else
            {
               Log.Trace("context [{0}]", context);
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  fileStream = openFiles[context];
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
            }
            if (offset > fileStream.Length)
            {
               readBytes = 0;
               errorCode = Dokan.DOKAN_ERROR;
            }
            else
            {
               fileStream.Seek(offset, SeekOrigin.Begin);
               readBytes = (uint)fileStream.Read(buffer, 0, buffer.Length);
            }
            if (closeOnReturn)
               fileStream.Close();
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("ReadFile threw: ", ex);
            errorCode = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Debug("ReadFile OUT readBytes=[{0}], errorCode[{1}]", readBytes, errorCode);
         }
         return errorCode;
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
      {
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               Log.Trace("context [{0}]", context);
               FileStream fileStream;
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  fileStream = openFiles[context];
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
               fileStream.Seek(offset, SeekOrigin.Begin);
               fileStream.Write(buffer, 0, buffer.Length);
               writtenBytes = (uint)buffer.Length;
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (NotSupportedException ex)
         {
            Log.ErrorException("WriteFile threw: ", ex);
            return Dokan.ERROR_ACCESS_DENIED;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("WriteFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("WriteFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int FlushFileBuffers(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("FlushFileBuffers IN DokanProcessId[{0}]", info.ProcessId);
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               Log.Trace("context [{0}]", context);
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  openFiles[context].Flush();
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("FlushFileBuffers OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
      {
         try
         {
            Log.Trace("GetFileInformation IN DokanProcessId[{0}]", info.ProcessId);
            string path = GetPath(filename);
            if (File.Exists(path))
            {
               FileInfo info2 = new FileInfo(path);
               fileinfo.Attributes = info2.Attributes;
               fileinfo.CreationTime = info2.CreationTime;
               fileinfo.LastAccessTime = info2.LastAccessTime;
               fileinfo.LastWriteTime = info2.LastWriteTime;
               fileinfo.Length = info2.Length;
               return Dokan.DOKAN_SUCCESS;
            }
            if (Directory.Exists(path))
            {
               DirectoryInfo info3 = new DirectoryInfo(path);
               fileinfo.Attributes = info3.Attributes;
               fileinfo.CreationTime = info3.CreationTime;
               fileinfo.LastAccessTime = info3.LastAccessTime;
               fileinfo.LastWriteTime = info3.LastWriteTime;
               fileinfo.Length = 0L;
               return Dokan.DOKAN_SUCCESS;
            }

         }
         finally
         {
            Log.Trace("GetFileInformation OUT Attributes[{0}] Length[{1}]", fileinfo.Attributes, fileinfo.Length);
         }
         return Dokan.DOKAN_ERROR;
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
               if (!configDetails.ShareDetails.Exists(share => share.Path == filename))
               {
                  Log.Debug("Adding a new share for path: {0}", filename);
                  configDetails.ShareDetails.Add(new ShareDetail { Path = filename });
                  if (!Directory.Exists(GetPath(filename)))
                  {
                     Log.Info("Share has not been traversed (Might be command line add");
                     int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);
                     if (lastDir > 0)
                     {
                        Log.Trace("Perform search for path: {0}", filename);
                        string newPart = filename.Substring(lastDir);
                        filename = filename.Substring(0, lastDir);
                     }
                     else
                        filename = PathDirectorySeparatorChar;
                  }
               }
               Log.Debug("Will attempt to find share details for [{0}]", filename);
            }
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
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FindFiles threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            string path = GetPath(filename);
            File.SetAttributes(path, attr);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetFileAttributes threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetFileAttributes OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetFileTime IN DokanProcessId[{0}]", info.ProcessId);
            string path = GetPath(filename);
            FileInfo info2 = new FileInfo(path);
            if (ctime != DateTime.MinValue)
            {
               info2.CreationTime = ctime;
            }
            if (mtime != DateTime.MinValue)
            {
               info2.LastWriteTime = mtime;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetFileTime threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
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
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int DeleteFile(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            dokanReturn = (File.Exists(GetPath(filename)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            DirectoryInfo dirInfo = new DirectoryInfo(GetPath(filename));
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
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteDirectory threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteDirectory OUT dokanReturn[(0}]", dokanReturn);
         }
         return dokanReturn;
      }


      // As this has an order of preference set by the user, and there may be duplicates that will need to be 
      // removed / ignored. There has to be a way of making sure that the (older) duplicate does not overwrite the shared visible
      private void XMoveDirectory(string filename, string pathTarget, bool replaceIfExisting)
      {
         try
         {
            Dictionary<string, int> hasPathBeenUsed = new Dictionary<string, int>();
            foundDirectoriesSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
            List<string> targetMoves = foundDirectories[filename];
            if (targetMoves != null)
               for (int i = targetMoves.Count - 1; i >= 0; i--)
               {
                  XMoveDirContents(targetMoves[i], pathTarget, hasPathBeenUsed, replaceIfExisting);
               }
            foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
            foundDirectories.Remove(filename);
         }
         finally
         {
            foundDirectoriesSync.ExitUpgradeableReadLock();
         }

         //string pathSource = GetPath(filename);
         //Dictionary<string, int> hasPathBeenUsed = new Dictionary<string, int>();
         //while (Directory.Exists(pathSource))
         //{
         //   XMoveDirContents(pathSource, pathTarget, hasPathBeenUsed, replaceIfExisting);
         //   // Remove the above so that it is not found in the next try !
         //   RemoveTargetFromLookup(pathSource);
         //   pathSource = GetPath(filename);
         //}
      }

      private void XMoveDirContents(string pathSource, string pathTarget, Dictionary<string, int> hasPathBeenUsed, bool replaceIfExisting)
      {
         Log.Info("XMoveDirContents pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
         DirectoryInfo currentDirectory = new DirectoryInfo(pathSource);
         if (!Directory.Exists(pathTarget))
            Directory.CreateDirectory(pathTarget);
         foreach (FileInfo filein in currentDirectory.GetFiles())
         {
            string fileTarget = pathTarget + filein.Name;
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
            XMoveDirContents(dr.FullName, pathTarget + dr.Name, hasPathBeenUsed, replaceIfExisting);
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
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
      }


      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         try
         {
            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
            Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);
            string pathTarget = GetPath(newname, true);

            CloseAndRemove(info);

            if (!info.IsDirectory)
            {
               string pathSource = GetPath(filename);
               Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
               XMoveFile(pathSource, pathTarget, replaceIfExisting);
               RemoveTargetFromLookup(pathSource);
            }
            else
            {
               XMoveDirectory(filename, pathTarget, replaceIfExisting);
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("MoveFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
               string path = GetPath(filename);
               using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
               {
                  stream.SetLength(length);
                  stream.Close();
               }
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetEndOfFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               Log.Trace("context [{0}]", context);
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  openFiles[context].SetLength(length);
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetAllocationSize threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               Log.Trace("context [{0}]", context);
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  openFiles[context].Lock(offset, length);
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("LockFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               Log.Trace("context [{0}]", context);
               try
               {
                  openFilesSync.TryEnterReadLock(configDetails.LockTimeout);
                  openFiles[context].Unlock(offset, length);
               }
               finally
               {
                  openFilesSync.ExitReadLock();
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("UnlockFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("UnlockFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
         try
         {
            openFilesSync.EnterWriteLock();
            foreach (FileStream obj2 in openFiles.Values)
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
         finally
         {
            openFilesSync.ExitWriteLock();
         }
         Log.Trace("Unmount out");
         return Dokan.DOKAN_SUCCESS;
      }

      #endregion
      private string GetPath(string filename, bool isCreate = false)
      {
         string foundPath = root;
         try
         {
            if (!String.IsNullOrWhiteSpace(filename) // Win 7 (x64) passes in a blank
               && (filename != PathDirectorySeparatorChar)
               )
            {
               try
               {
                  // Sometimes the windows share put a slash on the end .. Sometimes !!
                  bool isAShareReference = filename.EndsWith(PathDirectorySeparatorChar);
                  //filename = filename.TrimEnd(Path.DirectorySeparatorChar);
                  rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
                  if (!rootPaths.TryGetValue(filename, out foundPath))
                  {
                     bool found = false;
                     if (String.IsNullOrWhiteSpace(filename))
                        throw new ArgumentNullException(filename, "Not allowed to pass this length 2");
                     if (filename[0] != Path.DirectorySeparatorChar)
                        filename = PathDirectorySeparatorChar + filename;

                     if (!isAShareReference
                        && (configDetails.SourceLocations != null)
                        )
                     {
                        foreach (string newTarget in
                           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
                        {
                           Log.Trace("Try and find from [{0}]", newTarget);
                           //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
                           // this has been running, So this ought to try and find if it exists that way.
                           if (Directory.Exists(newTarget)
                              || File.Exists(newTarget)
                              )
                           {
                              TrimAndAddUnique(newTarget);
                              found = rootPaths.TryGetValue(filename, out foundPath);
                              break;
                           }
                        }
                     }
                     else
                     {
                        //found = configDetails.SourceLocations.Exists(delegate(ShareDetail shareDetail)
                        //                                      {
                        //                                         Log.Trace("Try and find from [{0}][{1}]",
                        //                                                   shareDetail.Path, filename);
                        //                                         return
                        //                                            rootPaths.TryGetValue(sourceLocation + filename,
                        //                                                                  out foundPath);
                        //                                      });
                     }
                     if (!found)
                     {
                        Log.Trace("was this a failed redirect thing from a network share ? [{0}]", filename);
                        if (isCreate)
                        {
                           int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);
                           if (lastDir > -1)
                           {
                              Log.Trace("Perform search for path: {0}", filename);
                              string newPart = filename.Substring(lastDir);
                              foundPath = GetPath(filename.Substring(0, lastDir), false) + newPart;
                              Log.Trace("Now make sure it can be found when it tries to repopen via the share");
                              TrimAndAddUnique(foundPath);
                           }
                           else
                              foundPath = root + filename; // This is used when creating new directory / file
                        }
                        else
                           foundPath = root + filename; // This is used when creating new directory / file
                     }
                  }
               }
               finally
               {
                  rootPathsSync.ExitUpgradeableReadLock();
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            Log.Debug("GetPath from [{0}] found [{1}]", filename, foundPath);
         }
         return foundPath;
      }

      private void AddFiles(string path, Dictionary<string, FileInformation> files, string pattern)
      {
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
                                      Attributes = info2.Attributes,
                                      CreationTime = info2.CreationTime,
                                      LastAccessTime = info2.LastAccessTime,
                                      LastWriteTime = info2.LastWriteTime,
                                      Length = (isDirectoy) ? 0L : ((FileInfo)info2).Length,
                                      FileName = info2.Name
                                   };
         files[TrimAndAddUnique(info2.FullName)] = item;
      }

      private string TrimAndAddUnique(string fullFilePath)
      {
         int index = configDetails.SourceLocations.FindIndex(fullFilePath.StartsWith);
         if (index >= 0)
         {
            string key = fullFilePath.Remove(0, configDetails.SourceLocations[index].Length);
            try
            {
               Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
               rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
               // TODO: Add the collisions / duplicate feedback from here
               rootPaths[key] = fullFilePath;
            }
            finally
            {
               rootPathsSync.ExitWriteLock();
            }
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }


      private void CloseAndRemove(DokanFileInfo info)
      {
         UInt64 context = Convert.ToUInt64(info.Context);
         if (!IsNullOrDefault(context))
         {
            Log.Trace("context [{0}]", context);
            FileStream fileStream;
            try
            {
               openFilesSync.EnterWriteLock();
               fileStream = openFiles[context];
               openFiles.Remove(context);
            }
            finally
            {
               openFilesSync.ExitWriteLock();
            }
            Log.Trace("CloseAndRemove [{0}] context[{1}]", fileStream.Name, context);
            fileStream.Flush();
            fileStream.Close();
            info.Context = 0;
         }
      }

      #region DLL Imports
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, UInt32 dwFlags);

      #endregion

      public void InitialiseShares(object state)
      {
         Log.Debug("InitialiseShares IN");
         try
         {
            Thread.Sleep(250); // Give the driver some time to mount
            if (ManagementLayer.Instance.State != LiquesceSvcState.Running)
               return; // A request to exit has occurred
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

            if (configDetails.ShareDetails == null)
               configDetails.ShareDetails = new List<ShareDetail>();
            Log.Info("Now pretension the searches ready for the direct share attach");
            List<RegistryLanManShare> matchedShares = LanManShares.MatchDriveLanManShares(configDetails.DriveLetter + Path.VolumeSeparatorChar);
            foreach (RegistryLanManShare share in matchedShares)
            {
               FileInformation[] files;
               FindFiles(share.Path.Substring(2) + Path.DirectorySeparatorChar, out files);
            }
            // I could do this: "Restart LanmanServer after the drive is mounted",
            // But then that would be painful on the OS and if the Service is just being restarted !
            // BUT - this way means that I do not have to work out what security each of the shares is supposed to have ;-)
            // A BIGGER BUT - "http://liquesce.codeplex.com/workitem/7106"

            ServiceController sc = new ServiceController("Server"); // This name is also used in Win 7
            TimeSpan waitPeriod = new TimeSpan(0, 0, 0, 2);
            foreach (ServiceController scDepends in sc.DependentServices)
            {
               try
               {
                  Log.Info("Attempting to stop " + scDepends.ServiceName);
                  scDepends.Stop();
                  scDepends.WaitForStatus(ServiceControllerStatus.Stopped, waitPeriod);
               }
               catch (Exception ex)
               {
                  Log.WarnException("Unable to stop Service", ex);
               }
            }
            try
            {
               Log.Info("Attempting to stop " + sc.ServiceName);
               sc.Stop();
               sc.WaitForStatus(ServiceControllerStatus.Stopped, waitPeriod);
               Thread.Sleep(250); //Have to keep these tight, as the Computer Browser service can be triggered from the OS as well.
            }
            catch (Exception ex)
            {
               Log.WarnException("Unable to stop Service", ex);
            }
            try
            {
               Log.Info("Attempting to start " + sc.ServiceName);
               sc.Start();
               sc.WaitForStatus(ServiceControllerStatus.Running, waitPeriod);
               Thread.Sleep(250);
            }
            catch (Exception ex)
            {
               Log.ErrorException("Unable to start Service", ex);
            }
            foreach (ServiceController scDepends in sc.DependentServices)
            {
               if ((scDepends.Status != ServiceControllerStatus.Running)
                  && (scDepends.Status != ServiceControllerStatus.StartPending)
                  )
               {
                  try
                  {
                     Log.Info("Attempting to start " + scDepends.ServiceName);
                     scDepends.Start();
                     scDepends.WaitForStatus(ServiceControllerStatus.Running, waitPeriod);
                  }
                  catch (Exception ex)
                  {
                     Log.WarnException("Above service reported this on start:", ex);
                     ManagementLayer.Instance.FireStateChange(LiquesceSvcState.InWarning, scDepends.ServiceName + " reports: " + ex.Message);
                  }
               }
            }
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
   }
}