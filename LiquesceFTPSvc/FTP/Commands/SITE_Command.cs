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
      /// SITE ALIAS, 
      /// SITE CDPATH, 
      /// SITE CHMOD, 
      /// SITE EXEC, 
      /// SITE GPASS, 
      /// SITE GROUP, 
      /// SITE GROUPS, 
      /// SITE HELP, 
      /// SITE IDLE, 
      /// SITE INDEX, 
      /// SITE MINFO, 
      /// SITE NEWER, 
      /// SITE NAMEFMT {#}
      /// SITE UMASK
      /// </remarks>
      /// <param name="cmdArguments"></param>
      private void SITE_Command(string cmdArguments)
      {
         // TODO: This is a mandatory command so will need the support it requires
         switch (cmdArguments.ToUpper())
         {
            case "NAMEFMT":
            case "NAMEFMT 1":
               SendOnControlStream("200 Now using naming format \"1\"");
               break;
            default:
               SendOnControlStream("504 Command not implemented for that parameter");
               break;
         }
      }
      
   }
}
