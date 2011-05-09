namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// XMKD(Make Directory)
      /// The XMKD creates the specified directory on the server, and is a duplicate of the "MKD" command. 
      /// It is implemented because some firewalls assume that all FTP characters must be 4 characters in length. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=XMKD
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XMKD_Command(string cmdArguments)
      {
         MKD_Command(cmdArguments);
      }

      private static void XMKD_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XMKD");
      }
   }
}