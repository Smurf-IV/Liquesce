// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using DokanNet;
using LiquesceFaçade;
using Microsoft.Win32.SafeHandles;
using NLog;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly ConfigDetails configDetails;
      private readonly Dictionary<UInt64, FileStream> openFiles = new Dictionary<UInt64, FileStream>();
      private UInt64 openFilesNextKey;
      private readonly string root;
      private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

      public LiquesceOps(ConfigDetails configDetails)
      {
         root = Path.GetFullPath(configDetails.SourceLocations[0]);
         this.configDetails = configDetails;
#if DEBUG
         configDetails.HoldOffBufferBytes /= 100;
#endif
      }

      #region IDokanOperations Implementation

      public int CreateFileRaw(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info)
      {
         try
         {
            Log.Trace("CreateFileRaw IN");
            string path = GetPath(filename);
            if (Directory.Exists(path))
            {
               // when filePath is a directory, flags is changed to the file be opened
               rawFlagsAndAttributes |= Dokan.FILE_FLAG_BACKUP_SEMANTICS;
               info.IsDirectory = true;
            }
           FileAccess access = FileAccess.Read;

            if ( ((rawAccessMode & Dokan.FILE_READ_DATA) == Dokan.FILE_READ_DATA) 
               && ((rawAccessMode & Dokan.FILE_WRITE_DATA) == Dokan.FILE_WRITE_DATA)
               )
            {
               access = FileAccess.ReadWrite;
            }
            else if ((rawAccessMode & Dokan.FILE_WRITE_DATA) == Dokan.FILE_WRITE_DATA)
            {
               access = FileAccess.Write;
            }
            else if ( ((rawAccessMode & Dokan.GENERIC_READ) == Dokan.GENERIC_READ) 
               && ((rawAccessMode & Dokan.GENERIC_WRITE) == Dokan.GENERIC_WRITE)
               )
            {
               access = FileAccess.ReadWrite;
            }
            else if ((rawAccessMode & Dokan.GENERIC_WRITE) == Dokan.GENERIC_WRITE)
            {
               access = FileAccess.Write;
            }
            else
            {
               access = FileAccess.Read;
            }

            if (!info.IsDirectory)
            {
               if (!File.Exists(path))
               {
                  if ((rawCreationDisposition == Dokan.OPEN_EXISTING)
                      || (rawCreationDisposition == Dokan.TRUNCATE_EXISTING)
                      || (access == FileAccess.Read)
                     )
                  {
                     return Dokan.ERROR_FILE_NOT_FOUND;
                  }
               }
               else if ((rawCreationDisposition == Dokan.CREATE_ALWAYS)
                        || (rawCreationDisposition == Dokan.OPEN_ALWAYS)
                  )
               {
                  // CreateFile should return ERROR_ALREADY_EXISTS (183) when the CreationDisposition is 
                  // CREATE_ALWAYS or OPEN_ALWAYS and the file under question has already existed.
                  return Dokan.ERROR_ALREADY_EXISTS;
               }
            }
            if (access != FileAccess.Read)
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

            // might be better to use this
            // FileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            SafeFileHandle win32Handle = CreateFile(path, rawAccessMode, rawShare, IntPtr.Zero, // Security !?!
                                            rawCreationDisposition, rawFlagsAndAttributes, IntPtr.Zero);
            if ( (win32Handle == null)
               || win32Handle.IsInvalid
               )
            {
               Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            info.Context = ++openFilesNextKey; // never be Zero !
            openFiles.Add(openFilesNextKey, new FileStream(win32Handle, access));
            return Dokan.DOKAN_SUCCESS;
         }
         catch (IOException ioex)
         {
            Log.ErrorException("CreateFileRaw threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateFileRaw threw: ", ex);
            return Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateFileRaw OUT");
         }
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("OpenDirectory IN");
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
            Log.Trace("CreateDirectory IN");
            string path = GetPath(filename);
            // TODO : Hunt for the parent and create from there downwards.
            if (Directory.Exists(path))
            {
               return Dokan.ERROR_ALREADY_EXISTS;
            }
            info.IsDirectory = true;
            Directory.CreateDirectory(path);
            return Dokan.DOKAN_SUCCESS;
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateDirectory threw: ", ex);
            return Dokan.DOKAN_ERROR;
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

      public int Cleanup(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("Cleanup IN");
            //   UInt64 context = Convert.ToUInt64(info.Context);
            //   if ( !IsNullOrDefault(context))
            //   {
            //      Stream fileStream = openFiles[context].FileStream;
            //      fileStream.Flush();
            //      fileStream.Close();
            //      openFiles.Remove(context);
            //   }
            //}
            //catch (IOException ioex)
            //{
            //   Log.ErrorException("ReadFile threw: ", ioex);
            //   return ((short)Marshal.GetHRForException(ioex) * -1);
            //}
            //catch (Exception ex)
            //{
            //   Log.ErrorException("Cleanup threw: ", ex);
            //                     return Dokan.DOKAN_ERROR;

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
            Log.Trace("CloseFile IN");
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Flush();
               fileStream.Close();
               openFiles.Remove(context);
            }
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cleanup threw: ", ex);
            return Dokan.DOKAN_ERROR;
         }
         finally
         {
            Log.Trace("CloseFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
      {
         try
         {
            Log.Trace("ReadFile IN");
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
               if (offset > fileStream.Length)
               {
                  readBytes = 0;
                  return Dokan.DOKAN_ERROR;
               }
               else
               {
                  fileStream.Seek(offset, SeekOrigin.Begin);
                  readBytes = (uint)fileStream.Read(buffer, 0, buffer.Length);
               }
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("ReadFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
         }
         finally
         {
            Log.Trace("ReadFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
      {
         try
         {
            Log.Trace("WriteFile IN");
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
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
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("FlushFileBuffers IN");
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               openFiles[context].Flush();
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("GetFileInformation IN");
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
            Log.Trace("GetFileInformation OUT");
         }
         return Dokan.DOKAN_ERROR;
      }

      public int FindFiles(string filename, List<FileInformation> files, DokanFileInfo info)
      {
         try
         {
            Log.Trace("FindFiles IN");

            string path = GetPath(filename);
            if (!Directory.Exists(path))
            {
               return Dokan.DOKAN_ERROR;
            }
            Log.Info("FindFiles filename: {0} path:{1} Root: {2}", filename, path, root);
            // TODO: This nneds to be redone in order to have multiple Dir's in different drive locations
            if (path == root)
            {
               Log.Info("Root!");
               rootPaths.Clear();
               configDetails.SourceLocations.ForEach(str2 => AddFiles(str2, files, true));
            }
            else
            {
               AddFiles(path, files, false);
            }

         }
         finally
         {
            Log.Trace("FindFiles OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetFileAttributes IN");
            string path = GetPath(filename);
            File.SetAttributes(path, attr);
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileAttributes threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("SetFileTime IN");
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
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileTime threw: ", ex);
            return Dokan.DOKAN_ERROR;
         }
         finally
         {
            Log.Trace("SetFileTime OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int DeleteFile(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("DeleteFile IN");
            // CloseFile(filename, info); <-- This will be called by the Dokan library to perform the cleanup after this call
            File.Delete(GetPath(filename));
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteFile threw: ", ex);
            return Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         try
         {
            Log.Trace("DeleteDirectory IN");
            string path = GetPath(filename);
            if (!Directory.Exists(path))
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
            Directory.Delete(path, false);
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteDirectory threw: ", ex);
            if (ex.Message.Contains("The directory is not empty"))
            {
               return Dokan.ERROR_DIR_NOT_EMPTY;
            }
            return Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteDirectory OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         try
         {
            Log.Trace("MoveFile IN");
            Log.Info("MoveFile filename: [{0}] newname: [{1}]", filename, newname);
            string pathSource = GetPath(filename);
            string pathTarget = GetPath(newname);
            Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);

            if ( !replaceIfExisting)
            {
               if ( info.IsDirectory )
                  Directory.Move(pathSource, pathTarget);
               else
               {
                  File.Move(pathSource, pathTarget);
               }
            }
            else
            {
               MoveFileEx(pathSource, pathTarget, 1);
            }
         }
         catch (IOException ioex)
         {
            Log.ErrorException("MoveFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("SetEndOfFile IN");
            string path = GetPath(filename);
            // TODO: Should this be detecting if the file is already in the cache
            using (Stream stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
               stream.SetLength(length);
               stream.Close();
            }
         }
         catch (IOException ioex)
         {
            Log.ErrorException("ReadFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
         }
         finally
         {
            Log.Trace("SetEndOfFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         try
         {
            Log.Trace("SetAllocationSize OUT");
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
               fileStream.SetLength(length);
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (IOException ioex)
         {
            Log.ErrorException("SetAllocationSize threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSize threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("LockFile IN");
            if (length < 0)
            {
               Log.Warn("Resetting length to [0] from [{0}]", length);
               length = 0;
            }
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Lock(offset, length);
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (IOException ioex)
         {
            Log.ErrorException("LockFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("UnlockFile IN");
            if (length < 0)
            {
               Log.Warn("Resetting length to [0] from [{0}]", length);
               length = 0;
            }
            UInt64 context = Convert.ToUInt64(info.Context);
            if (!IsNullOrDefault(context))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Unlock(offset, length);
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (IOException ioex)
         {
            Log.ErrorException("UnlockFile threw: ", ioex);
            return ((short)Marshal.GetHRForException(ioex) * -1);
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFile threw: ", ex);
            return Dokan.DOKAN_ERROR;
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
            Log.Trace("GetDiskFreeSpace IN");
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
         finally
         {
            Log.Trace("GetDiskFreeSpace OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Trace("Unmount IN");
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
         Log.Trace("Unmount out");
         return Dokan.DOKAN_SUCCESS;
      }

      #endregion
      private string GetPath(string filename)
      {
         string foundPath = root;
         try
         {
            if (filename != @"\")
            {
               int index = filename.IndexOf('\\', 1);
               string key = index > 0 ? filename.Substring(1, index - 1) : filename.Substring(1);
               foundPath = (rootPaths.ContainsKey(key) ? rootPaths[key] : root) + filename;
            }
         }
         finally
         {
            foundPath = Path.GetFullPath(foundPath);
            Log.Debug("GetPath from [{0}] found [{1}]", filename, foundPath);
         }
         return foundPath;
      }

      private void AddFiles(string path, List<FileInformation> files, bool isRoot)
      {
         FileSystemInfo[] fileSystemInfos = new DirectoryInfo(path).GetFileSystemInfos();
         foreach (FileSystemInfo info2 in fileSystemInfos)
         {
            FileInformation item = new FileInformation
                                      {
                                         Attributes = info2.Attributes,
                                         CreationTime = info2.CreationTime,
                                         LastAccessTime = info2.LastAccessTime,
                                         LastWriteTime = info2.LastWriteTime,
                                         Length = (info2 is DirectoryInfo) ? 0L : ((FileInfo)info2).Length,
                                         FileName = info2.Name
                                      };
            if (isRoot
               && !(info2.Name.StartsWith("$")
                  || info2.Attributes.ToString().Contains("Hidden")
                  )
               )
            {
               rootPaths.Add(info2.Name, path);
            }
            files.Add(item);
         }
      }

#region DLL Imports
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      [DllImport("kernel32.dll", SetLastError = true)]
      static extern int MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

      [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] UInt32 fileAccess,
            [MarshalAs(UnmanagedType.U4)] UInt32 fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] UInt32 creationDisposition,
            [MarshalAs(UnmanagedType.U4)] UInt32 flags,
            IntPtr template);

#endregion
   }
}