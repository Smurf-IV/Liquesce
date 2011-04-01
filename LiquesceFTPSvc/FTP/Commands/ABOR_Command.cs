namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: ABOR
      /// Aborts a file transfer currently in progress. 
      /// </summary>
      private void ABOR_Command()
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }
   }
}
