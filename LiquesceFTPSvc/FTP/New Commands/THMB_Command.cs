namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// THMB (Thumbnail)
      /// Starts a file download where the server sends a thumbnail image of a remote file in the specified format.
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=THMB
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void THMB_Command(string cmdArguments)
      {
         // TODO: Need more info on the THMB formatting !
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void THMB_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" THMB");
      }
   }
}