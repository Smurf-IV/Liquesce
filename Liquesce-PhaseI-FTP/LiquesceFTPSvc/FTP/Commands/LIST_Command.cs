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
            SendOnControlStream("550 Invalid path specified.");
            return;
         }

         NetworkStream DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }

         try
         {
            FileSystemInfo[] infos = dirInfo.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);

            foreach (FileSystemInfo info in
               infos.Where(info => ConnectedUser.CanViewHiddenFolders || ((info.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)))
            {
               DataSocket.WriteInfo(info.CreationTimeUtc.ToString("MM-dd-yy hh:mmtt"));
               DataSocket.WriteInfo(((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                                       ? " <DIR> "
                                       : string.Format(" {0} ", ((FileInfo)info).Length)
                  );
               DataSocket.WritePathNameCRLN(UseUTF8, info.Name);
            }
            DataSocket.Flush();
            SendOnControlStream("226 Transfer Complete.");
         }
         catch (DirectoryNotFoundException)
         {
            SendOnControlStream("550 Invalid path specified.");
         }
         catch
         {
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
}
