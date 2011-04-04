using System.IO;
using System.Linq;
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

         DirectoryInfo dirInfo = new DirectoryInfo(Path);

         if (!ConnectedUser.CanViewHiddenFolders
            && ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            )
         {
            SendMessage("550 Invalid path specified.\r\n");
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
               FileSystemInfo[] infos = dirInfo.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);
               foreach (string data in
                  from info in
                     infos.Where(
                        info =>
                        ConnectedUser.CanViewHiddenFolders ||
                        ((info.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                  let data = info.CreationTimeUtc.ToString("MM-dd-yy hh:mmtt")
                  select
                     data +
                     ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory
                         ? string.Format(" <DIR> {0}\r\n", info.Name)
                         : string.Format(" {0} {1}\r\n", ((FileInfo) info).Length, info.Name)))
               {
                  sw.Write(data);
               }
            }
            DataSocket.Flush();
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
            DataSocket.Close(15);
            // ReSharper disable RedundantAssignment
            DataSocket = null;
            // ReSharper restore RedundantAssignment
         }
      }

   }
}
