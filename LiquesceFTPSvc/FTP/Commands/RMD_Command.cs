using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: RMD remote-directory
      /// Deletes the named directory on the remote host. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void RMD_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanDeleteFolders)
         {
            SendMessage("550 Access Denied.\r\n");
            return;
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path))
         {
            try
            {
               Directory.Delete(Path, true);
               SendMessage("250 \"" + Path + "\" deleted.\r\n");
            }
            catch (Exception Ex) { SendMessage("550 " + Ex.Message + ".\r\n"); }
         }
         else SendMessage("550 Folder dose not exist.\r\n");
      }

   }
}
