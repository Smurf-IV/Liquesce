using System.IO;

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
         string Path = GetExactPath(cmdArguments);
         FileInfo info = new FileInfo(Path);
         if (info.Exists)
         {
            if (((info.Attributes & FileAttributes.System) == FileAttributes.System)
                || (!ConnectedUser.CanViewHiddenFolders
                    && ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                   )
               )
            {
               SendOnControlStream("550 Invalid File given");
            }
            else
            {
               SendOnControlStream("213 " + info.Length);
            }
         }
         else
            SendOnControlStream("550 File does not exist.");
      }

      private static void SIZE_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" SIZE");
      }
   }
}
