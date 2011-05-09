namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: ABOR
      /// Aborts a file transfer currently in progress. 
      /// http://www.smartftp.com/support/kb/what-is-the-abor-command-f149.html
      /// </summary>
      private void ABOR_Command()
      {
         // TODO: Need to implement the ABORt command for DataSocket transfers
         // This will require a revamp of the command statuses and transfer commands
         abortReceived = true;

         Rename_FilePath = string.Empty;
         SendOnControlStream("226 Complete.");
      }
   }
}
