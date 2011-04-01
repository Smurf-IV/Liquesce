using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: REST position
      /// Sets the point at which a file transfer should start; useful for resuming interrupted transfers. 
      /// For nonstructured files, this is simply a decimal number. 
      /// This command must immediately precede a data transfer command (RETR or STOR only); 
      /// i.e. it must come after any PORT or PASV command. 
      /// http://tools.ietf.org/html/rfc3659#page-14
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void REST_Command(string cmdArguments)
      {
         startOffset = 0;
         if (DataTransferEnabled)
         {
            if (int.TryParse(cmdArguments, out startOffset))
            {
               SendMessage("350 Restarting at " + startOffset + ". Send STORe or RETRieve.\r\n");
               return;
            }
         }
         SendMessage("550 Incorrect command format.\r\n");
      }


      private static void REST_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" REST STREAM\r\n");
      }
   }
}
