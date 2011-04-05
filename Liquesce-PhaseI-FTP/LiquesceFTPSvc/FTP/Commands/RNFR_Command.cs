using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax:  RNFR from-filename
      /// Used when renaming a file. Use this command to specify the file to be renamed; 
      /// follow it with an RNTO command to specify the new name for the file. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void RNFR_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanRenameFiles) 
         { 
            SendOnControlStream("550 Access Denied."); 
            return; 
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path) || File.Exists(Path))
         {
            Rename_FilePath = Path;
            SendOnControlStream("350 Please specify destination name.");
         }
         else 
            SendOnControlStream("550 File or directory doesn't exist.");
      }

 
   }
}
