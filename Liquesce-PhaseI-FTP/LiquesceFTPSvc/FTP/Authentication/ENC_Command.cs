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
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void ENC_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
