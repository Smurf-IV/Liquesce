using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// similar extension to PASV
      /// http://tools.ietf.org/html/rfc1639
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void LPSV_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void LPSV_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
