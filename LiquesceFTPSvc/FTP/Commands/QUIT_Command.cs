using System.Threading;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: QUIT
      /// Terminates the command connection. 
      /// </summary>
      private void QUIT_Command()
      {
         try
         {
            abortReceived = true;
            Thread.Yield();
            ClientSocket.WriteAsciiInfo("221 FTP server signing off.\r\n");
         }
         finally
         {
            Disconnect();
         } 
      }

   }
}
