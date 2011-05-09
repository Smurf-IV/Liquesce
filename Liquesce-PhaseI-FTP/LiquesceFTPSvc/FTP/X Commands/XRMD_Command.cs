namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XRMD (Remove Directory)
      /// The XDEL deletes the specified directory on the server, and is a duplicate of the "RMD" command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XRMD
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XRMD_Command(string cmdArguments)
      {
         RMD_Command(cmdArguments);
      }

      private static void XRMD_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XRMD");
      }
   }
}