using System;
using System.IO;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray.FTP
{
   abstract class FileSystemFTPInfo //: FileSystemInfo
   {
      protected readonly string path;
      protected readonly FtpClientExt FtpCmdInstance;
      protected internal FileAttributes attributes = 0;

      protected FileSystemFTPInfo(FtpClientExt ftpCmdInstance, string path)
      {
         this.path = path;
         FtpCmdInstance = ftpCmdInstance;
      }

      #region Overrides of FileSystemInfo

      public string FullName
      {
         get { return path; }
      }

// ReSharper disable UnusedMember.Local
      // Remove unsupported features
      private DateTime CreationTime { set; get; }
      private DateTime LastWriteTime { set; get; }
      private DateTime LastAccessTimeUtc { set; get; }
// ReSharper restore UnusedMember.Local

      internal string name;
      /// <summary>
      /// For files, gets the name of the file. For directories, gets the name of the last directory in the hierarchy if a hierarchy exists. Otherwise, the Name property gets the name of the directory.
      /// </summary>
      /// <returns>
      /// A string that is the name of the parent directory, the name of the last directory in the hierarchy, or the name of a file, including the file name extension.
      /// </returns>
      /// <filterpriority>1</filterpriority>
      public string Name
      {
         get
         {
            if (string.IsNullOrEmpty(name))
               name = FtpClient.ExtractPathItemName(path);
            return name;
         }
      }


      internal DateTime lastWriteTimeUtc;
      public DateTime LastWriteTimeUtc
      {
         get
         {
               return lastWriteTimeUtc;
            // TODO: call the MFMT
            return new DateTime();
         }
         set { lastWriteTimeUtc = value; }
      }

      internal DateTime creationTimeUtc;
      public DateTime CreationTimeUtc
      {
         get 
         { 
            return creationTimeUtc;
            // TODO: call the MFCT
            return new DateTime();
         }
         set { creationTimeUtc = value; }
      }

      /// <summary>
      /// Gets a value indicating whether the file or directory exists.
      /// </summary>
      /// <returns>
      /// true if the file or directory exists; otherwise, false.
      /// </returns>
      /// <filterpriority>1</filterpriority>
      public virtual bool Exists
      {
         get
         {
            return ((FileAttributes.Device & Attributes) != FileAttributes.Device);
         }
      }

      private long length = -1;

      public long Length
      {
         get
         {
            if (length < 0)
               length = FtpCmdInstance.GetFileSize(path);
            return length;
         }
         internal set
         {
            length = value;
         }
      }

      public FileAttributes Attributes
      {
         get
         {
            if (attributes == 0)
            {
               attributes = FileAttributes.Device;
               try
               {
                  // Use CWD
                  if (FtpCmdInstance.ChangeDirectory(path))
                  {
                     attributes = FileAttributes.Directory;
                  }
               }
               catch { }
               // No try to see if it a file and / or get more data against the directory
               try
               {
                  FileSystemFTPInfo info = FtpCmdInstance.GetFileDetails((attributes != FileAttributes.Directory) ? path : string.Empty);
                  if (info != null)
                  {
                     attributes = info.attributes;
                     length = info.length;
                     creationTimeUtc = info.creationTimeUtc;
                     lastWriteTimeUtc = info.lastWriteTimeUtc;
                     name = info.name;
                  }
               }
               catch { }
               //finally
               {
                  if ((path != "\\")
                     && ((attributes & FileAttributes.Directory) == FileAttributes.Directory )
                  )
                  {
                     FtpCmdInstance.ChangeDirectory("/");
                  }
               }
            }
            return attributes;
         }
         set
         {
            FtpCmdInstance.SetAttributes(path, value);
            attributes = value;
         }
      }


            /// <summary>
      /// Deletes a file or directory.
      /// </summary>
      /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive. </exception><filterpriority>2</filterpriority>
      public abstract void Delete();

      #endregion
   }
}
