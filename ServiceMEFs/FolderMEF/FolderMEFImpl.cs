using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using LiquesceFacade;
using LiquesceSvcMEF;
using NLog;

namespace FolderMEF
{
   [Export(typeof(IServicePlugin))]
   public class FolderMEFImpl : CommonStorage, IServicePlugin
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();


      #region Implementation of ILocations

      /// <summary>
      /// Return location of the new file, does not open it
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>.</returns>
      public string CreateLocation(string dokanPath)
      {
         string location = String.Empty;
         try
         {
            Log.Debug("CreateLocation([{0}])", dokanPath);
         }
         catch (Exception ex)
         {
            Log.ErrorException("Create", ex);
            location = GetPath(dokanPath, true);
         }
         finally
         {
            Log.Debug("Create returning([{0}])", location);
         }
         return location;
      }

      /// <summary>
      /// Return location an existing file
      /// </summary>
      /// <param name="path">DokanPath passed in</param>
      /// <returns>.</returns>
      public override string OpenLocation(string path)
      {
         throw new NotImplementedException();
      }


      #endregion

      #region Implementation of IFileEventHandlers

      /// <summary>
      /// To be used after a file has been updated and closed.
      /// Can be used to create the directory tree as well
      /// </summary>
      /// <param name="actualLocations"></param>
      public void FileClosed(List<string> actualLocations)
      {
      }

      /// <summary>
      /// To be used after a file has been updated and closed.
      /// </summary>
      /// <param name="actualLocation"></param>
      public void FileClosed(string actualLocation)
      {
      }

      /// <summary>
      /// A file has been removed from the system
      /// </summary>
      /// <param name="dokanPath"></param>
      public void FileDeleted(List<string> dokanPath)
      {
      }

      /// <summary>
      /// When a directory is deleted (i.e. is empty), this will be called
      /// </summary>
      /// <param name="actualLocations"></param>
      public void DirectoryDeleted(List<string> actualLocations)
      {
      }

      #endregion

      #region Location stuff
      private string GetPath(string dokanPath, bool isDirectory, bool isCreate = false)
      {
         string foundPath = root;
         try
         {
            if (!String.IsNullOrWhiteSpace(dokanPath) // Win 7 (x64) passes in a blank
               && (dokanPath != PathDirectorySeparatorChar)
               )
            {
               bool isAShare = false;
               if (!dokanPath.StartsWith(PathDirectorySeparatorChar))
               {
                  isAShare = true;
                  dokanPath = Path.DirectorySeparatorChar + dokanPath;
               }
               if (dokanPath.EndsWith(PathDirectorySeparatorChar))
                  isDirectory = isAShare = true;

               dokanPath = dokanPath.TrimEnd(Path.DirectorySeparatorChar);
               using (rootPathsSync.UpgradableReadLock())
               {
                  if (!rootPaths.TryGetValue(dokanPath, out foundPath))
                  {
                     bool found = false;
                     if (String.IsNullOrWhiteSpace(dokanPath))
                        throw new ArgumentNullException(dokanPath, "Not allowed to pass this length 2");
                     if (dokanPath[0] != Path.DirectorySeparatorChar)
                        dokanPath = PathDirectorySeparatorChar + dokanPath;

                     if (!isAShare
                        && (sourceLocations != null)
                        )
                     {
                        foreach (string newTarget in
                           sourceLocations.Select(sourceLocation => sourceLocation + dokanPath))
                        {
                           Log.Trace("Try and GetPath from [{0}]", newTarget);
                           //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
                           // this has been running, So this ought to try and find if it exists that way.
                           if (Directory.Exists(newTarget)
                              || File.Exists(newTarget)
                              )
                           {
                              TrimAndAddUnique(newTarget);
                              found = rootPaths.TryGetValue(dokanPath, out foundPath);
                              break;
                           }
                        }
                     }
                     else if (isAShare
                        && (knownSharePaths != null)
                        )
                     {
                        found = knownSharePaths.Exists(delegate(string sharePath)
                                                              {
                                                                 Log.Trace("Try and find from [{0}][{1}]", sharePath, dokanPath);
                                                                 return rootPaths.TryGetValue(sharePath + dokanPath, out foundPath);
                                                              });

                     }
                     if (!found)
                     {
                        Log.Trace("was this a failed redirect thing from a network share ? [{0}]", dokanPath);
                        //if (isCreate)
                        //{
                        //   int lastDir = dokanPath.LastIndexOf(Path.DirectorySeparatorChar);
                        //   if (lastDir > -1)
                        //   {
                        //      Log.Trace("Perform search for path: {0}", dokanPath);
                        //      string newPart = dokanPath.Substring(lastDir);
                        //      foundPath = GetPath(dokanPath.Substring(0, lastDir), false) + newPart;
                        //      Log.Trace("Now make sure it can be found when it tries to repopen via the share");
                        //      TrimAndAddUnique(foundPath);
                        //   }
                        //   else
                        //      foundPath = root + dokanPath; // This is used when creating new directory / file
                        //}
                        //else
                           foundPath = root + dokanPath; // This is used when creating new directory / file
                     }
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            Log.Debug("GetPath from [{0}] found [{1}]", dokanPath, foundPath);
         }
         return foundPath;
      }

      #endregion


      #region Code to be shared

      #endregion

      #region Implementation of IMoveManager

      /// <summary>
      /// Move directories depends on the scatter pattern beig used by the plugin.
      /// Therefore if a priority is implemented, then it could be that some files from a remote part are
      /// being collasced into a single location, but that location may already exist
      /// There are other difficult scenrios that each of the plugins will need to solve.
      /// When they have done, they must inform the other plugin's of their actions.
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="dokanTarget"></param>
      /// <param name="replaceIfExisting"></param>
      /// <param name="actualFileNewLocations"></param>
      /// <param name="actualFileDeleteLocations"></param>
      /// <param name="actualDirectoryDeleteLocations"></param>
      public void MoveDirectory(string dokanPath, string dokanTarget, bool replaceIfExisting, out List<string> actualFileNewLocations, out List<string> actualFileDeleteLocations, out List<string> actualDirectoryDeleteLocations)
      {
         DeleteLocation(dokanPath, true);
         throw new NotImplementedException();
      }

      #endregion
   }
}


//namespace LiquesceSvc
//{
//   internal class LiquesceOps : IDokanOperations
//   {
//      static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
//      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
//      private readonly ConfigDetails configDetails;
//      private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
//      private readonly Dictionary<UInt64, FileStream> openFiles = new Dictionary<UInt64, FileStream>();

//      private readonly Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
//      private readonly ReaderWriterLockSlim foundDirectoriesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

//      private UInt64 openFilesLastKey;
//      private readonly string root;
//      // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
//      private readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
//      private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

//      public LiquesceOps(ConfigDetails configDetails)
//      {
//         root = configDetails.SourceLocations[0]; // Already been trimmed in ReadConfigDetails()
//         this.configDetails = configDetails;
//      }

//      #region IDokanOperations Implementation

//      /// <summary>
//      /// The information given in the Dokan info is a bit misleading about the return codes
//      /// This is what the Win OS suystem is expecting http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx
//      /// So.. Everything succeeds but the Return code is ERROR_ALREADY_EXISTS
//      /// </summary>
//      /// <param name="filename"></param>
//      /// <param name="rawFlagsAndAttributes"></param>
//      /// <param name="info"></param>
//      /// <param name="rawAccessMode"></param>
//      /// <param name="rawShare"></param>
//      /// <param name="rawCreationDisposition"></param>
//      /// <returns></returns>
//      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info)
//      {
//         int actualErrorCode = Dokan.DOKAN_SUCCESS;
//         try
//         {
//            Log.Debug(
//               "CreateFile IN filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}], ProcessId[{5}]",
//               filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info.ProcessId);
//            string path = GetPath(filename, (rawCreationDisposition == Proxy.CREATE_NEW) || (rawCreationDisposition == Proxy.CREATE_ALWAYS));

//            if (Directory.Exists(path))
//            {
//               actualErrorCode = OpenDirectory(filename, info);
//               return actualErrorCode;
//            }

//            // Stop using exceptions to throw ERROR_FILE_NOT_FOUND
//            bool fileExists = File.Exists(path);
//            switch (rawCreationDisposition)
//            {
//               //case FileMode.Create:
//               //case FileMode.OpenOrCreate:
//               //   if (fileExists)
//               //      actualErrorCode = Dokan.ERROR_ALREADY_EXISTS;
//               //   break;
//               //case FileMode.CreateNew:
//               //   if (fileExists)
//               //      return Dokan.ERROR_FILE_EXISTS;
//               //   break;
//               case Proxy.OPEN_EXISTING:
//               //case FileMode.Append:
//               case Proxy.TRUNCATE_EXISTING:
//                  if (!fileExists)
//                  {
//                     Log.Debug("filename [{0}] ERROR_FILE_NOT_FOUND", filename);
//                     // Probably someone has removed this on the actual drive
//                     RemoveFromLookup(filename);
//                     actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
//                     return actualErrorCode;
//                  }
//                  break;
//            }
//            //if (!fileExists)
//            //{
//            //   if (fileAccess == FileAccess.Read)
//            //   {
//            //      actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
//            //   }
//            //}

//            bool writeable = (((rawAccessMode & Proxy.FILE_WRITE_DATA) == Proxy.FILE_WRITE_DATA));
//            if (!fileExists
//               && writeable
//               )
//            {
//               // Find Quota
//               string newDir = Path.GetDirectoryName(path);
//               ulong lpFreeBytesAvailable, lpTotalNumberOfBytes, lpTotalNumberOfFreeBytes;
//               // Check to see if the location has enough space 
//               if (GetDiskFreeSpaceEx(newDir, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes)
//                  && (lpFreeBytesAvailable < configDetails.HoldOffBufferBytes))
//               {
//                  string newDirLocation = configDetails.SourceLocations.Find(str =>
//                    (GetDiskFreeSpaceEx(str, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes)
//                           && (lpFreeBytesAvailable > configDetails.HoldOffBufferBytes))
//                 );
//                  if (!String.IsNullOrEmpty(newDirLocation))
//                  {
//                     path = newDirLocation + filename;
//                     newDir = Path.GetDirectoryName(path);
//                  }
//                  else
//                  {
//                     // MessageText: Not enough quota is available to process this command.
//                     // #define ERROR_NOT_ENOUGH_QUOTA           1816L 

//                     // unchecked stolen from Microsoft.Win32.Win32Native.MakeHRFromErrorCode
//                     Marshal.ThrowExceptionForHR(unchecked(((int)2147942400u) | 1816), new IntPtr(-1));
//                     // The above function justs make the whole exception stack dissappear up it's own pipe !!
//                     // _BUT_ Sticking the new IntPtr(-1) forces a new IErrorInfo and stops it reusing the
//                     // last one it auto-created !
//                  }
//               }
//               if (!String.IsNullOrWhiteSpace(newDir))
//                  Directory.CreateDirectory(newDir);
//            }

//            SafeFileHandle handle = CreateFile(path, rawAccessMode, rawShare, IntPtr.Zero, rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);
//            if (handle.IsInvalid)
//            {
//               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
//            }
//            FileStream fs = new FileStream(handle, writeable ? FileAccess.ReadWrite : FileAccess.Read, (int)configDetails.BufferReadSize);

//            info.Context = ++openFilesLastKey; // never be Zero !
//            try
//            {
//               openFilesSync.EnterWriteLock();
//               openFiles.Add(openFilesLastKey, fs);
//            }
//            finally
//            {
//               openFilesSync.ExitWriteLock();
//            }
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("CreateFile threw: ", ex);
//            actualErrorCode = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("CreateFile OUT actualErrorCode=[{0}] context[{1}]", actualErrorCode, openFilesLastKey);
//         }
//         return actualErrorCode;
//      }

//      public int OpenDirectory(string filename, DokanFileInfo info)
//      {
//         int dokanError = Dokan.DOKAN_ERROR;
//         try
//         {
//            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);
//            string path = GetPath(filename);
//            int start = path.IndexOf(Path.DirectorySeparatorChar );
//            path = start > 0 ? path.Substring(start) : PathDirectorySeparatorChar;

//            List<string> currentMatchingDirs = new List<string>(configDetails.SourceLocations.Count);
//            foreach (string newTarget in
//                           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + path))
//            {
//               Log.Trace("Try and OpenDirectory from [{0}]", newTarget);
//               if (Directory.Exists(newTarget))
//               {
//                  Log.Trace("Directory.Exists[{0}] Adding details", newTarget);
//                  currentMatchingDirs.Add(newTarget);
//               }
//            }
//            if (currentMatchingDirs.Count > 0)
//            {
//               info.IsDirectory = true;
//               try
//               {
//                  foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
//                  foundDirectories[filename] = currentMatchingDirs;
//               }
//               finally
//               {
//                  foundDirectoriesSync.ExitWriteLock();
//               }
//               dokanError = Dokan.DOKAN_SUCCESS;
//            }
//            else
//            {
//               Log.Warn("Probably someone has removed this from the actual mounts.");
//               RemoveFromLookup(filename);
//               dokanError = Dokan.ERROR_PATH_NOT_FOUND;
//            }

//         }
//         finally
//         {
//            Log.Trace("OpenDirectory OUT. dokanError[{0}]", dokanError);
//         }
//         return dokanError;
//      }


//      public int CreateDirectory(string filename, DokanFileInfo info)
//      {
//         int dokanError = Dokan.DOKAN_ERROR;
//         try
//         {
//            Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
//            string path = GetPath(filename, true);
//            if (Directory.Exists(path))
//            {
//               info.IsDirectory = true;
//               dokanError = Dokan.ERROR_ALREADY_EXISTS;
//            }
//            else if (Directory.CreateDirectory(path).Exists)
//            {
//               info.IsDirectory = true;
//               TrimAndAddUnique(path);
//               dokanError = Dokan.DOKAN_SUCCESS;
//            }
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("CreateDirectory threw: ", ex);
//            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("CreateDirectory OUT dokanError[{0}]", dokanError);
//         }
//         return dokanError;
//      }
//      static bool IsNullOrDefault<T>(T value)
//      {
//         return object.Equals(value, default(T));
//      }

//      /*
//      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
//      If the file system application stored file handle in the Context variable when the function CreateFile is invoked, 
//      this should be closed in the Cleanup function, not in CloseFile function. If the user application calls CloseHandle
//      and subsequently open the same file, the CloseFile function of the file system application may not be invoked 
//      before the CreateFile API is called. This may cause sharing violation error. 
//      Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
//      complete the I/O operations. The file system application should also properly work in this case.
//      */
//      /// <summary>
//      /// When info->DeleteOnClose is true, you must delete the file in Cleanup.
//      /// </summary>
//      /// <param name="filename"></param>
//      /// <param name="info"></param>
//      /// <returns></returns>
//      public int Cleanup(string filename, DokanFileInfo info)
//      {
//         try
//         {
//            Log.Trace("Cleanup IN DokanProcessId[{0}]", info.ProcessId);
//            CloseAndRemove(info);
//            if (info.DeleteOnClose)
//            {
//               if (info.IsDirectory)
//               {
//                  Log.Trace("DeleteOnClose Directory");
//                  try
//                  {
//                     // Only delete the directories that this knew about before the delet was called 
//                     // (As the user may be moving files into the sources from the mount !!)
//                     foundDirectoriesSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
//                     List<string> targetDeletes = foundDirectories[filename];
//                     if (targetDeletes != null)
//                        for (int index = 0; index < targetDeletes.Count; index++)
//                        {
//                           // Use an index for speed (It all counts !)
//                           string fullPath = targetDeletes[index];
//                           Log.Trace("Deleting matched dir [{0}]", fullPath);
//                           Directory.Delete(fullPath, false);
//                        }
//                     foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
//                     foundDirectories.Remove(filename);
//                  }
//                  finally
//                  {
//                     foundDirectoriesSync.ExitUpgradeableReadLock();
//                  }
//               }
//               else
//               {
//                  Log.Trace("DeleteOnClose File");
//                  string path = GetPath(filename);
//                  File.Delete(path);
//               }
//               RemoveFromLookup(filename);
//            }
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("Cleanup threw: ", ex);
//            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("Cleanup OUT");
//         }
//         return Dokan.DOKAN_SUCCESS;
//      }

//      private void RemoveTargetFromLookup(string realFilename)
//      {
//         try
//         {
//            rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
//            string key = string.Empty;
//            foreach (KeyValuePair<string, string> kvp in rootPaths.Where(kvp => kvp.Value == realFilename))
//            {
//               key = kvp.Key;
//               break;
//            }
//            if (!String.IsNullOrEmpty(key))
//            {
//               rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
//               rootPaths.Remove(key);
//            }
//         }
//         finally
//         {
//            rootPathsSync.ExitUpgradeableReadLock();
//         }
//      }

//      private void RemoveFromLookup(string filename)
//      {
//         try
//         {
//            rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
//            rootPaths.Remove(filename);
//         }
//         finally
//         {
//            rootPathsSync.ExitWriteLock();
//         }
//      }

//      public int CloseFile(string filename, DokanFileInfo info)
//      {
//         try
//         {
//            Log.Trace("CloseFile IN DokanProcessId[{0}]", info.ProcessId);
//            CloseAndRemove(info);
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("CloseFile threw: ", ex);
//            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("CloseFile OUT");
//         }
//         return Dokan.DOKAN_SUCCESS;
//      }
















//      /// <summary>
//      /// You should not delete file on DeleteFile or DeleteDirectory.
//      // When DeleteFile or DeleteDirectory, you must check whether
//      // you can delete or not, and return 0 (when you can delete it)
//      // or appropriate error codes such as -ERROR_DIR_NOT_EMPTY,
//      // -ERROR_SHARING_VIOLATION.
//      // When you return 0 (ERROR_SUCCESS), you get Cleanup with
//      // FileInfo->DeleteOnClose set TRUE, you delete the file.
//      //
//      /// </summary>
//      /// <param name="filename"></param>
//      /// <param name="info"></param>
//      /// <returns></returns>
//      public int DeleteFile(string filename, DokanFileInfo info)
//      {
//         int dokanReturn = Dokan.DOKAN_ERROR;
//         try
//         {
//            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
//            dokanReturn = (File.Exists(GetPath(filename)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("DeleteFile threw: ", ex);
//            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("DeleteFile OUT dokanReturn[(0}]", dokanReturn);
//         }
//         return dokanReturn;
//      }

//      public int DeleteDirectory(string filename, DokanFileInfo info)
//      {
//         int dokanReturn = Dokan.DOKAN_ERROR;
//         try
//         {
//            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
//            DirectoryInfo dirInfo = new DirectoryInfo(GetPath(filename));
//            if (dirInfo.Exists)
//            {
//               FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();
//               dokanReturn = (fileInfos.Length > 0) ? Dokan.ERROR_DIR_NOT_EMPTY : Dokan.DOKAN_SUCCESS;
//            }
//            else
//               dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("DeleteDirectory threw: ", ex);
//            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("DeleteDirectory OUT dokanReturn[(0}]", dokanReturn);
//         }
//         return dokanReturn;
//      }


//      // As this has an order of preference set by the user, and there may be duplicates that will need to be 
//      // removed / ignored. There has to be a way of making sure that the (older) duplicate does not overwrite the shared visible
//      private void XMoveDirectory(string filename, string pathTarget, bool replaceIfExisting)
//      {
//         try
//         {
//            Dictionary<string, int> hasPathBeenUsed = new Dictionary<string, int>();
//            foundDirectoriesSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
//            List<string> targetMoves = foundDirectories[filename];
//            if (targetMoves != null)
//               for (int i = targetMoves.Count - 1; i >= 0; i--)
//               {
//                  XMoveDirContents(targetMoves[i], pathTarget, hasPathBeenUsed, replaceIfExisting);
//               }
//            foundDirectoriesSync.TryEnterWriteLock(configDetails.LockTimeout);
//            foundDirectories.Remove(filename);
//         }
//         finally
//         {
//            foundDirectoriesSync.ExitUpgradeableReadLock();
//         }

//         //string pathSource = GetPath(filename);
//         //Dictionary<string, int> hasPathBeenUsed = new Dictionary<string, int>();
//         //while (Directory.Exists(pathSource))
//         //{
//         //   XMoveDirContents(pathSource, pathTarget, hasPathBeenUsed, replaceIfExisting);
//         //   // Remove the above so that it is not found in the next try !
//         //   RemoveTargetFromLookup(pathSource);
//         //   pathSource = GetPath(filename);
//         //}
//      }

//      private void XMoveDirContents(string pathSource, string pathTarget, Dictionary<string, int> hasPathBeenUsed, bool replaceIfExisting)
//      {
//         Log.Info("XMoveDirContents pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
//         DirectoryInfo currentDirectory = new DirectoryInfo(pathSource);
//         if (!Directory.Exists(pathTarget))
//            Directory.CreateDirectory(pathTarget);
//         foreach (FileInfo filein in currentDirectory.GetFiles())
//         {
//            string fileTarget = pathTarget + Path.DirectorySeparatorChar + filein.Name;
//            if (!hasPathBeenUsed.ContainsKey(fileTarget))
//            {
//               XMoveFile(filein.FullName, fileTarget, replaceIfExisting);
//               hasPathBeenUsed[fileTarget] = 1;
//            }
//            else
//            {
//               filein.Delete();
//            }
//         }
//         foreach (DirectoryInfo dr in currentDirectory.GetDirectories())
//         {
//             XMoveDirContents(dr.FullName, pathTarget + Path.DirectorySeparatorChar + dr.Name, hasPathBeenUsed, replaceIfExisting);
//         }
//         Directory.Delete(pathSource);
//      }

//      private void XMoveFile(string pathSource, string pathTarget, bool replaceIfExisting)
//      {
//         // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
//         UInt32 dwFlags = (uint)(replaceIfExisting ? 1 : 0);
//         // If the file is to be moved to a different volume, the function simulates the move by using the 
//         // CopyFile and DeleteFile functions.
//         dwFlags += 2; // MOVEFILE_COPY_ALLOWED 

//         // The function does not return until the file is actually moved on the disk.
//         // Setting this value guarantees that a move performed as a copy and delete operation 
//         // is flushed to disk before the function returns. The flush occurs at the end of the copy operation.
//         dwFlags += 8; // MOVEFILE_WRITE_THROUGH

//         if (!MoveFileEx(pathSource, pathTarget, dwFlags))
//            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
//      }


//      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
//      {
//         try
//         {
//            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
//            Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);
//            string pathTarget = GetPath(newname, true);

//            CloseAndRemove(info);

//            if (!info.IsDirectory)
//            {
//               string pathSource = GetPath(filename);
//               Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
//               XMoveFile(pathSource, pathTarget, replaceIfExisting);
//               RemoveTargetFromLookup(pathSource);
//            }
//            else
//            {
//               XMoveDirectory(filename, pathTarget, replaceIfExisting);
//            }
//         }
//         catch (Exception ex)
//         {
//            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
//            Log.ErrorException("MoveFile threw: ", ex);
//            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
//         }
//         finally
//         {
//            Log.Trace("MoveFile OUT");
//         }
//         return Dokan.DOKAN_SUCCESS;
//      }


//      #endregion
//      private string GetPath(string filename, bool isCreate = false)
//      {
//         string foundPath = root;
//         try
//         {
//            if (!String.IsNullOrWhiteSpace(filename) // Win 7 (x64) passes in a blank
//               && (filename != PathDirectorySeparatorChar)
//               )
//            {
//               try
//               {
//                  bool isAShare = false;
//                  if (!filename.StartsWith(PathDirectorySeparatorChar))
//                  {
//                     isAShare = true;
//                     filename = Path.DirectorySeparatorChar + filename;
//                  }
//                  if (filename.EndsWith(PathDirectorySeparatorChar))
//                     isAShare = true;

//                  filename = filename.TrimEnd(Path.DirectorySeparatorChar);
//                  rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
//                  if (!rootPaths.TryGetValue(filename, out foundPath))
//                  {
//                     bool found = false;
//                     if (String.IsNullOrWhiteSpace(filename))
//                        throw new ArgumentNullException(filename, "Not allowed to pass this length 2");
//                     if (filename[0] != Path.DirectorySeparatorChar)
//                        filename = PathDirectorySeparatorChar + filename;

//                     if (!isAShare
//                        && (configDetails.SourceLocations != null)
//                        )
//                     {
//                        foreach (string newTarget in
//                           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
//                        {
//                           Log.Trace("Try and GetPath from [{0}]", newTarget);
//                           //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
//                           // this has been running, So this ought to try and find if it exists that way.
//                           if (Directory.Exists(newTarget)
//                              || File.Exists(newTarget)
//                              )
//                           {
//                              TrimAndAddUnique(newTarget);
//                              found = rootPaths.TryGetValue(filename, out foundPath);
//                              break;
//                           }
//                        }
//                     }
//                     else if ( isAShare
//                        && (configDetails.KnownSharePaths != null)
//                        )
//                     {
//                        found = configDetails.KnownSharePaths.Exists(delegate(string sharePath)
//                                                              {
//                                                                 Log.Trace("Try and find from [{0}][{1}]", sharePath, filename);
//                                                                 return rootPaths.TryGetValue(sharePath + filename, out foundPath);
//                                                              });

//                     }
//                     if (!found)
//                     {
//                        Log.Trace("was this a failed redirect thing from a network share ? [{0}]", filename);
//                        if (isCreate)
//                        {
//                           int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);
//                           if (lastDir > -1)
//                           {
//                              Log.Trace("Perform search for path: {0}", filename);
//                              string newPart = filename.Substring(lastDir);
//                              foundPath = GetPath(filename.Substring(0, lastDir), false) + newPart;
//                              Log.Trace("Now make sure it can be found when it tries to repopen via the share");
//                              TrimAndAddUnique(foundPath);
//                           }
//                           else
//                              foundPath = root + filename; // This is used when creating new directory / file
//                        }
//                        else
//                           foundPath = root + filename; // This is used when creating new directory / file
//                     }
//                  }
//               }
//               finally
//               {
//                  rootPathsSync.ExitUpgradeableReadLock();
//               }
//            }
//         }
//         catch (Exception ex)
//         {
//            Log.ErrorException("GetPath threw: ", ex);
//         }
//         finally
//         {
//            Log.Debug("GetPath from [{0}] found [{1}]", filename, foundPath);
//         }
//         return foundPath;
//      }

//      private void AddFiles(string path, Dictionary<string, FileInformation> files, string pattern)
//      {
//         try
//         {
//            DirectoryInfo dirInfo = new DirectoryInfo(path);
//            if (dirInfo.Exists)
//            {
//               FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
//               foreach (FileSystemInfo info2 in fileSystemInfos)
//               {
//                  AddToUniqueLookup(info2, files);
//               }
//            }
//         }
//         catch (Exception ex)
//         {
//            Log.ErrorException("AddFiles threw: ", ex);
//         }
//      }

//      private void AddToUniqueLookup(FileSystemInfo info2, Dictionary<string, FileInformation> files)
//      {
//         bool isDirectoy = (info2.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
//         FileInformation item = new FileInformation
//                                   {
//                                      // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
//                                      // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
//                                      Attributes = info2.Attributes | FileAttributes.NotContentIndexed,
//                                      CreationTime = info2.CreationTime,
//                                      LastAccessTime = info2.LastAccessTime,
//                                      LastWriteTime = info2.LastWriteTime,
//                                      Length = (isDirectoy) ? 0L : ((FileInfo)info2).Length,
//                                      FileName = info2.Name
//                                   };
//         if (Log.IsTraceEnabled)
//            item.Attributes |= FileAttributes.Offline;
//         files[TrimAndAddUnique(info2.FullName)] = item;
//      }

//      private string TrimAndAddUnique(string fullFilePath)
//      {
//         int index = configDetails.SourceLocations.FindIndex(fullFilePath.StartsWith);
//         if (index >= 0)
//         {
//            string key = fullFilePath.Remove(0, configDetails.SourceLocations[index].Length);
//            try
//            {
//               Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
//               rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
//               // TODO: Add the collisions / duplicate feedback from here
//               rootPaths[key] = fullFilePath;
//            }
//            finally
//            {
//               rootPathsSync.ExitWriteLock();
//            }
//            return key;
//         }
//         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
//      }


//      private void CloseAndRemove(DokanFileInfo info)
//      {
//         UInt64 context = Convert.ToUInt64(info.Context);
//         if (!IsNullOrDefault(context))
//         {
//            Log.Trace("context [{0}]", context);
//            FileStream fileStream;
//            try
//            {
//               openFilesSync.EnterWriteLock();
//               fileStream = openFiles[context];
//               openFiles.Remove(context);
//            }
//            finally
//            {
//               openFilesSync.ExitWriteLock();
//            }
//            Log.Trace("CloseAndRemove [{0}] context[{1}]", fileStream.Name, context);
//            fileStream.Flush();
//            fileStream.Close();
//            info.Context = 0;
//         }
//      }

//      #region DLL Imports

//      [DllImport("kernel32.dll", SetLastError = true)]
//      private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, UInt32 dwFlags);

//      #endregion

//      public void InitialiseShares(object state)
//      {
//         Log.Debug("InitialiseShares IN");
//         try
//         {
//            Thread.Sleep(250); // Give the driver some time to mount
//            // Now check (in 2 phases) the existence of the drive
//            string path = configDetails.DriveLetter + ":" + PathDirectorySeparatorChar;
//            while (!Directory.Exists(path))
//            {
//               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares");
//               Thread.Sleep(1000);
//            }
//            // 2nd phase as the above is supposed to be cheap but can return false +ves
//            do
//            {
//               string[] drives = Environment.GetLogicalDrives();
//               if (Array.Exists(drives, dr => dr.Remove(1) == configDetails.DriveLetter))
//                  break;
//               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares (Phase 2)");
//               Thread.Sleep(100);
//            } while (ManagementLayer.Instance.State == LiquesceSvcState.Running);

//            configDetails.KnownSharePaths = new List<string>(configDetails.SharesToRestore.Count);
//            foreach (LanManShareDetails shareDetails in configDetails.SharesToRestore)
//            {
//               configDetails.KnownSharePaths.Add(shareDetails.Path);
//               try
//               {
//                  Log.Info("Restore share for : [{0}] [{1} : {2}]", shareDetails.Path, shareDetails.Name, shareDetails.Description);
//                  LanManShareHandler.SetLanManShare(shareDetails);
//               }
//               catch (Exception ex)
//               {
//                  Log.ErrorException("Unable to restore share for : " + shareDetails.Path, ex);
//               }
//            }
//            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.Running, "Shares restored - good to go");
//         }
//         catch (Exception ex)
//         {
//            Log.ErrorException("Init shares threw: ", ex);
//            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.InError, "Init shares reports: " + ex.Message);
//         }
//         finally
//         {
//            Log.Debug("InitialiseShares OUT");
//         }
//      }
//   }
//}
