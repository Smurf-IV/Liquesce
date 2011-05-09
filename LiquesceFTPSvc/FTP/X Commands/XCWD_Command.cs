namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XCWD (Change Working Directory)
      /// The XCWD changes the working directory for the connection to the requested path, and is a duplicate of the "CWD" command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XCWD
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XCWD_Command(string cmdArguments)
      {
         CWD_Command(cmdArguments);
      }

      private static void XCWD_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XCWD");
      }
   }
}