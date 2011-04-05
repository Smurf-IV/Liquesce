namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: APPE remote-filename
      /// Append data to the end of a file on the remote host. If the file does not already exist, it is created. 
      /// This command must be preceded by a PORT or PASV command so that the server knows where to receive data from. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void APPE_Command(string CmdArguments)
      {
         // Append the file if exists or create a new file.
         SendOnControlStream("500 This functionality is currently Unavailable.");
      }

 
   }
}
