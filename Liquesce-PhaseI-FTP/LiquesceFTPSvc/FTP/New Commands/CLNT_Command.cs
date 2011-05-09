namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// The CLNT command is used to identify the client software to the server. 
      /// This command serves no functional purpose other than to provide information to the server.
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=CLNT
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void CLNT_Command(string cmdArguments)
      {
         Log.Info("Client identifying itself as: [{0}]", cmdArguments);
         SendOnControlStream("200 Thankyou");
      }

      private static void CLNT_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" CLNT");
      }
   }
}