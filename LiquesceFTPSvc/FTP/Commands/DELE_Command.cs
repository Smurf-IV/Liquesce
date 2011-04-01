using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: DELE remote-filename
      /// Deletes the given file on the remote host. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void DELE_Command(string CmdArguments)
      {
         string Path = GetExactPath(CmdArguments);
         Path = ConnectedUser.StartUpDirectory + Path.Substring(0, Path.Length - 1);
         try
         {
            FileInfo FI = new FileInfo(Path);
            if (FI.Exists)
            {
               if (ConnectedUser.CanDeleteFiles)
               {
                  //if (ApplicationSettings.MoveDeletedFilesToRecycleBin)
                  //{
                  //    //Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Path,
                  //    //    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                  //    //    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                  //    string RecycleBinPath = Path.Substring(0, 2) + "\\RECYCLER\\";
                  //    if (!Directory.Exists(RecycleBinPath))
                  //        Directory.CreateDirectory(RecycleBinPath);
                  //    File.Move(Path, RecycleBinPath + System.IO.Path.GetFileName(Path));
                  //}
                  //else
                  FI.Attributes = FileAttributes.Normal;
                  FI.Delete();
                  SendMessage("250 File deleted.\r\n");
               }
               else 
                  SendMessage("550 Access Denied.\r\n");
            }
            else 
               SendMessage("550 File dose not exist.\r\n");
         }
         catch (Exception Ex) 
         { 
            SendMessage("550 " + Ex.Message + ".\r\n"); 
         }
      }

 
   }
}
