namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// sends a command with both integrity and confidentiality protection
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void ENC_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void ENC_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
