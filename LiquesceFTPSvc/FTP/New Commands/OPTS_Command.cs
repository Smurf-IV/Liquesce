using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// a generic mechanism for the client to specify options to arbitrary FTP commands
      /// http://tools.ietf.org/html/rfc2389#page-6
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void OPTS_Command(string cmdArguments)
      {
         switch (cmdArguments.ToLower())
         {
            case "utf8 on":
            case "utf-8 on":
               SendMessage("200 UTF-8 is on\r\n");
               UseUTF8 = true;
               break;
            default:
               SendMessage("501 [" + cmdArguments +"] Not Implemented.\r\n");
               break;
         }
      }

      private static void OPTS_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" UTF8\r\n");
      }
   }
}
