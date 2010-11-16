using System;
using System.IO;
using System.Net.Security;
using System.ServiceModel;
using DokanNet;

namespace LiquesceFacade
{
   // Interface used as the redirect target from the client to the actual share
   [ServiceContract]
   public interface IShareEnabler
   {
      /// <summary>
      /// A call to allow the user name with dmain to be checked at the server side for access.
      /// This will then be used to enable a cache lookup for Process ID Token with user name to allow access for the intended share
      /// </summary>
      /// <param name="DomainUserIdentity">{0}\{1}</param>
      /// <param name="sharePath">The path returned from the service when it states what shares are enabled</param>
      /// <param name="writeable">Does the user have write acces to the this share</param>
      /// <returns>true for valid user access</returns>
      [OperationContract]
      bool CanIdentityUseThis(string DomainUserIdentity, string sharePath, out bool writeable);

      [OperationContract]
      int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, out UInt64 fileRefContext, out bool isDirectory);

      [OperationContract]
      int OpenDirectory(string filename, out bool isDirectory);

      [OperationContract]
      int CreateDirectory(string filename, out bool isDirectory);

      [OperationContract]
      int Cleanup(string filename, ref UInt64 fileRefContext, bool deleteOnClose, bool isDirectory);

      [OperationContract]
      int CloseFile(string filename, ref UInt64 fileRefContext);

      [OperationContract]
      int ReadFile(string filename, ref byte[] buffer, ref uint readBytes, long offset, UInt64 fileRefContext);

      [OperationContract]
      int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, UInt64 fileRefContext);

      [OperationContract]
      int FlushFileBuffers(string filename, UInt64 fileRefContext);

      [OperationContract]
      int GetFileInformation(string filename, ref FileInformation fileinfo, UInt64 fileRefContext);

      [OperationContract]
      int FindFiles(string filename, out FileInformation[] files);

      [OperationContract]
      int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files);

      [OperationContract]
      int SetFileAttributes(string filename, FileAttributes attr, UInt64 fileRefContext);

      [OperationContract]
      int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, UInt64 fileRefContext);

      [OperationContract]
      int DeleteFile(string filename, ref UInt64 fileRefContext);

      [OperationContract]
      int DeleteDirectory(string filename, UInt64 fileRefContext, bool isDirectory);

      [OperationContract]
      int MoveFile(string filename, string newname, bool replace, UInt64 fileRefContext, bool isDirectory);

      [OperationContract]
      int SetEndOfFile(string filename, long length, UInt64 fileRefContext);

      [OperationContract]
      int SetAllocationSize(string filename, long length, UInt64 fileRefContext);

      [OperationContract]
      int LockFile(string filename, long offset, long length, UInt64 fileRefContext);

      [OperationContract]
      int UnlockFile(string filename, long offset, long length, UInt64 fileRefContext);

      [OperationContract]
      int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes);
   }
}
