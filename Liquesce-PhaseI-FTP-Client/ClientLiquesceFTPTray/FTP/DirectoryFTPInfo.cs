using System;
using System.IO;

namespace ClientLiquesceFTPTray.FTP
{
   class DirectoryFTPInfo : FileSystemFTPInfo
   {
      public DirectoryFTPInfo(FtpClientExt ftpCmdInstance, string path)
         :base( ftpCmdInstance, path )
      {
      }

      #region Overrides of FileSystemFTPInfo

      /// <summary>
      /// Deletes a file or directory.
      /// </summary>
      /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive. </exception><filterpriority>2</filterpriority>
      public override void Delete()
      {
         FtpCmdInstance.DeleteDirectory(path);
      }

      /// <summary>
      /// Gets a value indicating whether the file or directory exists.
      /// </summary>
      /// <returns>
      /// true if the file or directory exists; otherwise, false.
      /// </returns>
      /// <filterpriority>1</filterpriority>
      public override bool Exists
      {
         get 
         {
            return (base.Exists
               && ((FileAttributes.Directory & Attributes ) == FileAttributes.Directory)
               );
         }
      }

      #endregion

      public void Create()
      {
         FtpCmdInstance.MakeDirectory(path);
         attributes = 0;
      }

      public FileSystemFTPInfo[] GetFileSystemInfos(string pattern, SearchOption topDirectoryOnly)
      {
         if (pattern != "*")
            throw new ArgumentOutOfRangeException("pattern", "Cannot be anything but *");
         if (topDirectoryOnly != SearchOption.TopDirectoryOnly)
            throw new ArgumentOutOfRangeException("topDirectoryOnly", "Cannot be anything but topDirectoryOnly");

         return FtpCmdInstance.GetDirList(path).ToArray();
      }
   }

   internal class DirectoryFTP
   {
      #region Static functions
      static public bool Exists(FtpClientExt ftpCmdInstance, string path)
      {
         DirectoryFTPInfo local = new DirectoryFTPInfo(ftpCmdInstance, path);
         return local.Exists;
      }

      #endregion
   }
}
