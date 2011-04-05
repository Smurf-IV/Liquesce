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
            SendOnControlStream("503 Bad sequence of commands.");
            return;
         }

         string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

         if (Directory.Exists(Path) || File.Exists(Path))
            SendOnControlStream("550 File or folder with the same name already exists.");
         else
         {
            try
            {
               if (Directory.Exists(Rename_FilePath))
               {
                  if (ConnectedUser.CanRenameFolders) 
                  { 
                     Directory.Move(Rename_FilePath, Path); SendOnControlStream("250 Folder renamed successfully."); 
                  }
                  else 
                     SendOnControlStream("550 Access Denied.");
               }
               else if (File.Exists(Rename_FilePath))
               {
                  if (ConnectedUser.CanRenameFiles) 
                  { 
                     File.Move(Rename_FilePath, Path); SendOnControlStream("250 File renamed successfully."); 
                  }
                  else 
                     SendOnControlStream("550 Access Denied.");
               }
               else 
                  SendOnControlStream("550 Source file dose not exists.");
            }
            catch (Exception Ex) 
            { 
               SendOnControlStream("550 " + Ex.Message); 
            }
         }
         Rename_FilePath = "";
      }

 
   }
}
