using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: ACCT account-info
      /// This command is used to send account information on systems that require it. Typically sent after a PASS command. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void ACCT_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }
   }
}
