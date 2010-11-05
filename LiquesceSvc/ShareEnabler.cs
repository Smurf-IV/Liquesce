using System;
using System.IO;
using DokanNet;
using LiquesceFacade;

namespace LiquesceSvc
{
   public class ShareEnabler : IShareEnabler
   {
      #region Implementation of IShareEnabler

      /// <summary>
      /// A call to allow the user name with dmain to be checked at the server side for access.
      /// This will then be used to enable a cache lookup for Process ID Token with user name to allow access for the intended share
      /// </summary>
      /// <param name="DomainUserIdentity">{0}\{1}</param>
      /// <param name="sharePath">The path returned from the service when it states what shares are enabled</param>
      /// <param name="writeable">Does the user have write acces to the this share</param>
      /// <returns>true for valid user access</returns>
      public bool CanIdentityUseThis(string DomainUserIdentity, string sharePath, out bool writeable)
      {
         // TODO
         writeable = false;
         return true;
      }

      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, ref object DokanContext, ref bool isDirectory)
      {
         DokanFileInfo info = new DokanFileInfo(0){ Context = DokanContext, IsDirectory = isDirectory };
         int status = ManagementLayer.Instance.dokanOperations.CreateFile(filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info);
         DokanContext = info.Context;
         isDirectory = info.IsDirectory;
         return status;
      }

      public int OpenDirectory(string filename, ref object DokanContext, ref bool isDirectory)
      {
         throw new NotImplementedException();
      }

      public int CreateDirectory(string filename, ref object DokanContext, ref bool isDirectory)
      {
         throw new NotImplementedException();
      }

      public int Cleanup(string filename, ref object DokanContext, bool isDirectory)
      {
         throw new NotImplementedException();
      }

      public int CloseFile(string filename, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int FlushFileBuffers(string filename, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int GetFileInformation(string filename, FileInformation fileinfo, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int FindFiles(string filename, out FileInformation[] files, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int SetFileAttributes(string filename, FileAttributes attr, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int DeleteFile(string filename, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int DeleteDirectory(string filename, ref object DokanContext, ref bool isDirectory)
      {
         throw new NotImplementedException();
      }

      public int MoveFile(string filename, string newname, bool replace, ref object DokanContext, ref bool isDirectory)
      {
         throw new NotImplementedException();
      }

      public int SetEndOfFile(string filename, long length, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int SetAllocationSize(string filename, long length, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int LockFile(string filename, long offset, long length, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int UnlockFile(string filename, long offset, long length, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, ref object DokanContext)
      {
         throw new NotImplementedException();
      }

      #endregion
   }
}