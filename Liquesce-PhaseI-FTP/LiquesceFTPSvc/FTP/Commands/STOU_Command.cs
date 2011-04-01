namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: STOU
      /// Begins transmission of a file to the remote site; 
      /// the remote filename will be unique in the current directory. 
      /// The response from the server will include the filename. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void STOU_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      
   }
}
