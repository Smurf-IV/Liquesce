using System.IO;
using System.Net.Sockets;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax:  RETR remote-filename
      /// Begins transmission of a file from the remote host. 
      /// Must be preceded by either a PORT command or a PASV command to indicate where the server should send data. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void RETR_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanCopyFiles)
         {
            SendMessage("426 Access Denied.\r\n");
            return;
         }

         string ReturnMessage = string.Empty;

         FileStream FS = null;
         Socket DataSocket = null;
         try
         {
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            Path = Path.Substring(0, Path.Length - 1);

            if ( !ConnectedUser.CanViewHiddenFiles 
               && ((File.GetAttributes(Path) & FileAttributes.Hidden) == FileAttributes.Hidden)
               )
            {
               SendMessage("550 Access Denied or invalid path.\r\n");
               return;
            }

            FS = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read) {Position = startOffset};
         }
         catch(DirectoryNotFoundException ex )
         {
            ReturnMessage = "550 " + ex.Message + "!\r\n";
            goto FinaliseAll;
         }


         DataSocket = GetDataSocket();
         if (DataSocket == null)
            goto FinaliseAll;

         try
         {
            byte[] data = new byte[(FS.Length > 100000) ? 100000 : (int)FS.Length];
            while (DataSocket.Send(data, 0, FS.Read(data, 0, data.Length), SocketFlags.None) != 0)
            {
            }
            // If it gets this far then probably a good send 
            // Clients tend to forcibly close rather than use ABOR.....
            ReturnMessage = "226 Transfer Complete.\r\n";
         }
         catch
         {
            ReturnMessage = "426 Transfer aborted.\r\n";
         }

      FinaliseAll:
         if (FS != null) 
            FS.Close();
         if ((DataSocket != null) 
            && DataSocket.Connected
            )
         {
            DataSocket.Shutdown(SocketShutdown.Both);
            DataSocket.Close();
         }
// ReSharper disable RedundantAssignment
         DataSocket = null;
// ReSharper restore RedundantAssignment
         SendMessage(ReturnMessage);
      }

 
   }
}
