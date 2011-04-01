namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: HELP [command]
      /// If a command is given, returns help on that command; otherwise, returns general help for the FTP server (usually a list of supported commands). 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void HELP_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      
   }
}
