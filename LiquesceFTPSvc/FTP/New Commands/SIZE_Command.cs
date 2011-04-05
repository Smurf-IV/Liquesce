using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: SIZE remote-filename 
      /// Returns the size of the remote file as a decimal number. 
      /// http://tools.ietf.org/html/rfc3659#page-11
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void SIZE_Command(string cmdArguments)
      {
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(cmdArguments);
         Path = Path.Substring(0, Path.Length - 1);
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
            SendOnControlStream("213 " + fi.Length);
         else
            SendOnControlStream("550 File does not exist.");
      }

      private static void SIZE_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" SIZE");
      }
   }
}
