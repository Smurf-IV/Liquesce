using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DokanNet;
using LiquesceFacade;
using NLog;

namespace DokanTesting
{
   public class TestDokanOperations : IDokanOperations
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition,
                            uint rawFlagsAndAttributes, DokanFileInfo info)
      {
         Log.Debug("CreateFile IN filename [{0}], rawAccessMode[0x{1:X8}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}|{5}], ProcessId[{6}]",
           filename, rawAccessMode, (FileShare)rawShare, (FileMode)rawCreationDisposition, (rawFlagsAndAttributes & 0xFFFE0000), (FileAttributes)(rawFlagsAndAttributes & 0x0001FFFF), info.ProcessId);
         info.IsDirectory = (filename == "\\");
         info.refFileHandleContext = 1;
         return Dokan.DOKAN_SUCCESS;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         Log.Debug("OpenDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         info.IsDirectory = true;
         return Dokan.DOKAN_SUCCESS;
      }

      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         Log.Debug("CreateDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int Cleanup(string filename, DokanFileInfo info)
      {
         Log.Trace("Cleanup IN DokanProcessId[{0}] with filename [{1}] handle[{2}] isDir[{3}]", info.ProcessId, filename, info.refFileHandleContext, info.IsDirectory);
         return Dokan.DOKAN_SUCCESS;
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         Log.Trace("CloseFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.DOKAN_SUCCESS;
      }

      public int ReadFileNative(string file, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset,
                                DokanFileInfo convertFileInfo)
      {
         Log.Debug("ReadFile [{0}] IN offset=[{2}] DokanProcessId[{1}]", file, convertFileInfo.ProcessId, rawOffset);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten,
                                 long rawOffset, DokanFileInfo info)
      {
         Log.Debug("WriteFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int FlushFileBuffersNative(string filename, DokanFileInfo info)
      {
         Log.Debug("FlushFileBuffers IN [{0}] DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int GetFileInformationNative(string filename, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation,
                                          DokanFileInfo info)
      {
         Log.Debug("GetFileInformationNative [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         rawHandleFileInformation.dwFileAttributes = info.IsDirectory ?FileAttributes.Directory: FileAttributes.Temporary;
         rawHandleFileInformation.nFileSizeHigh = 0;
         rawHandleFileInformation.nFileSizeLow = 0;
         return Dokan.DOKAN_SUCCESS;
      }

      public int FindFiles(string filename, out WIN32_FIND_DATA[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files);
      }

      public int FindFilesWithPattern(string filename, string pattern, out WIN32_FIND_DATA[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files, pattern);
      }

      private int FindFiles(string filename, uint processId, out WIN32_FIND_DATA[] files, string pattern = "*")
      {
         Log.Debug("FindFiles IN [{0}], pattern[{1}]", filename, pattern);
         files = null;
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }


      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         Log.Debug("SetFileAttributes [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int SetFileTimeNative(string filename, ref WIN32_FIND_FILETIME rawCreationTime,
                                   ref WIN32_FIND_FILETIME rawLastAccessTime, ref WIN32_FIND_FILETIME rawLastWriteTime,
                                   DokanFileInfo info)
      {
         Log.Debug("SetFileTime [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int DeleteFile(string filename, DokanFileInfo info)
      {
         Log.Debug("DeleteFile [{0}] IN DokanProcessId[{1}], refFileHandleContext[{2}]", filename, info.ProcessId, info.refFileHandleContext);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         Log.Debug("DeleteDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
      {
         Log.Debug("MoveFile [{0}] to [{1}] IN DokanProcessId[{2}] context[{2}]", filename, newname, info.ProcessId, info.refFileHandleContext);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         Log.Debug("SetEndOfFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         Log.Debug("SetAllocationSize [{0}] IN DokanProcessId[{0}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         Log.Debug("LockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         Log.Debug("UnlockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         Log.Trace("GetDiskFreeSpace IN DokanProcessId[{0}]", info.ProcessId);
         freeBytesAvailable = totalBytes = totalFreeBytes = 1;
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetVolumeInformation(IntPtr rawVolumeNameBuffer, uint rawVolumeNameSize, ref uint rawVolumeSerialNumber,
                                      ref uint rawMaximumComponentLength, ref uint rawFileSystemFlags, IntPtr rawFileSystemNameBuffer,
                                      uint rawFileSystemNameSize, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("GetVolumeInformation IN DokanProcessId[{0}]", info.ProcessId);


            byte[] volume = Encoding.Unicode.GetBytes("UnitTesting");
            int length = volume.Length;
            byte[] volumeNull = new byte[length + 2];
            Array.Copy(volume, volumeNull, length);
            Marshal.Copy(volumeNull, 0, rawVolumeNameBuffer, Math.Min((int)rawVolumeNameSize, length + 2));
            rawVolumeSerialNumber = 123456789;
            rawMaximumComponentLength = 256;

            // FILE_FILE_COMPRESSION      0x00000010     // Don't do this.. It causes lot's of problems later on  
            // And the Dokan code does not support it
            //case FileStreamInformation:
            //            //DbgPrint("FileStreamInformation\n");
            //            status = STATUS_NOT_IMPLEMENTED;
            //            break;

            rawFileSystemFlags = (uint)(FILE_SYSTEM_FLAGS.FILE_CASE_PRESERVED_NAMES
               // | FILE_SYSTEM_FLAGS.FILE_CASE_SENSITIVE_SEARCH // NTFS is case-preserving but case-insensitive in the Win32 namespace
               //| FILE_SYSTEM_FLAGS.FILE_NAMED_STREAMS
               //| FILE_SYSTEM_FLAGS.FILE_SEQUENTIAL_WRITE_ONCE
               | FILE_SYSTEM_FLAGS.FILE_SUPPORTS_EXTENDED_ATTRIBUTES
               //| FILE_SYSTEM_FLAGS.FILE_SUPPORTS_HARD_LINKS  
               | FILE_SYSTEM_FLAGS.FILE_UNICODE_ON_DISK
               | FILE_SYSTEM_FLAGS.FILE_PERSISTENT_ACLS
               //| FILE_SYSTEM_FLAGS.FILE_VOLUME_QUOTAS
               );

            // rawFileSystemFlags |= (uint) FILE_SYSTEM_FLAGS.FILE_READ_ONLY_VOLUME;

            byte[] sys = Encoding.Unicode.GetBytes("DokanTesting");
            length = sys.Length;
            byte[] sysNull = new byte[length + 2];
            Array.Copy(sys, sysNull, length);

            Marshal.Copy(sysNull, 0, rawFileSystemNameBuffer, Math.Min((int)rawFileSystemNameSize, length + 2));

            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetVolumeInformation threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("GetVolumeInformation OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Warn("Unmount IN DokanProcessId[{0}]", info.ProcessId);
         return Dokan.DOKAN_SUCCESS;
      }

      public int GetFileSecurityNative(string file, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr rawSecurityDescriptor,
                                       uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded,
                                       DokanFileInfo info)
      {
         Log.Debug("GetFileSecurityNative [{0}] IN GetFileSecurity[{1}][{2}]", file, info.ProcessId, rawRequestedInformation);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }

      public int SetFileSecurityNative(string file, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr rawSecurityDescriptor,
                                       uint rawSecurityDescriptorLength, DokanFileInfo info)
      {
         Log.Debug("SetFileSecurityNative IN [{0}] SetFileSecurity[{1}][{2}]", file, info.ProcessId, rawSecurityInformation);
         return Dokan.ERROR_CALL_NOT_IMPLEMENTED;
      }
   }
}