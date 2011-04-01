namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// sends a command with integrity protection
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MIC_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void MIC_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
