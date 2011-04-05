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
            SendOnControlStream("550 Access Denied.");
            return;
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path) || File.Exists(Path))
            SendOnControlStream("550 A file or folder with the same name already exists.");
         else
         {
            try
            {
               Directory.CreateDirectory(Path);
               SendOnControlStream("257 \"" + Path + "\" directory created.");
            }
            catch (Exception Ex) 
            { 
               SendOnControlStream("550 " + Ex.Message + "."); 
            }
         }
      }

   }
}
