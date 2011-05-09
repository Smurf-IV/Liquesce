using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: CWD remote-directory
      /// Makes the given directory be the current directory on the remote host. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void CWD_Command(string cmdArguments)
      {
         try
         {
            string absPath = GetExactPath(cmdArguments);
            DirectoryInfo info = new DirectoryInfo(absPath);
            if (!info.Exists
               || ((info.Attributes & FileAttributes.System) == FileAttributes.System)
               )
               SendOnControlStream("550 System can't find directory '" + cmdArguments + "'.");
            else if (!ConnectedUser.CanViewHiddenFolders
                  && ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
               )
            {
               SendOnControlStream("550 Invalid path specified: " + cmdArguments);
            }
            else if (ConnectedUser.ChangeDirectory(absPath))
               SendOnControlStream("250 CWD command successful.");
            else
            {
               SendOnControlStream("550 Invalid path specified: " + cmdArguments);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException(string.Format("CWD Threw for[{0}]:", cmdArguments), ex);
            SendOnControlStream("550 Invalid path specified: " + ex.Message);
         }
      }

   }
}
