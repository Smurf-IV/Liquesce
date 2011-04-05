namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: REIN
      /// Reinitializes the command connection - cancels the current user/password/account information. 
      /// Should be followed by a USER command for another login. 
      /// </summary>
      private void REIN_Command()
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      
   }
}
