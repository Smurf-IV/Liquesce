using System;
using System.IO;
using System.Net.Sockets;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {  
      /// <summary>
      /// Syntax: STOR remote-filename
      /// Begins transmission of a file to the remote site. 
      /// Must be preceded by either a PORT command or a PASV command so the server knows where to accept data from. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void STOR_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanStoreFiles)
         {
            SendMessage("426 Access Denied.\r\n");
            return;
         }
         Stream FS;

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
         Path = Path.Substring(0, Path.Length - 1);

         try
         {
            FS = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None) {Position = startOffset};
         }
         catch (Exception Ex)
         {
            SendMessage("550 " + Ex.Message + "\r\n");
            return;
         }

         Socket DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }
         try
         {
            byte[] tmpBuffer = new byte[10000];

            int ReadBytes;
            // TODO: Respect the ABOR command
            do
            {
               ReadBytes = DataSocket.Receive(tmpBuffer);
               FS.Write(tmpBuffer, 0, ReadBytes);
            } while (ReadBytes > 0);

            SendMessage("226 Transfer Complete.\r\n");
         }
         catch
         {
            SendMessage("426 Connection closed unexpectedly.\r\n");
         }
         finally
         {
// ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (DataSocket != null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
            {
               DataSocket.Shutdown(SocketShutdown.Both);
               DataSocket.Close();
            }
            FS.Close();
         }
      }

 
   }
}
