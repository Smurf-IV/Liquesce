namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XPWD (Print Working Directory)
      /// The XPWD requests the current working directory for the connection, and is a duplicate of the "PWD" command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XPWD
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XPWD_Command()
      {
         PWD_Command();
      }

      private static void XPWD_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XPWD");
      }
   }
}