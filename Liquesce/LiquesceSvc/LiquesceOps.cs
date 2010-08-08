using System;
using System.Collections;
using System.IO;
using DokanNet;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int Cleanup(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int FlushFileBuffers(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int FindFiles(string filename, ArrayList files, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int DeleteFile(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         throw new NotImplementedException();
      }

      public int Unmount(DokanFileInfo info)
      {
         throw new NotImplementedException();
      }
   }
}