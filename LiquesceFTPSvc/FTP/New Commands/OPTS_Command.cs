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
               // http://tools.ietf.org/html/draft-ietf-ftpext-utf-8-option-00
               // Through use of this option, the user informs the server of its willingness to accept UTF-8 encoded pathnames.
               SendOnControlStream("200 UTF-8 is on");
               UseUTF8 = true;
               break;
            default:
               SendOnControlStream("501 [" + cmdArguments +"] Not Implemented.");
               break;
         }
      }

      private static void OPTS_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" UTF8");
      }
   }
}
