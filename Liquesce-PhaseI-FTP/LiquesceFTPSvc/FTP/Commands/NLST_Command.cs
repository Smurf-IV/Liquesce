using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: NLST [remote-directory]
      /// Returns a list of filenames in the given directory (defaulting to the current directory), with no other information. 
      /// Must be preceded by a PORT or PASV command. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void NLST_Command(string CmdArguments)
      {
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
         // TODO: Optimize this to use DirInfo
         if (!Directory.Exists(Path))
         {
            SendMessage("550 Invalid Path.\r\n");
            return;
         }

         Socket DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }

         try
         {
            string[] FoldersList = Directory.GetDirectories(Path, "*.*", SearchOption.TopDirectoryOnly);
            string FolderList = FoldersList.Aggregate("", (current, Folder) => current + (Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n"));
            DataSocket.Send(UseUTF8 ? Encoding.UTF8.GetBytes(FolderList) : Encoding.ASCII.GetBytes(FolderList));
            DataSocket.Shutdown(SocketShutdown.Both);
            DataSocket.Close();

            SendMessage("226 Transfer Complete.\r\n");
         }
         catch
         {
            SendMessage("426 Connection closed; transfer aborted.\r\n");
         }
      }

   }
}
