using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax:  RNTO to-filename
      /// Used when renaming a file. 
      /// After sending an RNFR command to specify the file to rename, send this command to specify the new name for the file. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void RNTO_Command(string CmdArguments)
      {
         if (Rename_FilePath.Length == 0)
         {
            SendMessage("503 Bad sequence of commands.\r\n");
            return;
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path) || File.Exists(Path))
            SendMessage("550 File or folder with the same name already exists.\r\n");
         else
         {
            try
            {
               if (Directory.Exists(Rename_FilePath))
               {
                  if (ConnectedUser.CanRenameFolders) 
                  { 
                     Directory.Move(Rename_FilePath, Path); SendMessage("250 Folder renamed successfully.\r\n"); 
                  }
                  else 
                     SendMessage("550 Access Denied.\r\n");
               }
               else if (File.Exists(Rename_FilePath))
               {
                  if (ConnectedUser.CanRenameFiles) 
                  { 
                     File.Move(Rename_FilePath, Path); SendMessage("250 File renamed successfully.\r\n"); 
                  }
                  else 
                     SendMessage("550 Access Denied.\r\n");
               }
               else 
                  SendMessage("550 Source file dose not exists.\r\n");
            }
            catch (Exception Ex) 
            { 
               SendMessage("550 " + Ex.Message + ".\r\n"); 
            }
         }
         Rename_FilePath = "";
      }

 
   }
}
