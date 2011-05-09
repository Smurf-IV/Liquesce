namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XDEL (Delete)
      /// The XDEL deletes the specified file on the server, and is a duplicate of the "DEL" command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XDEL
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XDEL_Command(string cmdArguments)
      {
         DELE_Command(cmdArguments);
      }

      private static void XDEL_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XDEL");
      }
   }
}