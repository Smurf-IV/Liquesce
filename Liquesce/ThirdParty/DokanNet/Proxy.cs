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
         return Marshal.PtrToStringUni(fileName);
      }


	


      public delegate int CreateFileDelegate( IntPtr rawFilName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO dokanFileInfo);

      public int CreateFileProxy( IntPtr rawFileName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            DokanFileInfo info = GetNewFileInfo(ref rawFileInfo);

            int ret = operations.CreateFileRaw(file, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info);

            if (info.IsDirectory)
               rawFileInfo.IsDirectory = 1;

            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateFileProxy threw:", ex);
            return Dokan.ERROR_FILE_NOT_FOUND;
         }

      }

      ////

      public delegate int OpenDirectoryDelegate( IntPtr FileName, ref DOKAN_FILE_INFO FileInfo);

      public int OpenDirectoryProxy( IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
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
            string file = GetFileName(rawFileName);

            List<FileInformation> files = new List<FileInformation>();
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
