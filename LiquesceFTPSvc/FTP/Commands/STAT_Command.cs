namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: STAT [remote-filespec]
      /// If invoked without parameters, returns general status information about the FTP server process.
      /// If a parameter is given, acts like the LIST command, except that data is sent over the control connection (no PORT or PASV command is required). 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void STAT_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      
   }
}
