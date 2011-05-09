using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// used to choose the language for FTP messages
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void LANG_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void LANG_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
