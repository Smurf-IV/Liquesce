using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: SMNT 
      /// mount a different file system or volume. Intended for systems such as DOS or VMS where there is a distinction 
      /// between volume and directory in pathnames; but commonly unimplemented even on such systems.
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void SMNT_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }
   }
}
