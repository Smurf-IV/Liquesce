using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NLog;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace DokanNet
{
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   struct BY_HANDLE_FILE_INFORMATION
   {
      public uint dwFileAttributes;
      public ComTypes.FILETIME ftCreationTime;
      public ComTypes.FILETIME ftLastAccessTime;
      public ComTypes.FILETIME ftLastWriteTime;
      private readonly uint dwVolumeSerialNumber;
      public uint nFileSizeHigh;
      public uint nFileSizeLow;
      private readonly uint dwNumberOfLinks;
      private readonly uint nFileIndexHigh;
      private readonly uint nFileIndexLow;
   }

   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   struct DOKAN_FILE_INFO
   {
      public ulong Context;
      public ulong DokanContext;
      public IntPtr DokanOptions;
      public uint ProcessId;
      public byte IsDirectory;
      public byte DeleteOnClose;
      public byte PagingIo;
      public byte SynchronousIo;
      public byte Nocache;
      public byte WriteToEndOfFile;
   }


   class Proxy
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly IDokanOperations operations;
      private ArrayList array;
      private readonly Dictionary<ulong, DokanFileInfo> infoTable;
      private ulong infoId;
      private readonly object infoTableLock = new object();
      private readonly DokanOptions options;

      public Proxy(DokanOptions options, IDokanOperations operations)
      {
         infoId = 0;
         this.operations = operations;
         this.options = options;
         array = new ArrayList();
         infoTable = new Dictionary<ulong, DokanFileInfo>();
      }

      private static void ConvertFileInfo(ref DOKAN_FILE_INFO rawInfo, DokanFileInfo info)
      {
         info.IsDirectory = rawInfo.IsDirectory == 1;
         info.ProcessId = rawInfo.ProcessId;
         info.PagingIo = rawInfo.PagingIo == 1;
         info.DeleteOnClose = rawInfo.DeleteOnClose == 1;
         info.SynchronousIo = rawInfo.SynchronousIo == 1;
         info.Nocache = rawInfo.Nocache == 1;
         info.WriteToEndOfFile = rawInfo.WriteToEndOfFile == 1;
      }

      private DokanFileInfo GetNewFileInfo(ref DOKAN_FILE_INFO rawFileInfo)
      {
         DokanFileInfo fileInfo = new DokanFileInfo(rawFileInfo.DokanContext);

         lock (infoTableLock)
         {
            fileInfo.InfoId = ++infoId;

            rawFileInfo.Context = fileInfo.InfoId;
            ConvertFileInfo(ref rawFileInfo, fileInfo);
            // to avoid GC
            infoTable[fileInfo.InfoId] = fileInfo;
         }
         return fileInfo;
      }

      private DokanFileInfo GetFileInfo(ref DOKAN_FILE_INFO rawFileInfo)
      {
         DokanFileInfo fileInfo = null;
         lock (infoTableLock)
         {
            if (rawFileInfo.Context != 0)
            {
               infoTable.TryGetValue(rawFileInfo.Context, out fileInfo);
            }

            if (fileInfo == null)
            {
               // bug?
               fileInfo = new DokanFileInfo(rawFileInfo.DokanContext);
            }
            ConvertFileInfo(ref rawFileInfo, fileInfo);
         }
         return fileInfo;
      }

      private static string GetFileName(IntPtr fileName)
      {
         string uniString = Marshal.PtrToStringUni(fileName);
         Log.Info(uniString);
         return uniString;
      }


      private const uint GENERIC_READ = 0x80000000;
      private const uint GENERIC_WRITE = 0x40000000;
      private const uint GENERIC_EXECUTE = 0x20000000;

      private const uint FILE_READ_DATA = 0x0001;
      private const uint FILE_READ_ATTRIBUTES = 0x0080;
      private const uint FILE_READ_EA = 0x0008;
      private const uint FILE_WRITE_DATA = 0x0002;
      private const uint FILE_WRITE_ATTRIBUTES = 0x0100;
      private const uint FILE_WRITE_EA = 0x0010;

      private const uint FILE_SHARE_READ = 0x00000001;
      private const uint FILE_SHARE_WRITE = 0x00000002;
      private const uint FILE_SHARE_DELETE = 0x00000004;

      private const uint CREATE_NEW = 1;
      private const uint CREATE_ALWAYS = 2;
      private const uint OPEN_EXISTING = 3;
      private const uint OPEN_ALWAYS = 4;
      private const uint TRUNCATE_EXISTING = 5;

      private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
      private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
      private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
      private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
      private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
      private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
      private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
      private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
      private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;


      public delegate int CreateFileDelegate( IntPtr rawFilName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO dokanFileInfo);

      public int CreateFileProxy( IntPtr rawFileName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start CreateFileProxy");
            string file = GetFileName(rawFileName);

            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);

            FileAccess access = FileAccess.Read;
            FileShare share = FileShare.None;
            FileMode mode = FileMode.Open;
            FileOptions options = FileOptions.None;

            if ((rawAccessMode & FILE_READ_DATA) != 0 && (rawAccessMode & FILE_WRITE_DATA) != 0)
            {
               access = FileAccess.ReadWrite;
            }
            else if ((rawAccessMode & FILE_WRITE_DATA) != 0)
            {
               access = FileAccess.Write;
            }
            else
            {
               access = FileAccess.Read;
            }

            if ((rawShare & FILE_SHARE_READ) != 0)
            {
               share = FileShare.Read;
            }

            if ((rawShare & FILE_SHARE_WRITE) != 0)
            {
               share |= FileShare.Write;
            }

            if ((rawShare & FILE_SHARE_DELETE) != 0)
            {
               share |= FileShare.Delete;
            }

            switch (rawCreationDisposition)
            {
               case CREATE_NEW:
                  mode = FileMode.CreateNew;
                  break;
               case CREATE_ALWAYS:
                  mode = FileMode.Create;
                  break;
               case OPEN_EXISTING:
                  mode = FileMode.Open;
                  break;
               case OPEN_ALWAYS:
                  mode = FileMode.OpenOrCreate;
                  break;
               case TRUNCATE_EXISTING:
                  mode = FileMode.Truncate;
                  break;
            }

            int ret = operations.CreateFile(file, access, share, mode, options, info);

            if (info.IsDirectory)
               rawFileInfo.IsDirectory = 1;

            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateFileProxy threw:", ex);
            return -2;
         }

      }

      ////

      public delegate int OpenDirectoryDelegate( IntPtr FileName, ref DOKAN_FILE_INFO FileInfo);

      public int OpenDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start OpenDirectoryProxy");
            string file = GetFileName(rawFileName);

            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);
            return operations.OpenDirectory(file, info);

         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenDirectoryProxy threw:", ex);
            return -1;
         }
      }

      ////

      public delegate int CreateDirectoryDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CreateDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start CreateDirectoryProxy");
            string file = GetFileName(rawFileName);

            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);
            return operations.CreateDirectory(file, info);

         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateDirectoryProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int CleanupDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CleanupProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start CleanupProxy");
            string file = GetFileName(rawFileName);
            return operations.Cleanup(file, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("CleanupProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int CloseFileDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CloseFileProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start CloseFileProxy");
            string file = GetFileName(rawFileName);
            DokanFileInfo info = GetFileInfo(ref rawFileInfo);

            int ret = operations.CloseFile(file, info);

            rawFileInfo.Context = 0;

            lock (infoTableLock)
            {
               infoTable.Remove(info.InfoId);
            }
            return ret;

         }
         catch (Exception ex)
         {
            Log.ErrorException("CloseFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int ReadFileDelegate( IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int ReadFileProxy( IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start ReadFileProxy");
            string file = GetFileName(rawFileName);

            byte[] buf = new Byte[rawBufferLength];

            uint readLength = 0;
            int ret = operations.ReadFile(
                file, buf, ref readLength, rawOffset, GetFileInfo(ref rawFileInfo));
            if (ret == 0)
            {
               rawReadLength = readLength;
               Marshal.Copy(buf, 0, rawBuffer, (int)rawBufferLength);
            }
            return ret;

         }
         catch (Exception ex)
         {
            Log.ErrorException("ReadFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int WriteFileDelegate( IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int WriteFileProxy( IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start WriteFileProxy");
            string file = GetFileName(rawFileName);

            Byte[] buf = new Byte[rawNumberOfBytesToWrite];
            Marshal.Copy(rawBuffer, buf, 0, (int)rawNumberOfBytesToWrite);

            uint bytesWritten = 0;
            int ret = operations.WriteFile(
                file, buf, ref bytesWritten, rawOffset, GetFileInfo(ref rawFileInfo));
            if (ret == 0)
               rawNumberOfBytesWritten = bytesWritten;
            return ret;

         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int FlushFileBuffersDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int FlushFileBuffersProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start FlushFileBuffersProxy");
            string file = GetFileName(rawFileName);
            int ret = operations.FlushFileBuffers(file, GetFileInfo(ref rawFileInfo));
            return ret;

         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffersProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int GetFileInformationDelegate( IntPtr FileName, ref BY_HANDLE_FILE_INFORMATION HandleFileInfo, ref DOKAN_FILE_INFO FileInfo);

      public int GetFileInformationProxy( IntPtr rawFileName, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start GetFileInformationProxy");
            string file = GetFileName(rawFileName);

            FileInformation fi = new FileInformation();

            int ret = operations.GetFileInformation(file, fi, GetFileInfo(ref rawFileInfo));

            if (ret == 0)
            {
               rawHandleFileInformation.dwFileAttributes = (uint)fi.Attributes;

               rawHandleFileInformation.ftCreationTime.dwHighDateTime = (int)(fi.CreationTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftCreationTime.dwLowDateTime = (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastAccessTime.dwHighDateTime = (int)(fi.LastAccessTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastAccessTime.dwLowDateTime = (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastWriteTime.dwHighDateTime = (int)(fi.LastWriteTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastWriteTime.dwLowDateTime = (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.nFileSizeLow = (uint)(fi.Length & 0xffffffff);
               rawHandleFileInformation.nFileSizeHigh = (uint)(fi.Length >> 32);
            }

            return ret;

         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileInformationProxy threw: ", ex);
            return -1;
         }

      }

      ////

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
      struct WIN32_FIND_DATA
      {
         public FileAttributes dwFileAttributes;
         public ComTypes.FILETIME ftCreationTime;
         public ComTypes.FILETIME ftLastAccessTime;
         public ComTypes.FILETIME ftLastWriteTime;
         public uint nFileSizeHigh;
         public uint nFileSizeLow;
         public uint dwReserved0;
         public uint dwReserved1;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
         public string cFileName;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
         public string cAlternateFileName;
      }

      private delegate int FILL_FIND_DATA( ref WIN32_FIND_DATA rawFindData, ref DOKAN_FILE_INFO rawFileInfo);

      public delegate int FindFilesDelegate( IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo);

      public int FindFilesProxy( IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start FindFilesProxy");
            string file = GetFileName(rawFileName);

            ArrayList files = new ArrayList();
            int ret = operations.FindFiles(file, files, GetFileInfo(ref rawFileInfo));

            FILL_FIND_DATA fill = (FILL_FIND_DATA)Marshal.GetDelegateForFunctionPointer( rawFillFindData, typeof(FILL_FIND_DATA));

            if (ret == 0)
            {
               IEnumerator entry = files.GetEnumerator();
               while (entry.MoveNext())
               {
                  FileInformation fi = (FileInformation)(entry.Current);
                  WIN32_FIND_DATA data = new WIN32_FIND_DATA();
                  //ZeroMemory(&data, sizeof(WIN32_FIND_DATAW));

                  data.dwFileAttributes = fi.Attributes;

                  data.ftCreationTime.dwHighDateTime = (int)(fi.CreationTime.ToFileTime() >> 32);
                  data.ftCreationTime.dwLowDateTime = (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

                  data.ftLastAccessTime.dwHighDateTime = (int)(fi.LastAccessTime.ToFileTime() >> 32);
                  data.ftLastAccessTime.dwLowDateTime = (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

                  data.ftLastWriteTime.dwHighDateTime = (int)(fi.LastWriteTime.ToFileTime() >> 32);
                  data.ftLastWriteTime.dwLowDateTime = (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

                  data.nFileSizeLow = (uint)(fi.Length & 0xffffffff);
                  data.nFileSizeHigh = (uint)(fi.Length >> 32);

                  data.cFileName = fi.FileName;

                  fill(ref data, ref rawFileInfo);
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

      ////

      public delegate int SetEndOfFileDelegate( IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetEndOfFileProxy( IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start SetEndOfFileProxy");
            string file = GetFileName(rawFileName);

            return operations.SetEndOfFile(file, rawByteOffset, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFileProxy threw: ", ex);
            return -1;
         }
      }


      public delegate int SetAllocationSizeDelegate( IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetAllocationSizeProxy( IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start SetAllocationSizeProxy");
            string file = GetFileName(rawFileName);

            return operations.SetAllocationSize(file, rawLength, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSizeProxy threw: ", ex);
            return -1;
         }
      }


      ////

      public delegate int SetFileAttributesDelegate( IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetFileAttributesProxy( IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start SetFileAttributesProxy");
            string file = GetFileName(rawFileName);

            FileAttributes attr = (FileAttributes)rawAttributes;
            return operations.SetFileAttributes(file, attr, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileAttributesProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int SetFileTimeDelegate( IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime, 
         ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetFileTimeProxy( IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
          ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start SetFileTimeProxy");
            string file = GetFileName(rawFileName);

            long time = ((long)rawCreationTime.dwHighDateTime << 32) + (uint)rawCreationTime.dwLowDateTime;
            DateTime ctime = DateTime.FromFileTime(time);

            if (time == 0)
               ctime = DateTime.MinValue;

            time = ((long)rawLastAccessTime.dwHighDateTime << 32) + (uint)rawLastAccessTime.dwLowDateTime;
            DateTime atime = DateTime.FromFileTime(time);

            if (time == 0)
               atime = DateTime.MinValue;

            time = ((long)rawLastWriteTime.dwHighDateTime << 32) + (uint)rawLastWriteTime.dwLowDateTime;
            DateTime mtime = DateTime.FromFileTime(time);

            if (time == 0)
               mtime = DateTime.MinValue;

            return operations.SetFileTime( file, ctime, atime, mtime, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileTimeProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int DeleteFileDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int DeleteFileProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start DeleteFileProxy");
            string file = GetFileName(rawFileName);

            return operations.DeleteFile(file, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int DeleteDirectoryDelegate( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int DeleteDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start DeleteDirectoryProxy");
            string file = GetFileName(rawFileName);
            return operations.DeleteDirectory(file, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteDirectoryProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int MoveFileDelegate( IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo);

      public int MoveFileProxy( IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start MoveFileProxy");
            string file = GetFileName(rawFileName);
            string newfile = GetFileName(rawNewFileName);

            return operations.MoveFile( file, newfile, rawReplaceIfExisting != 0 ? true : false, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int LockFileDelegate( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int LockFileProxy( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start LockFileProxy");
            string file = GetFileName(rawFileName);
            return operations.LockFile( file, rawByteOffset, rawLength, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int UnlockFileDelegate( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int UnlockFileProxy( IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start UnlockFileProxy");
            string file = GetFileName(rawFileName);
            return operations.UnlockFile( file, rawByteOffset, rawLength, GetFileInfo(ref rawFileInfo));

         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int GetDiskFreeSpaceDelegate( ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo);

      public int GetDiskFreeSpaceProxy( ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start GetDiskFreeSpaceProxy");
            return operations.GetDiskFreeSpace( ref rawFreeBytesAvailable, ref rawTotalNumberOfBytes,
                ref rawTotalNumberOfFreeBytes, GetFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetDiskFreeSpaceProxy threw: ", ex );
            return -1;
         }
      }

      public delegate int GetVolumeInformationDelegate( IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO rawFileInfo);

      public int GetVolumeInformationProxy( IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO fileInfo)
      {
         try
         {
            Log.Info("Start GetVolumeInformationProxy");
            byte[] volume = System.Text.Encoding.Unicode.GetBytes(options.VolumeLabel);
            Marshal.Copy(volume, 0, rawVolumeNameBuffer, Math.Min((int)rawVolumeNameSize, volume.Length));
            rawVolumeSerialNumber = 0x19831116;
            rawMaximumComponentLength = 256;

            // FILE_CASE_SENSITIVE_SEARCH | 
            // FILE_CASE_PRESERVED_NAMES |
            // FILE_UNICODE_ON_DISK
            rawFileSystemFlags = 7;

            byte[] sys = System.Text.Encoding.Unicode.GetBytes("DOKAN");
            Marshal.Copy(sys, 0, rawFileSystemNameBuffer, Math.Min((int)rawFileSystemNameSize, sys.Length));
            return 0;

         }
         catch (Exception ex)
         {
            Log.ErrorException("GetVolumeInformationProxy threw: ", ex);
            return -1;
         }
      }


      public delegate int UnmountDelegate( ref DOKAN_FILE_INFO rawFileInfo);

      public int UnmountProxy( ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Info("Start UnmountProxy");
            return operations.Unmount(GetFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnmountProxy threw: ", ex);
            return -1;
         }
      }
   }
}
