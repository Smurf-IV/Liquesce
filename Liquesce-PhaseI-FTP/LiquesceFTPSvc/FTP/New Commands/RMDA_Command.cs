namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// RMDA (Remove Directory & Contents)
      /// Removes the specified directory and all its file contents and subdirectories on the server. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=RMDA
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void RMDA_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void RMDA_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" RMDA");
      }
   }
}