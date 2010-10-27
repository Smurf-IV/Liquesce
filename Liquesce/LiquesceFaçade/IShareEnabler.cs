using System;
using System.IO;
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
      int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref Object DokanContext, ref bool isDirectory);

      [OperationContract]
      int OpenDirectory(string filename, ref Object DokanContext, ref bool isDirectory);

      [OperationContract]
      int CreateDirectory(string filename, ref Object DokanContext, ref bool isDirectory);

      [OperationContract]
      int Cleanup(string filename, ref Object DokanContext, bool isDirectory);

      [OperationContract]
      int CloseFile(string filename, ref Object DokanContext);

      [OperationContract]
      int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, ref Object DokanContext);

      [OperationContract]
      int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, ref Object DokanContext);

      [OperationContract]
      int FlushFileBuffers(string filename, ref Object DokanContext);

      [OperationContract]
      int GetFileInformation(string filename, FileInformation fileinfo, ref Object DokanContext);

      [OperationContract]
      int FindFiles(string filename, out FileInformation[] files, ref Object DokanContext);

      [OperationContract]
      int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files, ref Object DokanContext);

      [OperationContract]
      int SetFileAttributes(string filename, FileAttributes attr, ref Object DokanContext);

      [OperationContract]
      int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, ref Object DokanContext);

      [OperationContract]
      int DeleteFile(string filename, ref Object DokanContext);

      [OperationContract]
      int DeleteDirectory(string filename, ref Object DokanContext, ref bool isDirectory);

      [OperationContract]
      int MoveFile(string filename, string newname, bool replace, ref Object DokanContext, ref bool isDirectory);

      [OperationContract]
      int SetEndOfFile(string filename, long length, ref Object DokanContext);

      [OperationContract]
      int SetAllocationSize(string filename, long length, ref Object DokanContext);

      [OperationContract]
      int LockFile(string filename, long offset, long length, ref Object DokanContext);

      [OperationContract]
      int UnlockFile(string filename, long offset, long length, ref Object DokanContext);

      [OperationContract]
      int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, ref Object DokanContext);
   }
}
