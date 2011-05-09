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
      /// Syntax: NLST [remote-directory]
      /// Returns a list of filenames in the given directory (defaulting to the current directory), with no other information. 
      /// Must be preceded by a PORT or PASV command. 
      /// http://forums.proftpd.org/smf/index.php?topic=2974.0;wap2
      /// </summary>
      /// <param name="CmdArguments"></param>
      void NLST_Command(string CmdArguments)
      {
         string Path = GetExactPath(CmdArguments);
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
               FileInfo[] foldersList = dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
               foreach (FileSystemInfo info in
                  foldersList.Where(info2 => ((info2.Attributes & FileAttributes.System) != FileAttributes.System)
                        && (ConnectedUser.CanViewHiddenFolders
                           || ((info2.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                           )
                     )
               {
                  sw.Write(info.Name);
               }
            }
            DataSocket.Flush();
            DataSocket.Close(15);
            SendOnControlStream("226 Transfer Complete.");
         }
         catch (DirectoryNotFoundException ex)
         {
            Log.ErrorException("LIST_Command: ", ex);
            SendOnControlStream("550 Invalid path specified." + ex.Message);
         }
         catch (UnauthorizedAccessException uaex)
         {
            Log.ErrorException("LIST_Command: ", uaex);
            SendOnControlStream("550 Requested action not taken. permission denied. " + uaex.Message);
         }
         catch (Exception ex)
         {
            Log.ErrorException("LIST_Command: ", ex);
            SendOnControlStream("426 Connection closed; transfer aborted.");
         }
      }

   }
}
