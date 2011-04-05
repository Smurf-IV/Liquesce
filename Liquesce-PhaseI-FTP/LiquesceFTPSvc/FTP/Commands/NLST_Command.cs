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
      /// http://forums.proftpd.org/smf/index.php?topic=2974.0;wap2
      /// </summary>
      /// <param name="CmdArguments"></param>
      void NLST_Command(string CmdArguments)
      {
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
         DirectoryInfo dirInfo = new DirectoryInfo(Path);
         if (!dirInfo.Exists)
         {
            SendOnControlStream("550 Invalid Path.");
            return;
         }

         NetworkStream DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }

         try
         {
            using (StreamWriter sw = new StreamWriter(DataSocket, UseUTF8 ? Encoding.UTF8 : Encoding.ASCII))
            {
               FileInfo[] FoldersList = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
               for (int index = 0; index < FoldersList.Length; index++)
               {
                  sw.Write(FoldersList[index].Name);
               }
            }
            DataSocket.Flush();
            DataSocket.Close(15);
            SendOnControlStream("226 Transfer Complete.");
         }
         catch
         {
            SendOnControlStream("426 Connection closed; transfer aborted.");
         }
      }

   }
}
