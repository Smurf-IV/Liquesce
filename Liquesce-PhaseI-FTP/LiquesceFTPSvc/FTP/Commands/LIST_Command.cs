using System.IO;
using System.Net.Sockets;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   /// <summary>
   /// Syntax: LIST [remote-filespec]
   /// If remote-filespec refers to a file, sends information about that file. 
   /// If remote-filespec refers to a directory, sends information about each file in that directory. 
   /// remote-filespec defaults to the current directory. This command must be preceded by a PORT or PASV command. 
   /// </summary>
   partial class FTPClientCommander
   {
      void LIST_Command(string CmdArguments)
      {
         // TODO: Deal with -a -l -la options to this command
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
         // TODO: Optimize this to use dirInfo
         if (!ConnectedUser.CanViewHiddenFolders && (new DirectoryInfo(Path).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
         {
            SendMessage("550 Invalid path specified.\r\n");
            return;
         }

         Socket DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }

         try
         {
            string[] FilesList = Directory.GetFiles(Path, "*.*", SearchOption.TopDirectoryOnly);
            string[] FoldersList = Directory.GetDirectories(Path, "*.*", SearchOption.TopDirectoryOnly);
            string strFilesList = "";

            if (ConnectedUser.CanViewHiddenFolders)
            {
               foreach (string Folder in FoldersList)
               {
                  string date = Directory.GetCreationTime(Folder).ToString("MM-dd-yy hh:mmtt");
                  strFilesList += date + " <DIR> " + Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
               }
            }
            else
            {
               foreach (string Folder in FoldersList)
               {
                  if ((new DirectoryInfo(Folder).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;

                  string date = Directory.GetCreationTime(Folder).ToString("MM-dd-yy hh:mmtt");
                  strFilesList += date + " <DIR> " + Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
               }
            }

            if (ConnectedUser.CanViewHiddenFiles)
            {
               foreach (string FileName in FilesList)
               {
                  string date = File.GetCreationTime(FileName).ToString("MM-dd-yy hh:mmtt");
                  strFilesList += date + " " + new FileInfo(FileName).Length + " " + FileName.Substring(FileName.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
               }
            }
            else
            {
               foreach (string FileName in FilesList)
               {
                  if ((File.GetAttributes(FileName) & FileAttributes.Hidden) == FileAttributes.Hidden) continue;

                  string date = File.GetCreationTime(FileName).ToString("MM-dd-yy hh:mmtt");
                  strFilesList += date + " " + new FileInfo(FileName).Length + " " + FileName.Substring(FileName.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
               }
            }
            DataSocket.Send(UseUTF8 ? Encoding.UTF8.GetBytes(strFilesList) : Encoding.ASCII.GetBytes(strFilesList));
            SendMessage("226 Transfer Complete.\r\n");
         }
         catch (DirectoryNotFoundException)
         {
            SendMessage("550 Invalid path specified.\r\n");
         }
         catch
         {
            SendMessage("426 Connection closed; transfer aborted.\r\n");
         }
         finally
         {
            DataSocket.Shutdown(SocketShutdown.Both);
            DataSocket.Close(); 
// ReSharper disable RedundantAssignment
            DataSocket = null;
// ReSharper restore RedundantAssignment
         }
      }

   }
}
