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
      /// <remarks>
      /// Some abuses of Site:
      /// http://forum.filezilla-project.org/viewtopic.php?f=1&t=15686
      /// SITE UTIME test.txt 20080825033443 20080825033443 20080825033443 UTC
      /// SITE UTIME 20071206044230 Ha293E_IC434_02789.fit
      /// </remarks>
      /// <param name="cmdArguments"></param>
      private void SITE_Command(string cmdArguments)
      {
         // TODO: This is a mandatory command so will need the support it requires
         SendOnControlStream("500 Command Not Implemented.");
      }
      
   }
}
