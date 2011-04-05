using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: STRU structure-character
      /// Sets the file structure for transfer to one of:
      /// * F - File (no structure)
      /// * R - Record structure
      /// * P - Page structure 
      /// The default structure is File. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void STRU_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }
   }
}
