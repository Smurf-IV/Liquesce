namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: MODE mode-character
      /// Sets the transfer mode to one of:
      /// * S - Stream
      /// * B - Block
      /// * C - Compressed 
      /// The default mode is Stream. 
      ///  * Z /zlib
      ///  http://www.smartftp.com/support/kb/mode-z-zlib-f192.html
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MODE_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }
      

   }
}
