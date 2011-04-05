namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: NOOP
      /// Does nothing except return a response. 
      /// </summary>
      private void NOOP_Command()
      {
         SendOnControlStream("200 OK.");
      }

      
   }
}
