namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XCUP (Change To Parent Directory)
      /// The XCUP changes the current working directory for the current connection to the immediate parent directory, and is a duplicate of the "CWD .." command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XCUP
      /// </summary>
      private void XCUP_Command()
      {
         CDUP_Command();
      }

      private static void XCUP_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XCUP");
      }
   }
}