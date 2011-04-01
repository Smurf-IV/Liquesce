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
         SendMessage("221 FTP server signing off\r\n");
         Disconnect();
      }

   }
}
