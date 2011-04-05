using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// similar extension to PASV
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void EPSV_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      private static void EPSV_Support(FTPClientCommander thisClient)
      {
         // 
      }
   }
}
