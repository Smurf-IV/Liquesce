// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
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
      private UInt64 openFilesNextKey;
      private readonly string root;
      // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
      private readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

      public LiquesceOps(ConfigDetails configDetails)
      {
         root = Path.GetFullPath(configDetails.SourceLocations[0]).TrimEnd(Path.DirectorySeparatorChar);
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
            string path = GetPath(filename, fileMode == FileMode.CreateNew);
            if (Directory.Exists(path))
            {
               actualErrorCode = OpenDirectory(filename, info);
               return actualErrorCode;
            }

            //bool fileExists = File.Exists( path );
            //switch (fileMode)
            //{
            //   //case FileMode.Create:
            //   //case FileMode.OpenOrCreate:
            //   //   if (fileExists)
            //   //      actualErrorCode = Dokan.ERROR_ALREADY_EXISTS;
            //   //   break;
            //   case FileMode.CreateNew:
            //      if (fileExists)
            //         return Dokan.ERROR_FILE_EXISTS;
            //      break;
            //   case FileMode.Open:
            //   case FileMode.Append:
            //   case FileMode.Truncate:
            //      if (!fileExists)
            //         return Dokan.ERROR_FILE_NOT_FOUND;
            //      break;
            //}
            //if (!fileExists)
            //{
            //   if (fileAccess == FileAccess.Read)
            //   {
            //      actualErrorCode = Dokan.ERROR_FILE_NOT_FOUND;
            //   }
            //}

            if (fileAccess != FileAccess.Read)
            {
               // Find Quota
               string newDir = Path.GetPathRoot(path);
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
                     path = Path.Combine(newDirLocation, filename);
                     newDir = Path.GetPathRoot(path);
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
            info.Context = ++openFilesNextKey; // never be Zero !
            try
            {
               openFilesSync.EnterWriteLock();
               openFiles.Add(openFilesNextKey, fs);
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
            Log.Trace("CreateFile OUT actualErrorCode=[{0}] context[{1}]", actualErrorCode, openFilesNextKey);
         }
         return actualErrorCode;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("OpenDirectory IN DokanProcessId[{0}]", info.ProcessId);
            if (Directory.Exists(GetPath(filename)))
            {
               info.IsDirectory = true;
               return Dokan.DOKAN_SUCCESS;
            }
            return Dokan.ERROR_PATH_NOT_FOUND;

         }
         finally
         {
            Log.Trace("OpenDirectory OUT");
         }
      }

      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("CreateDirectory IN DokanProcessId[{0}]", info.ProcessId);
            string path = GetPath(filename, true);
            // TODO : Hunt for the parent and create from there downwards.
            if (Directory.Exists(path))
            {
               return Dokan.ERROR_ALREADY_EXISTS;
            }
            info.IsDirectory = true;
            if (Directory.CreateDirectory(path).Exists)
               TrimAndAddUnique(path);
            return Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CreateDirectory threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateDirectory OUT");
         }
      }

      /*
      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
      If the file system application stored file handle in the Context variable when the function CreateFile is invoked, 
      this should be closed in the Cleanup function, not in CloseFile function. If the user application calls CloseHandle
      and subsequently open the same file, the CloseFile function of the file system application may not be invoked 
      before the CreateFile API is called. This may cause sharing violation error. 
      Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      complete the I/O operations. The file system application should also properly work in this case.
       * * */
      static bool IsNullOrDefault<T>(T value)
      {
         return object.Equals(value, default(T));
      }

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
               string path = GetPath(filename);
               if (info.IsDirectory)
               {
                  Log.Trace("DeleteOnClose Directory");
                  if (Directory.Exists(path))
                  {
                     Directory.Delete(path, false);
                  }
               }
               else
               {
                  Log.Trace("DeleteOnClose File");
                  File.Delete(path);
               }
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
            configDetails.SourceLocations.ForEach(location => AddFiles(location + filename, uniqueFiles, pattern));
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
         try
         {
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            return (File.Exists(GetPath(filename)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteFile OUT");
         }
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
            return (Directory.Exists(GetPath(filename)) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteDirectory threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteDirectory OUT");
         }
      }

      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         try
         {
            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
            Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);
            string pathSource = GetPath(filename);
            string pathTarget = GetPath(newname, true);
            Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);

            CloseAndRemove(info);

            if (info.IsDirectory)
            {
               try
               {
                  Directory.Move(pathSource, pathTarget);
               }
               catch (IOException ioex)
               {
                  // An attempt was made to move a directory to a different volume.
                  // The system cannot move the file to a different disk drive.
                  // define ERROR_NOT_SAME_DEVICE            17L
                  int win32 = ((short)Marshal.GetHRForException(ioex) * -1);
                  if (win32 == 17)
                  {
                     // TODO: perform the opertion snecessary to get this dir onto the other medium
                  }
                  throw;
               }
            }
            else
            {
               // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
               UInt32 dwFlags = (uint)(replaceIfExisting ? 1 : 0);
               dwFlags += 2; // MOVEFILE_COPY_ALLOWED
               dwFlags += 8; // MOVEFILE_WRITE_THROUGH
               MoveFileEx(pathSource, pathTarget, dwFlags);
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
         try
         {
            Log.Trace("SetEndOfFile IN DokanProcessId[{0}]", info.ProcessId);
            int error = SetAllocationSize(filename, length, info);
            if (error == Dokan.ERROR_FILE_NOT_FOUND)
            {
               string path = GetPath(filename);
               using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
               {
                  stream.SetLength(length);
                  stream.Close();
               }
               error = Dokan.DOKAN_SUCCESS;
            }
            return error;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetEndOfFile threw: ", ex);
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetEndOfFile OUT");
         }
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
         string foundPath = String.Empty;
         try
         {
            if (filename != PathDirectorySeparatorChar)
            {
               try
               {
                  // Sometimes the windows share put a slash on the end .. Sometimes !!
                  filename = filename.TrimEnd(Path.DirectorySeparatorChar);
                  rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
                  if (!rootPaths.TryGetValue(filename, out foundPath))
                  {
                     bool found = false;
                     if (filename[0] != Path.DirectorySeparatorChar)
                        filename = PathDirectorySeparatorChar + filename;
                     if (configDetails.ShareDetails != null)
                        found = configDetails.ShareDetails.Exists(delegate(ShareDetail shareDetail)
                                                              {
                                                                 Log.Trace("Try and find from [{0}][{1}]",
                                                                           shareDetail.Path, filename);
                                                                 return
                                                                    rootPaths.TryGetValue(shareDetail.Path + filename,
                                                                                          out foundPath);
                                                              });
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
            else
            {
               foundPath = root;
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
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
         }
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
      static extern int MoveFileEx(string lpExistingFileName, string lpNewFileName, UInt32 dwFlags);

      #endregion

      public void InitialiseShares(object state)
      {
         Log.Debug("InitialiseShares IN");
         try
         {
            Thread.Sleep(250); // Give the driver some time to mount
            if (ManagementLayer.Instance.State != LiquesceSvcState.Running)
               return; // A request to exit has occurred
            do
            {
               string[] drives = Environment.GetLogicalDrives();
               if (Array.Exists(drives, dr => dr.Remove(1) == configDetails.DriveLetter))
                  break;
               Log.Info("Waiting for Dokan to create the drive letter before reappling the shares");
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
            ServiceController sc = new ServiceController("Server"); // This name is also used in Win 7
            foreach (ServiceController scDepends in sc.DependentServices)
            {
               Log.Info("Attempting to stop " + scDepends.ServiceName);
               scDepends.Stop();
               scDepends.WaitForStatus(ServiceControllerStatus.Stopped);
            }
            Log.Info("Attempting to stop " + sc.ServiceName);
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped);
            Thread.Sleep(250); //Have to keep these tight, as the Computer Browser service can be triggered from the OS as well.
            Log.Info("Attempting to start " + sc.ServiceName);
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
            foreach (ServiceController scDepends in sc.DependentServices)
            {
               Log.Info("Attempting to start " + scDepends.ServiceName);
               scDepends.Start();
               scDepends.WaitForStatus(ServiceControllerStatus.Running);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Init shares threw: ", ex);
         }
         finally
         {
            Log.Debug("InitialiseShares OUT");
         }
      }
   }
}