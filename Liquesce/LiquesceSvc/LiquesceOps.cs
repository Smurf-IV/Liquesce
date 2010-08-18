// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DokanNet;
using LiquesceFaçade;
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
      // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
      private ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim( LockRecursionPolicy.SupportsRecursion );
      private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

      public LiquesceOps( ConfigDetails configDetails )
      {
         root = Path.GetFullPath( configDetails.SourceLocations[0] );
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
      public int CreateFile( string filename, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, FileOptions fileOptions, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "CreateFile IN filename [{0}], fileMode[{1}], fileAccess[{2}], fileShare[{3}], fileOptions[{4}]",
                        filename, fileMode, fileAccess, fileShare, fileOptions );
            string path = GetPath( filename );
            if (Directory.Exists( path ))
            {
               return OpenDirectory( filename, info );
            }

            const int actualErrorCode = Dokan.DOKAN_SUCCESS;
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
               string newDir = Path.GetPathRoot( path );
               ulong lpFreeBytesAvailable, lpTotalNumberOfBytes, lpTotalNumberOfFreeBytes;
               // Check to see if the location has enough space 
               if (GetDiskFreeSpaceEx( newDir, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes )
                  && (lpFreeBytesAvailable < configDetails.HoldOffBufferBytes))
               {
                  string newDirLocation = configDetails.SourceLocations.Find( str =>
                    (GetDiskFreeSpaceEx( str, out lpFreeBytesAvailable, out lpTotalNumberOfBytes, out lpTotalNumberOfFreeBytes )
                           && (lpFreeBytesAvailable > configDetails.HoldOffBufferBytes))
                 );
                  if (!String.IsNullOrEmpty( newDirLocation ))
                  {
                     path = Path.Combine( newDirLocation, filename );
                     newDir = Path.GetPathRoot( path );
                  }
                  else
                  {
                     // MessageText: Not enough quota is available to process this command.
                     // #define ERROR_NOT_ENOUGH_QUOTA           1816L
                     Marshal.ThrowExceptionForHR( -1816 );

                     //                     return -1816;
                  }
               }
               if (!String.IsNullOrWhiteSpace( newDir ))
                  Directory.CreateDirectory( newDir );
            }

            // might be better to use this
            FileStream fs = new FileStream( path, fileMode, fileAccess, fileShare, (int)configDetails.BufferReadSize, fileOptions );
            info.Context = ++openFilesNextKey; // never be Zero !
            openFiles.Add( openFilesNextKey, fs );
            return actualErrorCode;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "CreateFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "CreateFile OUT" );
         }
      }

      public int OpenDirectory( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "OpenDirectory IN" );
            if (Directory.Exists( GetPath( filename ) ))
            {
               info.IsDirectory = true;
               return Dokan.DOKAN_SUCCESS;
            }
            return Dokan.ERROR_PATH_NOT_FOUND;

         }
         finally
         {
            Log.Trace( "OpenDirectory OUT" );
         }
      }

      public int CreateDirectory( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "CreateDirectory IN" );
            string path = GetPath( filename );
            // TODO : Hunt for the parent and create from there downwards.
            if (Directory.Exists( path ))
            {
               return Dokan.ERROR_ALREADY_EXISTS;
            }
            info.IsDirectory = true;
            if ( Directory.CreateDirectory( path ).Exists )
               TrimAndAddUnique( path );
            return Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "CreateDirectory threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "CreateDirectory OUT" );
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
      static bool IsNullOrDefault<T>( T value )
      {
         return object.Equals( value, default( T ) );
      }

      /// <summary>
      /// When info->DeleteOnClose is true, you must delete the file in Cleanup.
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int Cleanup( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "Cleanup IN" );
            CloseAndRemove( info );
            if (info.DeleteOnClose)
            {
               string path = GetPath( filename );
               if (info.IsDirectory)
               {
                  Log.Trace( "DeleteOnClose Directory" );
                  if (Directory.Exists( path ))
                  {
                     FileInformation[] files;
                     int errorCode = FindFiles( filename, out files );
                     if (Dokan.DOKAN_SUCCESS == errorCode)
                     {
                        if (files.Length == 0)
                        {
                           Directory.Delete(path, false);
                        }
                        else
                           return Dokan.ERROR_DIR_NOT_EMPTY;
                     }
                     else
                     {
                        return errorCode;
                     }
                  }
               }
               else
               {
                  Log.Trace( "DeleteOnClose File" );
                  File.Delete( path );
               }
               try
               {
                  rootPathsSync.TryEnterWriteLock( configDetails.LockTimeout );
                  rootPaths.Remove( filename );
               }
               finally
               {
                  rootPathsSync.ExitWriteLock();
               }
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "Cleanup threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "Cleanup OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int CloseFile( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "CloseFile IN" );
            CloseAndRemove( info );
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "CloseFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "CloseFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int ReadFile( string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info )
      {
         try
         {
            Log.Trace("ReadFile IN");
            bool closeOnReturn = false;
               DokanFileInfo info2 = new DokanFileInfo(1);
            UInt64 context = Convert.ToUInt64(info.Context);
            if (IsNullOrDefault( context ))
            {
               string path = GetPath( filename );

               Log.Warn("No context handle for [" + path + "]");
               int returnValue = CreateFile(filename, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan, info2 );
               if (returnValue != Dokan.DOKAN_SUCCESS)
                  return returnValue;
               context = Convert.ToUInt64( info2.Context );
               if (IsNullOrDefault( context ))
               {
                  return Dokan.ERROR_FILE_NOT_FOUND;
               }
               closeOnReturn = true;
            }
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
            if (closeOnReturn)
               CloseAndRemove( info2 );
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "ReadFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("ReadFile OUT");
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int WriteFile( string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "WriteFile IN" );
            UInt64 context = Convert.ToUInt64( info.Context );
            if (!IsNullOrDefault( context ))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Seek( offset, SeekOrigin.Begin );
               fileStream.Write( buffer, 0, buffer.Length );
               writtenBytes = (uint)buffer.Length;
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (NotSupportedException ex)
         {
            Log.ErrorException( "WriteFile threw: ", ex );
            return Dokan.ERROR_ACCESS_DENIED;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "WriteFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "WriteFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int FlushFileBuffers( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "FlushFileBuffers IN" );
            UInt64 context = Convert.ToUInt64( info.Context );
            if (!IsNullOrDefault( context ))
            {
               openFiles[context].Flush();
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "FlushFileBuffers threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "FlushFileBuffers OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetFileInformation( string filename, FileInformation fileinfo, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "GetFileInformation IN" );
            string path = GetPath( filename );
            if (File.Exists( path ))
            {
               FileInfo info2 = new FileInfo( path );
               fileinfo.Attributes = info2.Attributes;
               fileinfo.CreationTime = info2.CreationTime;
               fileinfo.LastAccessTime = info2.LastAccessTime;
               fileinfo.LastWriteTime = info2.LastWriteTime;
               fileinfo.Length = info2.Length;
               return Dokan.DOKAN_SUCCESS;
            }
            if (Directory.Exists( path ))
            {
               DirectoryInfo info3 = new DirectoryInfo( path );
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
            Log.Trace( "GetFileInformation OUT" );
         }
         return Dokan.DOKAN_ERROR;
      }

      public int FindFiles( string filename, out FileInformation[] files, DokanFileInfo info )
      {
         return FindFiles( filename, out files );
      }
      private int FindFiles( string filename, out FileInformation[] files )
      {
         files = null;
         try
         {
            Log.Trace( "FindFiles IN" );
            Dictionary<string, FileInformation> uniqueFiles = new Dictionary<string, FileInformation>();
            configDetails.SourceLocations.ForEach( str2 => AddFiles( str2 + filename, uniqueFiles ) );
            files = new FileInformation[uniqueFiles.Values.Count];
            uniqueFiles.Values.CopyTo( files, 0 );
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "FindFiles threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "FindFiles OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileAttributes( string filename, FileAttributes attr, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "SetFileAttributes IN" );
            string path = GetPath( filename );
            File.SetAttributes( path, attr );
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "SetFileAttributes threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "SetFileAttributes OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetFileTime( string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "SetFileTime IN" );
            string path = GetPath( filename );
            FileInfo info2 = new FileInfo( path );
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
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "SetFileTime threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "SetFileTime OUT" );
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
      public int DeleteFile( string filename, DokanFileInfo info )
      {
         try
         {
            return (File.Exists( GetPath( filename ) ) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "DeleteFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "DeleteFile OUT" );
         }
      }

      public int DeleteDirectory( string filename, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "DeleteDirectory IN" );
            return (Directory.Exists( GetPath( filename ) ) ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_FILE_NOT_FOUND);
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "DeleteDirectory threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "DeleteDirectory OUT" );
         }
      }

      public int MoveFile( string filename, string newname, bool replaceIfExisting, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "MoveFile IN" );
            Log.Info( "MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname );
            string pathSource = GetPath( filename );
            string pathTarget = GetPath( newname );
            Log.Info( "MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget );

            CloseAndRemove( info );

            if (info.IsDirectory)
            {
               try
               {
                  Directory.Move( pathSource, pathTarget );
               }
               catch (IOException ioex)
               {
                  // An attempt was made to move a directory to a different volume.
                  // The system cannot move the file to a different disk drive.
                  // define ERROR_NOT_SAME_DEVICE            17L
                  int win32 = ((short)Marshal.GetHRForException( ioex ) * -1);
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
               MoveFileEx( pathSource, pathTarget, dwFlags );
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "MoveFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "MoveFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetEndOfFile( string filename, long length, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "SetEndOfFile IN" );
            string path = GetPath( filename );
            // TODO: Should this be detecting if the file is already in the cache
            using (Stream stream = File.Open( path, FileMode.Open, FileAccess.ReadWrite, FileShare.None ))
            {
               stream.SetLength( length );
               stream.Close();
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "SetEndOfFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "SetEndOfFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int SetAllocationSize( string filename, long length, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "SetAllocationSize OUT" );
            UInt64 context = Convert.ToUInt64( info.Context );
            if (!IsNullOrDefault( context ))
            {
               FileStream fileStream = openFiles[context];
               fileStream.SetLength( length );
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "SetAllocationSize threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "SetAllocationSize OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int LockFile( string filename, long offset, long length, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "LockFile IN" );
            if (length < 0)
            {
               Log.Warn( "Resetting length to [0] from [{0}]", length );
               length = 0;
            }
            UInt64 context = Convert.ToUInt64( info.Context );
            if (!IsNullOrDefault( context ))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Lock( offset, length );
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "LockFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "LockFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int UnlockFile( string filename, long offset, long length, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "UnlockFile IN" );
            if (length < 0)
            {
               Log.Warn( "Resetting length to [0] from [{0}]", length );
               length = 0;
            }
            UInt64 context = Convert.ToUInt64( info.Context );
            if (!IsNullOrDefault( context ))
            {
               FileStream fileStream = openFiles[context];
               fileStream.Unlock( offset, length );
            }
            else
            {
               return Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "UnlockFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "UnlockFile OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetDiskFreeSpace( ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info )
      {
         try
         {
            Log.Trace( "GetDiskFreeSpace IN" );
            ulong localFreeBytesAvailable = 0, localTotalBytes = 0, localTotalFreeBytes = 0;
            configDetails.SourceLocations.ForEach( str =>
                                                     {
                                                        ulong num;
                                                        ulong num2;
                                                        ulong num3;
                                                        if (GetDiskFreeSpaceEx( str, out num, out num2, out num3 ))
                                                        {
                                                           localFreeBytesAvailable += num;
                                                           localTotalBytes += num2;
                                                           localTotalFreeBytes += num3;
                                                        }
                                                     } );
            freeBytesAvailable = localFreeBytesAvailable;
            totalBytes = localTotalBytes;
            totalFreeBytes = localTotalFreeBytes;
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException( ex ) * -1);
            Log.ErrorException( "UnlockFile threw: ", ex );
            return (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace( "GetDiskFreeSpace OUT" );
         }
         return Dokan.DOKAN_SUCCESS;
      }

      public int Unmount( DokanFileInfo info )
      {
         Log.Trace( "Unmount IN" );
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
               Log.InfoException( "Unmount closing files threw: ", ex );
            }
         }
         openFiles.Clear();
         Log.Trace( "Unmount out" );
         return Dokan.DOKAN_SUCCESS;
      }

      #endregion
      private string GetPath( string filename )
      {
         string foundPath = String.Empty;
         try
         {
            if (filename != @"\")
            {
               try
               {
                  rootPathsSync.TryEnterReadLock( configDetails.LockTimeout );
                  if (!rootPaths.TryGetValue( filename, out foundPath ))
                     foundPath = root + filename;  // This is used when creating new directory / file
               }
               finally
               {
                  rootPathsSync.ExitReadLock();
               }
            }
            else
            {
               foundPath = root;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException( "GetPath threw: ", ex );
         }
         finally
         {
            Log.Debug( "GetPath from [{0}] found [{1}]", filename, foundPath );
         }
         return foundPath;
      }

      private void AddFiles( string path, Dictionary<string,FileInformation> files )
      {
         try
         {
            DirectoryInfo dirInfo = new DirectoryInfo( path );
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileSystemInfos = new DirectoryInfo(path).GetFileSystemInfos();
               foreach (FileSystemInfo info2 in fileSystemInfos)
               {
                  bool isDirectoy = (info2.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                  FileInformation item = new FileInformation
                                            {
                                               Attributes = info2.Attributes,
                                               CreationTime = info2.CreationTime,
                                               LastAccessTime = info2.LastAccessTime,
                                               LastWriteTime = info2.LastWriteTime,
                                               Length = (isDirectoy) ? 0L : ((FileInfo) info2).Length,
                                               FileName = info2.Name
                                            };
                  files[TrimAndAddUnique(info2.FullName)] = item;
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException( "AddFiles threw: ", ex );
         }
      }

      private string TrimAndAddUnique(string fullFilePath)
      {
         int index = configDetails.SourceLocations.FindIndex( fullFilePath.StartsWith );
         if (index >= 0)
         {
            string key = fullFilePath.Remove( 0, configDetails.SourceLocations[index].Length );
            try
            {
               rootPathsSync.TryEnterWriteLock( configDetails.LockTimeout );
               rootPaths[key] = fullFilePath;
            }
            finally
            {
               rootPathsSync.ExitWriteLock();
            }
            return key;
         }
         throw new ArgumentException( "Unable to find BelongTo Path: " + fullFilePath, fullFilePath );
      }


      private void CloseAndRemove( DokanFileInfo info )
      {
         UInt64 context = Convert.ToUInt64( info.Context );
         if (!IsNullOrDefault( context ))
         {
            FileStream fileStream = openFiles[context];
            Log.Trace( "CloseAndRemove [{0}]", fileStream.Name );
            fileStream.Flush();
            fileStream.Close();
            openFiles.Remove( context );
            info.Context = 0;
         }
      }

      #region DLL Imports
      [DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
      private static extern bool GetDiskFreeSpaceEx( string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes );

      [DllImport( "kernel32.dll", SetLastError = true )]
      static extern int MoveFileEx( string lpExistingFileName, string lpNewFileName, UInt32 dwFlags );

      #endregion
   }
}