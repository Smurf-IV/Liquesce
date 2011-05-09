using System;
using System.IO;
using DeleteToRecycleBin;

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
            SendOnControlStream("550 Access Denied.");
            return;
         }

         string Path = GetExactPath(CmdArguments);

         DirectoryInfo DI = new DirectoryInfo(Path);
         if (DI.Exists)
         {
            try
            {
               if (Settings1.Default.MoveDeletedFilesToRecycleBin)
               {
                  RecybleBin.SendSilent(DI.FullName);
               }
               else
               {
                  DI.Delete(true);
               }
               SendOnControlStream("250 \"" + Path + "\" deleted.");
            }
            catch (Exception Ex) 
            { 
               SendOnControlStream("550 " + Ex.Message); 
            }
         }
         else SendOnControlStream("550 Folder dose not exist.");
      }

   }
}
