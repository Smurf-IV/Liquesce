using System;
using System.IO;
using System.Runtime.InteropServices;
using DeleteToRecycleBin;

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
                  FI.Attributes = FileAttributes.Normal;
                  if (Settings1.Default.MoveDeletedFilesToRecycleBin)
                  {
                     RecybleBin.SendSilent(FI.FullName); 
                  }
                  else
                  {
                     FI.Delete();
                  }
                  SendOnControlStream("250 File deleted.");
               }
               else 
                  SendOnControlStream("550 Access Denied.");
            }
            else 
               SendOnControlStream("550 File dose not exist.");
         }
         catch (Exception Ex) 
         { 
            SendOnControlStream("550 " + Ex.Message + "."); 
         }
      }


      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
      public struct SHFILEOPSTRUCT
      {
         public IntPtr hwnd;
         [MarshalAs(UnmanagedType.U4)]
         public int wFunc;
         public string pFrom;
         public string pTo;
         public short fFlags;
         [MarshalAs(UnmanagedType.Bool)]
         public bool fAnyOperationsAborted;
         public IntPtr hNameMappings;
         public string lpszProgressTitle;

      }

      [DllImport("shell32.dll", CharSet = CharSet.Auto)]
      static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
      const int FO_DELETE = 3;
      const int FOF_ALLOWUNDO = 0x40;
      const int FOF_NOCONFIRMATION = 0x10;    //No prompt dialogs 


   }
}
