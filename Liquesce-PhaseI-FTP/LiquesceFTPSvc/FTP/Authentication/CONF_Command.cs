namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// sends a command with confidentiality protection
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void CONF_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void CONF_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
