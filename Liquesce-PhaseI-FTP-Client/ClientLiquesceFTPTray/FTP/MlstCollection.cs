using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClientLiquesceFTPTray.FTP;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray.FTP
{
   /// <summary>
   /// Ftp item list.
   /// </summary>
   internal class MlstCollection : List<FileSystemFTPInfo>
   {
      private readonly FtpClientExt ftpCmdInstance;
      private readonly string path;

      /// <summary>
      /// Default constructor for FtpItemCollection.
      /// </summary>
      public MlstCollection()
      { }

      /// <summary>
      /// Split a multi-line file list text response and add the parsed items to the collection.
      /// </summary>
      /// <param name="ftpCmdInstance"></param>
      /// <param name="path">Path to the item on the FTP server.</param>
      /// <param name="dirResults">The multi-line file list text from the FTP server.</param>
      public MlstCollection(FtpClientExt ftpCmdInstance, string path, FtpResponseCollection dirResults)
      {
         if (ftpCmdInstance == null) throw new ArgumentNullException("ftpCmdInstance");
         this.ftpCmdInstance = ftpCmdInstance;
         this.path = path;
         Parse(dirResults);
      }


      private void Parse(FtpResponseCollection dirResults)
      {
         foreach (FileSystemFTPInfo item in
            dirResults.Select(response => ParseLine(response.RawText))
            .Where(item => (item != null) && (item.Name != ".") && (item.Name != ".."))
            )
         {
            Add(item);
         }
      }

      private const string TYPE = @"TYPE=FILE;";
      private const string WIN32_EA = @"WIN32.EA=";
      private const string SIZE = @"SIZE=";
      private const string MODIFY = @"MODIFY=";
      private const string CREATE = @"CREATE=";
      //sb.Append(@"Unique=").Append(fileInfo.FullName.GetHashCode()).Append(';');
      //sb.Append(@"Media-Type=").Append(MimeTypeDetector(fileInfo)).Append(';');
      //   ClientSocket.WritePathNameCRLN(UseUTF8, info.Name);

      private FileSystemFTPInfo ParseLine(string features)
      {
         int nameStart = features.LastIndexOf(';');
         if ((features[0] != '5')
            && (nameStart > 0)
            )
         {
            string localUpper = features.ToUpper();
            string name = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.Default.GetBytes(features.Substring(nameStart+1).Trim()));
            string newPath = Path.Combine(path, name);
            FileSystemFTPInfo info;
            if (!localUpper.Contains(TYPE))
               info = new DirectoryFTPInfo(ftpCmdInstance, newPath);
            else
               info = new FileFTPInfo(ftpCmdInstance, newPath);
            info.name = name;
            int size = localUpper.LastIndexOf(SIZE);
            if (size > 0)
            {
               size += 5; // Enough to step over the search text
               long result;
               if (long.TryParse(features.Substring(size, localUpper.IndexOf(';', size) - size), out result))
                  info.Length = result;
            }

            FileAttributes attributes = 0;
            int win32ea = localUpper.LastIndexOf(WIN32_EA);
            if (win32ea > 0)
            {
               win32ea += 9; // Enough to step over the search text
               if (localUpper[win32ea + 1] == 'X')
                  win32ea += 2;
               UInt32 result;
               if (UInt32.TryParse(features.Substring(win32ea, localUpper.IndexOf(';', win32ea) - win32ea),
                                   NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result))
                  attributes = (FileAttributes)result;
            }
            info.lastWriteTimeUtc = DecodeTime(localUpper, MODIFY);
            info.creationTimeUtc = DecodeTime(localUpper, CREATE);
            if (attributes == 0)
            {
               // TODO: Do this the hardway !
               //sb.Append(@"Perm=");
               //            sb.Append('w'); // that the STOR command may be applied to the object named
               //      sb.Append('a'); // Indicates that the APPE (append) command may be applied to the file named.
               //   }
               //   if (ConnectedUser.CanRenameFiles)
               //      sb.Append('f'); // Allow user to rename
               //      sb.Append('c'); // It indicates that files may be created in the directory named.
               //      sb.Append('m'); // MKD command may be used
               //   }
               //   if (ConnectedUser.CanDeleteFolders)
               //   {
               //      sb.Append('d'); // It indicates that the object named may be deleted
               //      sb.Append('p'); // Directory can be Purged (Deleted)
               //   }
               //   if (ConnectedUser.CanRenameFolders)
               //      sb.Append('f'); // Allow user to rename
               //sb.Append('e');      // Allow user to Enter the directoy
               //sb.Append(@"l;");      // Allow user to List the directoy
               //}
               //sb.Append("r;"); // indicates that the RETR command may be applied to that object
            }
            if (attributes == 0)
            {
               attributes = (info is FileFTPInfo) ? FileAttributes.Normal : FileAttributes.Directory;
            }
            info.attributes = attributes;

            return info;
         }
          return null;
      }

      //sb.Append(@"Modify=").Append(GetFormattedTime(fileInfo.LastWriteTimeUtc)).Append(';');
      // See $\Liquesce-PhaseI-FTP\LiquesceFTPSvc\FTP\New Commands\MDTM_Command.SetFormattedTime
      private const string utcFTPFormat = "yyyyMMddHHmmss";
      private DateTime DecodeTime(string localUpper, string searchText)
      {
         DateTime result = new DateTime();
         int timeStart = localUpper.LastIndexOf(searchText);
         if (timeStart > 0)
         {
            timeStart += searchText.Length; // Enough to step over the search text
            DateTime.TryParseExact(localUpper.Substring(timeStart, localUpper.IndexOf(';', timeStart) - timeStart), utcFTPFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);
         }
         return result;
      }
   }
}