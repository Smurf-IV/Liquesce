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
         writeable = true;
         return true;
      }

      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, out UInt64 fileRefContext, out bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.CreateFile(filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info);
         fileRefContext = info.refFileHandleContext;
         isDirectory = info.IsDirectory;
         return status;
      }

      public int OpenDirectory(string filename, out bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.OpenDirectory(filename, info);
         isDirectory = info.IsDirectory;
         return status;
      }

      public int CreateDirectory(string filename, out bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.CreateDirectory(filename, info);
         isDirectory = info.IsDirectory;
         return status;
      }

      public int Cleanup(string filename, ref UInt64 fileRefContext, bool deleteOnClose, bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext, DeleteOnClose = deleteOnClose, IsDirectory = isDirectory };
         int status = ManagementLayer.Instance.dokanOperations.Cleanup(filename, info);
         fileRefContext = info.refFileHandleContext;
         return status;
      }

      public int CloseFile(string filename, ref UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.CloseFile(filename, info);
         fileRefContext = info.refFileHandleContext;
         return status;
      }

      public int ReadFile(string filename, ref byte[] buffer, ref uint readBytes, long offset, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         // TODO: Do not send a huge empty buffer over, just to have it return partially filled !!
         int status = ManagementLayer.Instance.dokanOperations.ReadFile(filename, ref buffer, ref readBytes, offset, info);
         return status;
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.WriteFile(filename, buffer, ref writtenBytes, offset, info);
         return status;
      }

      public int FlushFileBuffers(string filename, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.FlushFileBuffers(filename, info);
         return status;
      }

      public int GetFileInformation(string filename, ref FileInformation fileinfo, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.GetFileInformation(filename, ref fileinfo, info);
         return status;
      }

      public int FindFiles(string filename, out FileInformation[] files)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.FindFiles(filename, out files, info);
         return status;
      }

      public int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.FindFilesWithPattern(filename, pattern, out files, info);
         return status;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.SetFileAttributes(filename, attr, info);
         return status;
      }

      public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.SetFileTime(filename, ctime, atime, mtime, info);
         return status;
      }

      public int DeleteFile(string filename, ref UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.DeleteFile(filename, info);
         if ( status == Dokan.DOKAN_SUCCESS )
            status = ManagementLayer.Instance.dokanOperations.Cleanup(filename, info);
         fileRefContext = info.refFileHandleContext;
         return status;
      }

      public int DeleteDirectory(string filename, UInt64 fileRefContext, bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext, IsDirectory = isDirectory};
         int status = ManagementLayer.Instance.dokanOperations.DeleteDirectory(filename, info);
         if (status == Dokan.DOKAN_SUCCESS)
            status = ManagementLayer.Instance.dokanOperations.Cleanup(filename, info);
         return status;
      }

      public int MoveFile(string filename, string newname, bool replace, UInt64 fileRefContext, bool isDirectory)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext, IsDirectory = isDirectory };
         int status = ManagementLayer.Instance.dokanOperations.MoveFile(filename, newname, replace, info);
         return status;
      }

      public int SetEndOfFile(string filename, long length, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.SetEndOfFile(filename, length, info);
         return status;
      }

      public int SetAllocationSize(string filename, long length, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.SetAllocationSize(filename, length, info);
         return status;
      }

      public int LockFile(string filename, long offset, long length, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.LockFile(filename, offset, length, info);
         return status;
      }

      public int UnlockFile(string filename, long offset, long length, UInt64 fileRefContext)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo { refFileHandleContext = fileRefContext };
         int status = ManagementLayer.Instance.dokanOperations.UnlockFile(filename, offset, length, info);
         return status;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes)
      {
         if (ManagementLayer.Instance.dokanOperations == null)
            throw new NullReferenceException("The Dokan Drive has not been started");
         DokanFileInfo info = new DokanFileInfo();
         int status = ManagementLayer.Instance.dokanOperations.GetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes, info);
         return status;
      }

      #endregion
   }
}