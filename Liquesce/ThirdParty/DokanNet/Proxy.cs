using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NLog;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace DokanNet
{
   [StructLayout( LayoutKind.Sequential, Pack = 4 )]
   public struct BY_HANDLE_FILE_INFORMATION
   {
      public uint dwFileAttributes;
      public ComTypes.FILETIME ftCreationTime;
      public ComTypes.FILETIME ftLastAccessTime;
      public ComTypes.FILETIME ftLastWriteTime;
      private readonly uint dwVolumeSerialNumber;
      public uint nFileSizeHigh;
      public uint nFileSizeLow;
      internal uint dwNumberOfLinks;
      internal uint nFileIndexHigh;
      internal uint nFileIndexLow;
   }

   [StructLayout( LayoutKind.Sequential, Pack = 4 )]
   public struct DOKAN_FILE_INFO
   {
      public ulong Context;
      public ulong DokanContext;
      private readonly IntPtr DokanOptions;
      public readonly uint ProcessId;
      public byte IsDirectory;
      public readonly byte DeleteOnClose;
      public readonly byte PagingIo;
      public readonly byte SynchronousIo;
      public readonly byte Nocache;
      public readonly byte WriteToEndOfFile;
   }


   public class Proxy
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly IDokanOperations operations;
      private readonly Dictionary<ulong, DokanFileInfo> infoTable;
      private ulong infoId;
      private readonly object infoTableLock = new object();
      private readonly DokanOptions options;

      public Proxy( DokanOptions options, IDokanOperations operations )
      {
         infoId = 0;
         this.operations = operations;
         this.options = options;
         infoTable = new Dictionary<ulong, DokanFileInfo>();
      }

      private static void ConvertFileInfo( ref DOKAN_FILE_INFO rawInfo, DokanFileInfo info )
      {
         info.IsDirectory = rawInfo.IsDirectory == 1;
         info.ProcessId = rawInfo.ProcessId;
         info.PagingIo = rawInfo.PagingIo == 1;
         info.DeleteOnClose = rawInfo.DeleteOnClose == 1;
         info.SynchronousIo = rawInfo.SynchronousIo == 1;
         info.Nocache = rawInfo.Nocache == 1;
         info.WriteToEndOfFile = rawInfo.WriteToEndOfFile == 1;
      }

      private DokanFileInfo GetNewFileInfo( ref DOKAN_FILE_INFO rawFileInfo )
      {
         DokanFileInfo fileInfo = new DokanFileInfo( rawFileInfo.DokanContext );

         lock (infoTableLock)
         {
            fileInfo.InfoId = ++infoId;

            rawFileInfo.Context = fileInfo.InfoId;
            ConvertFileInfo( ref rawFileInfo, fileInfo );
            // to avoid GC
            infoTable[fileInfo.InfoId] = fileInfo;
         }
         return fileInfo;
      }

      private DokanFileInfo GetFileInfo( ref DOKAN_FILE_INFO rawFileInfo )
      {
         DokanFileInfo fileInfo = null;
         lock (infoTableLock)
         {
            if (rawFileInfo.Context != 0)
            {
               infoTable.TryGetValue( rawFileInfo.Context, out fileInfo );
            }

            if (fileInfo == null)
            {
               // bug?
               fileInfo = new DokanFileInfo( rawFileInfo.DokanContext );
            }
            ConvertFileInfo( ref rawFileInfo, fileInfo );
         }
         return fileInfo;
      }

      private static string GetFileName( IntPtr fileName )
      {
         return Marshal.PtrToStringUni( fileName );
      }



      #region Win32 Constants fro file controls
      // ReSharper disable InconsistentNaming
#pragma warning disable 169
      private const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;
      private const uint GENERIC_READ = 0x80000000;
      private const uint GENERIC_WRITE = 0x40000000;
      private const uint GENERIC_EXECUTE = 0x20000000;

      private const uint FILE_READ_DATA = 0x00000001;
      public const uint FILE_WRITE_DATA = 0x00000002;
      private const uint FILE_APPEND_DATA = 0x00000004;
      private const uint FILE_READ_EA = 0x00000008;
      private const uint FILE_WRITE_EA = 0x00000010;
      private const uint FILE_EXECUTE = 0x00000020;
      private const uint FILE_READ_ATTRIBUTES = 0x00000080;
      private const uint FILE_WRITE_ATTRIBUTES = 0x00000100;
      private const uint DELETE = 0x00010000;
      private const uint READ_CONTROL = 0x00020000;
      private const uint WRITE_DAC = 0x00040000;
      private const uint WRITE_OWNER = 0x00080000;
      private const uint SYNCHRONIZE = 0x00100000;

      private const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;

      private const uint STANDARD_RIGHTS_READ = READ_CONTROL;
      private const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
      private const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;

      private const uint FILE_SHARE_READ = 0x00000001;
      private const uint FILE_SHARE_WRITE = 0x00000002;
      private const uint FILE_SHARE_DELETE = 0x00000004;

      public const uint CREATE_NEW = 1;
      public const uint CREATE_ALWAYS = 2;
      public const uint OPEN_EXISTING = 3;
      private const uint OPEN_ALWAYS = 4;
      public const uint TRUNCATE_EXISTING = 5;

      private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
      private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
      private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
      private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
      private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
      private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
      private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
      private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
      private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;

      //
      // File creation flags must start at the high end since they
      // are combined with the attributes
      //

      private const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
      private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
      private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
      private const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
      private const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
      private const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
      public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
      private const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
      private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
      private const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
      private const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
#pragma warning restore 169
      // ReSharper restore InconsistentNaming
      #endregion

      public delegate int CreateFileDelegate(IntPtr rawFilName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO dokanFileInfo);

      public int CreateFileProxy( IntPtr rawFileName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            Log.Trace("CreateFileProxy IN  rawFileName[{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}]", 
                        rawFileName, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes );
            string file = GetFileName( rawFileName );

            DokanFileInfo info = GetNewFileInfo( ref rawFileInfo );

            int ret = operations.CreateFile( file, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info );

            if (info.IsDirectory)
               rawFileInfo.IsDirectory = 1;

            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "CreateFileProxy threw:", ex );
            return Dokan.ERROR_FILE_NOT_FOUND;
         }

      }

      ////

      public delegate int OpenDirectoryDelegate( IntPtr fileName, ref DOKAN_FILE_INFO fileInfo );

      public int OpenDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            DokanFileInfo info = GetNewFileInfo( ref rawFileInfo );
            int ret = operations.OpenDirectory(file, info);
            if (info.IsDirectory)
               rawFileInfo.IsDirectory = 1;
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "OpenDirectoryProxy threw:", ex );
            return -1;
         }
      }

      ////

      public delegate int CreateDirectoryDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int CreateDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            DokanFileInfo info = GetNewFileInfo( ref rawFileInfo );
            int ret = operations.CreateDirectory( file, info );
            if (info.IsDirectory)
               rawFileInfo.IsDirectory = 1;
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "CreateDirectoryProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int CleanupDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int CleanupProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            return operations.Cleanup( file, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "CleanupProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int CloseFileDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int CloseFileProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            DokanFileInfo info = GetFileInfo( ref rawFileInfo );

            int ret = operations.CloseFile( file, info );

            rawFileInfo.Context = 0;

            lock (infoTableLock)
            {
               infoTable.Remove( info.InfoId );
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "CloseFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int ReadFileDelegate( IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo );

      public int ReadFileProxy( IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            byte[] buf = new Byte[rawBufferLength];

            uint readLength = 0;
            int ret = operations.ReadFile(
                file, buf, ref readLength, rawOffset, GetFileInfo( ref rawFileInfo ) );
            if (ret == 0)
            {
               rawReadLength = readLength;
               Marshal.Copy( buf, 0, rawBuffer, (int)rawBufferLength );
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "ReadFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int WriteFileDelegate( IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo );

      public int WriteFileProxy( IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            Byte[] buf = new Byte[rawNumberOfBytesToWrite];
            Marshal.Copy( rawBuffer, buf, 0, (int)rawNumberOfBytesToWrite );

            uint bytesWritten = 0;
            int ret = operations.WriteFile(
                file, buf, ref bytesWritten, rawOffset, GetFileInfo( ref rawFileInfo ) );
            if (ret == 0)
               rawNumberOfBytesWritten = bytesWritten;
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "WriteFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int FlushFileBuffersDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int FlushFileBuffersProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            int ret = operations.FlushFileBuffers( file, GetFileInfo( ref rawFileInfo ) );
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "FlushFileBuffersProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int GetFileInformationDelegate( IntPtr fileName, ref BY_HANDLE_FILE_INFORMATION handleFileInfo, ref DOKAN_FILE_INFO fileInfo );

      public int GetFileInformationProxy( IntPtr rawFileName, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            FileInformation fi = new FileInformation();

            int ret = operations.GetFileInformation( file, fi, GetFileInfo( ref rawFileInfo ) );

            if (ret == 0)
            {
               rawHandleFileInformation.dwFileAttributes = (uint)fi.Attributes + FILE_ATTRIBUTE_VIRTUAL;

               rawHandleFileInformation.ftCreationTime.dwHighDateTime = (int)(fi.CreationTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftCreationTime.dwLowDateTime = (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastAccessTime.dwHighDateTime = (int)(fi.LastAccessTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastAccessTime.dwLowDateTime = (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastWriteTime.dwHighDateTime = (int)(fi.LastWriteTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastWriteTime.dwLowDateTime = (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.nFileSizeLow = (uint)(fi.Length & 0xffffffff);
               rawHandleFileInformation.nFileSizeHigh = (uint)(fi.Length >> 32);
               rawHandleFileInformation.dwNumberOfLinks = 0;
               rawHandleFileInformation.nFileIndexHigh = 0;
               rawHandleFileInformation.nFileIndexLow = 0;
            }

            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "GetFileInformationProxy threw: ", ex );
            return -1;
         }

      }

      ////

      [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4 )]
      struct WIN32_FIND_DATA
      {
         public FileAttributes dwFileAttributes;
         public ComTypes.FILETIME ftCreationTime;
         public ComTypes.FILETIME ftLastAccessTime;
         public ComTypes.FILETIME ftLastWriteTime;
         public uint nFileSizeHigh;
         public uint nFileSizeLow;
         private readonly uint dwReserved0;
         private readonly uint dwReserved1;
         [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
         public string cFileName;
         [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 14 )]
         private readonly string cAlternateFileName;
      }

      private delegate int FILL_FIND_DATA( ref WIN32_FIND_DATA rawFindData, ref DOKAN_FILE_INFO rawFileInfo );

      public delegate int FindFilesDelegate( IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo );

      public int FindFilesProxy( IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            FileInformation[] files;
            int ret = operations.FindFiles( file, out files, GetFileInfo( ref rawFileInfo ) );

            FILL_FIND_DATA fill = (FILL_FIND_DATA)Marshal.GetDelegateForFunctionPointer( rawFillFindData, typeof( FILL_FIND_DATA ) );

            if ((ret == 0)
               &&(files != null)
               )
            {
               // ReSharper disable ForCanBeConvertedToForeach
               // Used a single entry call to speed up the "enumeration" of the list
               for (int index = 0; index < files.Length; index++)
               // ReSharper restore ForCanBeConvertedToForeach
               {
                  Addto( fill, ref rawFileInfo, files[index] );
               }
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "FindFilesProxy threw: ", ex );
            return -1;
         }

      }

      public delegate int FindFilesWithPatternDelegate(IntPtr rawFileName, IntPtr rawSearchPattern,
          IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo);

      public int FindFilesWithPatternProxy(IntPtr rawFileName, IntPtr rawSearchPattern, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            string pattern = GetFileName(rawSearchPattern);

            FileInformation[] files;
            int ret = operations.FindFilesWithPattern(file, pattern, out files, GetFileInfo(ref rawFileInfo));

            FILL_FIND_DATA fill = (FILL_FIND_DATA)Marshal.GetDelegateForFunctionPointer(rawFillFindData, typeof(FILL_FIND_DATA));

            if ((ret == 0)
               && (files != null)
               )
            {
               // ReSharper disable ForCanBeConvertedToForeach
               // Used a single entry call to speed up the "enumeration" of the list
               for (int index = 0; index < files.Length; index++)
               // ReSharper restore ForCanBeConvertedToForeach
               {
                  Addto(fill, ref rawFileInfo, files[index]);
               }
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("FindFilesProxy threw: ", ex);
            return -1;
         }

      }

      private void Addto(FILL_FIND_DATA fill, ref DOKAN_FILE_INFO rawFileInfo, FileInformation fi)
      {
         WIN32_FIND_DATA data = new WIN32_FIND_DATA
         {
            dwFileAttributes = fi.Attributes,
            ftCreationTime =
            {
               dwHighDateTime = (int)(fi.CreationTime.ToFileTime() >> 32),
               dwLowDateTime = (int)(fi.CreationTime.ToFileTime() & 0xffffffff)
            },
            ftLastAccessTime =
            {
               dwHighDateTime = (int)(fi.LastAccessTime.ToFileTime() >> 32),
               dwLowDateTime = (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff)
            },
            ftLastWriteTime =
            {
               dwHighDateTime = (int)(fi.LastWriteTime.ToFileTime() >> 32),
               dwLowDateTime = (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff)
            },
            nFileSizeLow = (uint)(fi.Length & 0xffffffff),
            nFileSizeHigh = (uint)(fi.Length >> 32),
            cFileName = fi.FileName
         };
         //ZeroMemory(&data, sizeof(WIN32_FIND_DATAW));

         fill( ref data, ref rawFileInfo );

      }

      ////

      public delegate int SetEndOfFileDelegate( IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo );

      public int SetEndOfFileProxy( IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            return operations.SetEndOfFile( file, rawByteOffset, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "SetEndOfFileProxy threw: ", ex );
            return -1;
         }
      }


      public delegate int SetAllocationSizeDelegate( IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo );

      public int SetAllocationSizeProxy( IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            return operations.SetAllocationSize( file, rawLength, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "SetAllocationSizeProxy threw: ", ex );
            return -1;
         }
      }


      ////

      public delegate int SetFileAttributesDelegate( IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo );

      public int SetFileAttributesProxy( IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            FileAttributes attr = (FileAttributes)rawAttributes;
            return operations.SetFileAttributes( file, attr, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "SetFileAttributesProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int SetFileTimeDelegate( IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
         ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo );

      public int SetFileTimeProxy( IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
          ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            long time = ((long)rawCreationTime.dwHighDateTime << 32) + (uint)rawCreationTime.dwLowDateTime;
            if (time == -1)
               time = 0;
            DateTime ctime = DateTime.FromFileTime( time );

            if (time == 0)
               ctime = DateTime.UtcNow;

            time = ((long)rawLastAccessTime.dwHighDateTime << 32) + (uint)rawLastAccessTime.dwLowDateTime;
            if (time == -1)
               time = 0;
            DateTime atime = DateTime.FromFileTime( time );

            if (time == 0)
               atime = DateTime.UtcNow;

            time = ((long)rawLastWriteTime.dwHighDateTime << 32) + (uint)rawLastWriteTime.dwLowDateTime;
            if (time == -1)
               time = 0;
            DateTime mtime = DateTime.FromFileTime( time );

            if (time == 0)
               mtime = DateTime.UtcNow;

            return operations.SetFileTime( file, ctime, atime, mtime, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "SetFileTimeProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int DeleteFileDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int DeleteFileProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );

            return operations.DeleteFile( file, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "DeleteFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int DeleteDirectoryDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo );

      public int DeleteDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            return operations.DeleteDirectory( file, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "DeleteDirectoryProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int MoveFileDelegate( IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo );

      public int MoveFileProxy( IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            string newfile = GetFileName( rawNewFileName );

            return operations.MoveFile( file, newfile, (rawReplaceIfExisting != 0), GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "MoveFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int LockFileDelegate( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo );

      public int LockFileProxy( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            return operations.LockFile( file, rawByteOffset, rawLength, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "LockFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int UnlockFileDelegate( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo );

      public int UnlockFileProxy( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            string file = GetFileName( rawFileName );
            return operations.UnlockFile( file, rawByteOffset, rawLength, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "UnlockFileProxy threw: ", ex );
            return -1;
         }
      }

      ////

      public delegate int GetDiskFreeSpaceDelegate( ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo );

      public int GetDiskFreeSpaceProxy( ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            return operations.GetDiskFreeSpace( ref rawFreeBytesAvailable, ref rawTotalNumberOfBytes,
                ref rawTotalNumberOfFreeBytes, GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "GetDiskFreeSpaceProxy threw: ", ex );
            return -1;
         }
      }

      public delegate int GetVolumeInformationDelegate( IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO rawFileInfo );

      public int GetVolumeInformationProxy( IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO fileInfo )
      {
         try
         {
            byte[] volume = System.Text.Encoding.Unicode.GetBytes( options.VolumeLabel );
            int length = volume.Length;
            byte[] volumeNull = new byte[length+2];
            Array.Copy(volume, volumeNull, length);
            Marshal.Copy(volumeNull, 0, rawVolumeNameBuffer, Math.Min((int)rawVolumeNameSize, length + 2));
            rawVolumeSerialNumber = 0x20101112;
            rawMaximumComponentLength = 256;

//#define FILE_CASE_SENSITIVE_SEARCH      0x00000001  
//#define FILE_CASE_PRESERVED_NAMES       0x00000002  
//#define FILE_UNICODE_ON_DISK            0x00000004  
//#define FILE_PERSISTENT_ACLS            0x00000008  
            rawFileSystemFlags = 7;
            
            byte[] sys = System.Text.Encoding.Unicode.GetBytes( "DOKAN" );
            length = sys.Length;
            byte[] sysNull = new byte[length + 2];
            Array.Copy(sys, sysNull, length);

            Marshal.Copy( sysNull, 0, rawFileSystemNameBuffer, Math.Min( (int)rawFileSystemNameSize, length+2 ) );
            return 0;
         }
         catch (Exception ex)
         {
            Log.ErrorException( "GetVolumeInformationProxy threw: ", ex );
            return -1;
         }
      }


      public delegate int UnmountDelegate( ref DOKAN_FILE_INFO rawFileInfo );

      public int UnmountProxy( ref DOKAN_FILE_INFO rawFileInfo )
      {
         try
         {
            return operations.Unmount( GetFileInfo( ref rawFileInfo ) );
         }
         catch (Exception ex)
         {
            Log.ErrorException( "UnmountProxy threw: ", ex );
            return -1;
         }
      }
   }
}
