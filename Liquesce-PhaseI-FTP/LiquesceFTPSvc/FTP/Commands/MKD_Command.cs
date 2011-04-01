using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: MKD remote-directory
      /// Creates the named directory on the remote host. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void MKD_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanStoreFolder)
         {
            SendMessage("550 Access Denied.\r\n");
            return;
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path) || File.Exists(Path))
            SendMessage("550 A file or folder with the same name already exists.\r\n");
         else
         {
            try
            {
               Directory.CreateDirectory(Path);
               SendMessage("257 \"" + Path + "\" directory created.\r\n");
            }
            catch (Exception Ex) { SendMessage("550 " + Ex.Message + ".\r\n"); }
         }
      }

   }
}
