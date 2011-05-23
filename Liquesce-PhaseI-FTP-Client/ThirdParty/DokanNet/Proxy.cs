using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NLog;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace DokanNet
{
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct BY_HANDLE_FILE_INFORMATION
   {
      public uint dwFileAttributes;
      public ComTypes.FILETIME ftCreationTime;
      public ComTypes.FILETIME ftLastAccessTime;
      public ComTypes.FILETIME ftLastWriteTime;
      internal uint dwVolumeSerialNumber;
      public uint nFileSizeHigh;
      public uint nFileSizeLow;
      internal uint dwNumberOfLinks;
      internal uint nFileIndexHigh;
      internal uint nFileIndexLow;
   }

   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct DOKAN_FILE_INFO
   {
      public ulong Context;
      private readonly ulong DokanContext;
      private readonly IntPtr DokanOptions;
      public readonly uint ProcessId;
      public byte IsDirectory;
      public readonly byte DeleteOnClose;
      public readonly byte PagingIo;
      public readonly byte SynchronousIo;
      public readonly byte Nocache;
      public readonly byte WriteToEndOfFile;
   }

   /// <summary>
   /// Check http://msdn.microsoft.com/en-us/library/cc230369%28v=prot.13%29.aspx
   /// and usage http://msdn.microsoft.com/en-us/library/ff556635%28v=vs.85%29.aspx
   /// </summary>
   [Flags] 
   public enum SECURITY_INFORMATION : uint
   {
      /// <summary>
      /// Structure taken from http://www.pinvoke.net/default.aspx/Enums/SECURITY_INFORMATION.html
      /// </summary>
      OWNER_SECURITY_INFORMATION = 0x00000001,
      GROUP_SECURITY_INFORMATION = 0x00000002,
      DACL_SECURITY_INFORMATION = 0x00000004,
      SACL_SECURITY_INFORMATION = 0x00000008,
      // Dokan may not be passing Label ?? 0x00000010
      UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
      UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
      PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
      PROTECTED_DACL_SECURITY_INFORMATION = 0x80000000
   }

   /// <summary>
   /// See http://www.pinvoke.net/search.aspx?search=SECURITY_DESCRIPTOR&namespace=[All]
   /// </summary>
   [StructLayoutAttribute(LayoutKind.Sequential, Pack = 4)]
   public struct SECURITY_DESCRIPTOR
   {
      /// <summary>
      /// Structure taken from http://msdn.microsoft.com/en-us/library/ff556610%28v=vs.85%29.aspx
      /// </summary>
      public byte revision;
      public byte size;
      public short control;   // == SECURITY_DESCRIPTOR_CONTROL
      public IntPtr owner;    // == PSID  
      public IntPtr group;    // == PSID  
      public IntPtr sacl;     // == PACL  
      public IntPtr dacl;     // == PACL  
   }

   public class Proxy
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly IDokanOperations operations;
      private readonly DokanOptions options;
      private const uint volumeSerialNumber = 0x20101112;

      public Proxy(DokanOptions options, IDokanOperations operations)
      {
         this.operations = operations;
         this.options = options;
      }

      /// <summary>
      /// DOKAN_FILE_INFO is a struct so pass byref to keep things the same (Speed etc)
      /// </summary>
      /// <param name="rawInfo">DOKAN_FILE_INFO struct</param>
      /// <returns>new internal DokanFileInfo class</returns>
      private static DokanFileInfo ConvertFileInfo(ref DOKAN_FILE_INFO rawInfo)
      {
         // TODO: If this proves to be expensive in the GC, then perhaps just pass the rawInfo around as a ref !!
         return new DokanFileInfo
         {
            refFileHandleContext = rawInfo.Context,
            IsDirectory = rawInfo.IsDirectory == 1,
            ProcessId = rawInfo.ProcessId,
            DeleteOnClose = rawInfo.DeleteOnClose == 1,
            PagingIo = rawInfo.PagingIo == 1,
            SynchronousIo = rawInfo.SynchronousIo == 1,
            Nocache = rawInfo.Nocache == 1,
            WriteToEndOfFile = rawInfo.WriteToEndOfFile == 1
         };
      }

      private static string GetFileName(IntPtr fileName)
      {
         return Marshal.PtrToStringUni(fileName);
      }



      #region Win32 Constants fro file controls
      // ReSharper disable InconsistentNaming
#pragma warning disable 169
      public const uint GENERIC_READ = 0x80000000;
      public const uint GENERIC_WRITE = 0x40000000;
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

      public const uint FILE_SHARE_READ = 0x00000001;
      public const uint FILE_SHARE_WRITE = 0x00000002;
      private const uint FILE_SHARE_DELETE = 0x00000004;

      public const uint CREATE_NEW = 1;
      public const uint CREATE_ALWAYS = 2;
      public const uint OPEN_EXISTING = 3;
      public const uint OPEN_ALWAYS = 4;
      public const uint TRUNCATE_EXISTING = 5;

      private const uint FILE_ATTRIBUTE_READONLY           =  0x00000001;
      private const uint FILE_ATTRIBUTE_HIDDEN             =  0x00000002;
      private const uint FILE_ATTRIBUTE_SYSTEM             =  0x00000004;
      public const uint FILE_ATTRIBUTE_DIRECTORY          =  0x00000010;
      private const uint FILE_ATTRIBUTE_ARCHIVE            =  0x00000020;
      private const uint FILE_ATTRIBUTE_ENCRYPTED          =  0x00000040;
      private const uint FILE_ATTRIBUTE_NORMAL             =  0x00000080;
      private const uint FILE_ATTRIBUTE_TEMPORARY          =  0x00000100;
      private const uint FILE_ATTRIBUTE_SPARSE_FILE        =  0x00000200;
      private const uint FILE_ATTRIBUTE_REPARSE_POINT      =  0x00000400;
      private const uint FILE_ATTRIBUTE_COMPRESSED         =  0x00000800;
      private const uint FILE_ATTRIBUTE_OFFLINE            =  0x00001000;
      private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
      private const uint FILE_ATTRIBUTE_VIRTUAL            =  0x00010000;
         
         //
      // File creation flags must start at the high end since they
      // are combined with the attributes
      //

      public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
      public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
      public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
      public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
      public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
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

      public int CreateFileProxy(IntPtr rawFileName, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            Log.Trace("CreateFileProxy IN  rawFileName[{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}]",
                        rawFileName, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes);
            string file = GetFileName(rawFileName);

            DokanFileInfo info = ConvertFileInfo(ref rawFileInfo);

            int ret = operations.CreateFile(file, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info);

            rawFileInfo.Context = info.refFileHandleContext;
            rawFileInfo.IsDirectory = Convert.ToByte(info.IsDirectory);

            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateFileProxy threw:", ex);
            return Dokan.ERROR_FILE_NOT_FOUND;
         }

      }

      ////

      public delegate int OpenDirectoryDelegate(IntPtr fileName, ref DOKAN_FILE_INFO fileInfo);

      public int OpenDirectoryProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            DokanFileInfo info = ConvertFileInfo(ref rawFileInfo);
            int ret = operations.OpenDirectory(file, info);
            rawFileInfo.Context = info.refFileHandleContext;
            rawFileInfo.IsDirectory = Convert.ToByte(info.IsDirectory);
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenDirectoryProxy threw:", ex);
            return -1;
         }
      }

      ////

      public delegate int CreateDirectoryDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CreateDirectoryProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            DokanFileInfo info = ConvertFileInfo(ref rawFileInfo);
            int ret = operations.CreateDirectory(file, info);
            rawFileInfo.Context = info.refFileHandleContext;
            rawFileInfo.IsDirectory = Convert.ToByte(info.IsDirectory);
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateDirectoryProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int CleanupDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CleanupProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            int ret = operations.Cleanup(file, ConvertFileInfo(ref rawFileInfo));
            rawFileInfo.Context = 0;
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CleanupProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int CloseFileDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int CloseFileProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            int ret = operations.CloseFile(file, ConvertFileInfo(ref rawFileInfo));
            rawFileInfo.Context = 0;
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CloseFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int ReadFileDelegate(IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int ReadFileProxy(IntPtr rawFileName, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            // Need to reduce memory footprint from Dokan to .Net !!
            // http://code.google.com/p/dokan/issues/detail?id=174
            return operations.ReadFileNative(file, rawBuffer, rawBufferLength, ref rawReadLength, rawOffset, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("ReadFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int WriteFileDelegate(IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int WriteFileProxy(IntPtr rawFileName, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            // Need to reduce memory footprint from Dokan to .Net !!
            // http://code.google.com/p/dokan/issues/detail?id=174
            return operations.WriteFileNative(file, rawBuffer, rawNumberOfBytesToWrite, ref rawNumberOfBytesWritten, rawOffset, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int FlushFileBuffersDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int FlushFileBuffersProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            int ret = operations.FlushFileBuffers(file, ConvertFileInfo(ref rawFileInfo));
            return ret;
         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffersProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int GetFileInformationDelegate(IntPtr fileName, ref BY_HANDLE_FILE_INFORMATION handleFileInfo, ref DOKAN_FILE_INFO fileInfo);

      public int GetFileInformationProxy(IntPtr rawFileName, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            FileInformation fi = new FileInformation();

            int ret = operations.GetFileInformation(file, ref fi, ConvertFileInfo(ref rawFileInfo));

            if (ret == 0)
            {
               rawHandleFileInformation.dwFileAttributes = (uint)fi.Attributes/* + FILE_ATTRIBUTE_VIRTUAL*/;

               rawHandleFileInformation.ftCreationTime.dwHighDateTime = (int)(fi.CreationTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftCreationTime.dwLowDateTime = (int)(fi.CreationTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastAccessTime.dwHighDateTime = (int)(fi.LastAccessTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastAccessTime.dwLowDateTime = (int)(fi.LastAccessTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.ftLastWriteTime.dwHighDateTime = (int)(fi.LastWriteTime.ToFileTime() >> 32);
               rawHandleFileInformation.ftLastWriteTime.dwLowDateTime = (int)(fi.LastWriteTime.ToFileTime() & 0xffffffff);

               rawHandleFileInformation.dwVolumeSerialNumber = volumeSerialNumber;

               rawHandleFileInformation.nFileSizeLow = (uint)(fi.Length & 0xffffffff);
               rawHandleFileInformation.nFileSizeHigh = (uint)(fi.Length >> 32);
               rawHandleFileInformation.dwNumberOfLinks = 1;
               rawHandleFileInformation.nFileIndexHigh = 0;
               rawHandleFileInformation.nFileIndexLow = (uint)file.GetHashCode();
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
         private readonly uint dwReserved0;
         private readonly uint dwReserved1;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
         public string cFileName;
         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
         private readonly string cAlternateFileName;
      }

      private delegate int FILL_FIND_DATA(ref WIN32_FIND_DATA rawFindData, ref DOKAN_FILE_INFO rawFileInfo);

      public delegate int FindFilesDelegate(IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo);

      public int FindFilesProxy(IntPtr rawFileName, IntPtr rawFillFindData, // function pointer
          ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            FileInformation[] files;
            int ret = operations.FindFiles(file, out files, ConvertFileInfo(ref rawFileInfo));

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
            int ret;
            FileInformation[] files = null;
            char[] matchDOS = ("\"<>?").ToCharArray();
            if (-1 != pattern.IndexOfAny(matchDOS))  // See http://liquesce.codeplex.com/workitem/7556
            {
               Log.Info("An Application is using DOS_STAR style pattern matching[{0}], Will switch to compatible mode matching", pattern);
               // PureSync (And maybe others) use the following to get this and / or the subdir contents
               // DirName<"*
               // But there is an issue with the code inside dokan see http://code.google.com/p/dokan/issues/detail?id=192 
               FileInformation[] nonPatternFiles;
               ret = operations.FindFiles(file, out nonPatternFiles, ConvertFileInfo(ref rawFileInfo));
               if (ret == Dokan.DOKAN_SUCCESS)
               {
                  List<FileInformation> matchedFiles = new List<FileInformation>();
                  matchedFiles.AddRange(nonPatternFiles.Where(patternFile => DokanDll.DokanIsNameInExpression(pattern, patternFile.FileName, false)));
                  files = matchedFiles.ToArray();
               }
               // * (asterisk) Matches zero or more characters.
               // ? (question mark) Matches a single character.
               // #define DOS_DOT (L'"') -  Matches either a period or zero characters beyond the name string.
               // #define DOS_QM (L'>') - Matches any single character or, upon encountering a period or end of name string, 
               // advances the expression to the end of the set of contiguous DOS_QMs.
               // #define DOS_STAR (L'<') - Matches zero or more characters until encountering and matching the final . in the name. 
               Log.Debug("DOS_STAR style pattern OUT [found {0}]", (files != null) ? files.Length : 0);
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
            else
               ret = operations.FindFilesWithPattern(file, pattern, out files, ConvertFileInfo(ref rawFileInfo));

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

         fill(ref data, ref rawFileInfo);

      }

      ////

      public delegate int SetEndOfFileDelegate(IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetEndOfFileProxy(IntPtr rawFileName, long rawByteOffset, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            return operations.SetEndOfFile(file, rawByteOffset, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFileProxy threw: ", ex);
            return -1;
         }
      }


      public delegate int SetAllocationSizeDelegate(IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetAllocationSizeProxy(IntPtr rawFileName, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            return operations.SetAllocationSize(file, rawLength, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSizeProxy threw: ", ex);
            return -1;
         }
      }


      ////

      public delegate int SetFileAttributesDelegate(IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetFileAttributesProxy(IntPtr rawFileName, uint rawAttributes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            FileAttributes attr = (FileAttributes)rawAttributes;
            return operations.SetFileAttributes(file, attr, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileAttributesProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int SetFileTimeDelegate(IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
         ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetFileTimeProxy(IntPtr rawFileName, ref ComTypes.FILETIME rawCreationTime, ref ComTypes.FILETIME rawLastAccessTime,
          ref ComTypes.FILETIME rawLastWriteTime, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            // http://liquesce.codeplex.com/workitem/8488
            return operations.SetFileTimeNative(file, ref rawCreationTime, ref rawLastAccessTime, ref rawLastWriteTime, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileTimeProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int DeleteFileDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int DeleteFileProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);

            return operations.DeleteFile(file, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int DeleteDirectoryDelegate(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo);

      public int DeleteDirectoryProxy(IntPtr rawFileName, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            return operations.DeleteDirectory(file, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteDirectoryProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int MoveFileDelegate(IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo);

      public int MoveFileProxy(IntPtr rawFileName, IntPtr rawNewFileName, int rawReplaceIfExisting, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            string newfile = GetFileName(rawNewFileName);

            return operations.MoveFile(file, newfile, (rawReplaceIfExisting != 0), ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int LockFileDelegate(IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int LockFileProxy(IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            return operations.LockFile(file, rawByteOffset, rawLength, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int UnlockFileDelegate(IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int UnlockFileProxy(IntPtr rawFileName, long rawByteOffset, long rawLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            return operations.UnlockFile(file, rawByteOffset, rawLength, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFileProxy threw: ", ex);
            return -1;
         }
      }

      ////

      public delegate int GetDiskFreeSpaceDelegate(ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo);

      public int GetDiskFreeSpaceProxy(ref ulong rawFreeBytesAvailable, ref ulong rawTotalNumberOfBytes,
          ref ulong rawTotalNumberOfFreeBytes, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            return operations.GetDiskFreeSpace(ref rawFreeBytesAvailable, ref rawTotalNumberOfBytes,
                ref rawTotalNumberOfFreeBytes, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetDiskFreeSpaceProxy threw: ", ex);
            return -1;
         }
      }

      public delegate int GetVolumeInformationDelegate(IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO rawFileInfo);

      public int GetVolumeInformationProxy(IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
          ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer, uint rawFileSystemNameSize, ref DOKAN_FILE_INFO fileInfo)
      {
         try
         {
            byte[] volume = Encoding.Unicode.GetBytes(options.VolumeLabel);
            int length = volume.Length;
            byte[] volumeNull = new byte[length + 2];
            Array.Copy(volume, volumeNull, length);
            Marshal.Copy(volumeNull, 0, rawVolumeNameBuffer, Math.Min((int)rawVolumeNameSize, length + 2));
            rawVolumeSerialNumber = volumeSerialNumber;
            rawMaximumComponentLength = 256;

            //#define FILE_CASE_SENSITIVE_SEARCH      0x00000001  
            //#define FILE_CASE_PRESERVED_NAMES       0x00000002  
            //#define FILE_UNICODE_ON_DISK            0x00000004  
            //#define FILE_PERSISTENT_ACLS            0x00000008  // This sends the data to the Recycler and not Recycled
            //#define FILE_SUPPORTS_REMOTE_STORAGE    0x00000100

            // See http://msdn.microsoft.com/en-us/library/cc232101%28v=prot.10%29.aspx for more flags
            //
            // FILE_FILE_COMPRESSION      0x00000010     // Don't do this.. It causes lot's of problems later on
            // And the Dokan code does not support it
            //case FileStreamInformation:
            //            //DbgPrint("FileStreamInformation\n");
            //            status = STATUS_NOT_IMPLEMENTED;
            //            break;
            rawFileSystemFlags = 0x107;

            byte[] sys = Encoding.Unicode.GetBytes("ClientLiquesceFTP");
            length = sys.Length;
            byte[] sysNull = new byte[length + 2];
            Array.Copy(sys, sysNull, length);

            Marshal.Copy(sysNull, 0, rawFileSystemNameBuffer, Math.Min((int)rawFileSystemNameSize, length + 2));
            return 0;
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetVolumeInformationProxy threw: ", ex);
            return -1;
         }
      }


      public delegate int UnmountDelegate(ref DOKAN_FILE_INFO rawFileInfo);

      public int UnmountProxy(ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            return operations.Unmount(ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnmountProxy threw: ", ex);
            return -1;
         }
      }

      public delegate int GetFileSecurityDelegate( IntPtr rawFileName, ref SECURITY_INFORMATION rawRequestedInformation,
          ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded,
          ref DOKAN_FILE_INFO rawFileInfo);

      public int GetFileSecurity( IntPtr rawFileName, ref SECURITY_INFORMATION rawRequestedInformation,
          ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded,
          ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            return operations.GetFileSecurityNative(file, ref rawRequestedInformation, ref rawSecurityDescriptor, rawSecurityDescriptorLength, ref rawSecurityDescriptorLengthNeeded, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileSecurity threw: ", ex);
            return -1;
         }
      }

      public delegate int SetFileSecurityDelegate( IntPtr rawFileName, ref SECURITY_INFORMATION rawSecurityInformation,
          ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref DOKAN_FILE_INFO rawFileInfo);

      public int SetFileSecurity( IntPtr rawFileName, ref SECURITY_INFORMATION rawSecurityInformation,
          ref SECURITY_DESCRIPTOR rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref DOKAN_FILE_INFO rawFileInfo)
      {
         try
         {
            string file = GetFileName(rawFileName);
            return operations.SetFileSecurityNative(file, ref rawSecurityInformation, ref rawSecurityDescriptor, rawSecurityDescriptorLength, ConvertFileInfo(ref rawFileInfo));
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileSecurity threw: ", ex);
            return -1;
         }
      }
   }
}
