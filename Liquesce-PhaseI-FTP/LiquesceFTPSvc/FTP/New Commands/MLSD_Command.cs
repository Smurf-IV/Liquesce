using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// retrieve listing of files in a directory. 
      /// Unlike NLST, this returns not only file names but also attributes; 
      /// but unlike LIST, it returns the attributes in an extensible standardised format rather than an arbitrary platform-specific one.
      /// http://rfc-ref.org/RFC-TEXTS/3659/chapter7.html
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MLSD_Command(string cmdArguments)
      {
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(cmdArguments);
         DirectoryInfo dirInfo = new DirectoryInfo(Path);
         if (!ConnectedUser.CanViewHiddenFolders
            && ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            )
         {
            SendOnControlStream("550 Invalid path specified.");
         }
         else if (!dirInfo.Exists)
            SendOnControlStream("501 " + cmdArguments + " does not exist.");
         else
         {
            NetworkStream DataSocket = GetDataSocket();
            if (DataSocket == null)
            {
               return;
            }
            try
            {
               FileSystemInfo[] infos = dirInfo.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);
               DataSocket.WriteInfoCRLN(string.Format("150 Details for: [{0}] with [{1}] entries.", cmdArguments, infos.Length));
               foreach (FileSystemInfo info in
                  infos.Where(
                     info =>
                     ConnectedUser.CanViewHiddenFolders ||
                     ((info.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)))
               {
                  DataSocket.WriteInfo(((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                              ? SendDirectory(info)
                              : SendFile(info));
                  DataSocket.WritePathNameCRLN(UseUTF8, info.Name);
               }
               DataSocket.Flush();
               SendOnControlStream("226 Transfer Complete.");
            }
            catch (DirectoryNotFoundException ex)
            {
               Log.ErrorException("MLSD_Command: ", ex);
               SendOnControlStream("550 Invalid path specified." + ex.Message);
            }
            catch (Exception ex)
            {
               Log.ErrorException("MLSD_Command: ", ex);
               SendOnControlStream("426 Connection closed; transfer aborted.");
            }
            finally
            {
               DataSocket.Close(15);
               // ReSharper disable RedundantAssignment
               DataSocket = null;
               // ReSharper restore RedundantAssignment
            }
         }
      }

      private string SendDirectory(FileSystemInfo dirInfo)
      {
         StringBuilder sb = new StringBuilder(@"Type=dir;");
         sb.Append(@"size=4096;");
         sb.Append(@"Modify=").Append(GetFormattedTime(dirInfo.LastWriteTimeUtc)).Append(';');
         sb.Append(@"Create=").Append(GetFormattedTime(dirInfo.CreationTimeUtc)).Append(';');
         sb.Append(@"Perm=");
         if ((dirInfo.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
         {
            if (ConnectedUser.CanStoreFolder)
            {
               sb.Append('c'); // It indicates that files may be created in the directory named.
               sb.Append('m'); // MKD command may be used
            }
            if (ConnectedUser.CanDeleteFolders)
            {
               sb.Append('d'); // It indicates that the object named may be deleted
               sb.Append('p'); // Directory can be Purged (Deleted)
            }
            if (ConnectedUser.CanRenameFolders)
               sb.Append('f'); // Allow user to rename
         }
         sb.Append('e');      // Allow user to Enter the directoy
         sb.Append(@"l;");      // Allow user to List the directoy

         sb.Append(@"Unique=").Append(dirInfo.FullName.GetHashCode()).Append(';');
         sb.Append(@"Win32.ea=").AppendFormat("0x{0:x8}", (uint)dirInfo.Attributes).Append("; ");
         string buffer = sb.ToString();
         Log.Trace("SendDirectory: " + buffer);
         return buffer;
      }

      private static void MLSD_Support(FTPClientCommander thisClient)
      {
         // Note that there is no distinct FEAT output for MLSD.  The presence of the MLST feature indicates 
         // that both MLST and MLSD are supported.
         thisClient.SendOnControlStream(" MLSD");
      }

   }
}
