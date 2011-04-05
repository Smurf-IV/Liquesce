using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: SITE site-specific-command
      /// Executes a site-specific command. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void SITE_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }
      
   }
}
