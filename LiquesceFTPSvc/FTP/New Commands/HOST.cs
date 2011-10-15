namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// </summary>
      /// http://tools.ietf.org/html/draft-ietf-ftpext2-hosts-02
      /// <param name="cmdArguments"></param>
      private void HOST_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void HOST_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" HOST");
      }
   }
}