using System;
using System.IO;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray.FTP
{
   class FileFTPInfo : FileSystemFTPInfo
   {
      #region Overrides of FileSystemFTPInfo

      public FileFTPInfo(FtpClientExt ftpCmdInstance, string path)
         : base(ftpCmdInstance, path)
      {
      }

      /// <summary>
      /// Deletes a file or directory.
      /// </summary>
      /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive. </exception><filterpriority>2</filterpriority>
      public override void Delete()
      {
         FtpCmdInstance.DeleteFile(path);
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
               && ((FileAttributes.Directory & Attributes) != FileAttributes.Directory)
               );
         }
      }


#endregion

      public void Open(FileMode rawCreationDisposition)
      {
         // http://msdn.microsoft.com/en-us/library/aa363858%28v=vs.85%29.aspx
         bool createZeroLengthFile = false;
         switch (rawCreationDisposition)
         {
            case FileMode.CreateNew:
               createZeroLengthFile = true;
               break;
            case FileMode.Create:
            case FileMode.Truncate:
               if ( Exists )
                  Delete();
               createZeroLengthFile = true;
               break;
            case FileMode.Open:
               break;
            case FileMode.OpenOrCreate:
               createZeroLengthFile =  !Exists;
               break;
            case FileMode.Append:
               // TODO: remember to use the APPE command
               break;
            default:
               throw new ArgumentOutOfRangeException("rawCreationDisposition");
         }
         if (createZeroLengthFile)
         {
            Stream dummy = new MemoryStream(new byte[0], 0, 0, false);
            FtpCmdInstance.PutFile(dummy, path, false);
            // Now force an internal reset to the attribes to find the Size / Dates etc.
            attributes = 0;
         }
      }
   }

   internal class FileFTP
   {
      #region Static functions
      static public bool Exists(FtpClientExt ftpCmdInstance, string path)
      {
         FileFTPInfo local = new FileFTPInfo(ftpCmdInstance, path);
         return local.Exists;
      }

      #endregion
   }

}
