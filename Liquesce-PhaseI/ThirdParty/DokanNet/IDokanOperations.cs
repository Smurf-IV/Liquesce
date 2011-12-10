using System;
using System.IO;

namespace DokanNet
{
   public class DokanFileInfo
   {
      public ulong refFileHandleContext; // Used by the ops.cs files to store a lookup to the FileStream etc.
      public bool IsDirectory;
      public uint ProcessId;
      public bool DeleteOnClose;
      public bool PagingIo;
      public bool SynchronousIo;
      public bool Nocache;
      public bool WriteToEndOfFile;
   }

   public interface IDokanOperations
   {
      int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info);

      int OpenDirectory( string filename, DokanFileInfo info);

      int CreateDirectory( string filename, DokanFileInfo info);

      int Cleanup( string filename, DokanFileInfo info);

      int CloseFile( string filename, DokanFileInfo info);

      int ReadFileNative(string file, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo convertFileInfo);

      int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info);

      int FlushFileBuffersNative( string filename, DokanFileInfo info);

      int GetFileInformationNative( string filename, ref BY_HANDLE_FILE_INFORMATION rawHandleFileInformation, DokanFileInfo info);

      int FindFiles(string filename, out WIN32_FIND_DATA[] files, DokanFileInfo info);

      int FindFilesWithPattern(string filename, string pattern, out WIN32_FIND_DATA[] files, DokanFileInfo info);

      int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info);

      int SetFileTimeNative(string filename, ref WIN32_FIND_FILETIME rawCreationTime, ref WIN32_FIND_FILETIME rawLastAccessTime,
          ref WIN32_FIND_FILETIME rawLastWriteTime, DokanFileInfo info);

      int DeleteFile( string filename, DokanFileInfo info);

      int DeleteDirectory( string filename, DokanFileInfo info);

      int MoveFile( string filename, string newname, bool replace, DokanFileInfo info);

      int SetEndOfFile( string filename, long length, DokanFileInfo info);

      int SetAllocationSize( string filename, long length, DokanFileInfo info);

      int LockFile( string filename, long offset, long length, DokanFileInfo info);

      int UnlockFile( string filename, long offset, long length, DokanFileInfo info);

      int GetDiskFreeSpace( ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info);

      int Unmount( DokanFileInfo info);

      int GetFileSecurityNative(string file, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info);

      int SetFileSecurityNative(string file, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, DokanFileInfo info);
   }
}