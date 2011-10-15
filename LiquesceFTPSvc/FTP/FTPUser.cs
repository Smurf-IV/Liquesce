using System;
using System.IO;
using NLog;

namespace LiquesceFTPSvc.FTP
{
   class FTPUser
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      internal bool CanDeleteFiles, CanDeleteFolders, CanRenameFiles,
           CanRenameFolders, CanStoreFiles, CanStoreFolder,
           CanViewHiddenFolders, CanCopyFiles;

      internal string UserName;
      // TODO: This needs some serious thought on how to populate this directory
      internal string StartUpDirectory = @"C:\Blam";
      internal string CurrentWorkingDirectory = @"C:\Blam";
      internal bool IsAuthenticated = false;
      string Password;

      internal void LoadProfile(string userName)
      {
         try
         {
            if (userName == this.UserName)
               return;
            if ((this.UserName = userName).Length == 0)
               return;
            IsAuthenticated = false;
            // TODO: Add user list
            CanStoreFiles = true;
            CanStoreFolder = true;
            CanRenameFiles = true;
            CanRenameFolders = true;
            CanDeleteFiles = true;
            CanDeleteFolders = true;
            CanCopyFiles = true;
            CanViewHiddenFolders = true;

            //XmlNodeList Users = ApplicationSettings.GetUserList();

            //foreach (XmlNode User in Users)
            //{
            //    if (User.Attributes[0].Value != userName) continue;

            //    password = User.Attributes[1].Value;
            //    StartUpDirectory = User.Attributes[2].Value;

            //    char[] Permissions = User.Attributes[3].Value.ToCharArray();

            //    CanStoreFiles = Permissions[0] == '1';
            //    CanStoreFolder = Permissions[1] == '1';
            //    CanRenameFiles = Permissions[2] == '1';
            //    CanRenameFolders = Permissions[3] == '1';
            //    CanDeleteFiles = Permissions[4] == '1';
            //    CanDeleteFolders = Permissions[5] == '1';
            //    CanCopyFiles = Permissions[6] == '1';                    
            //    CanViewHiddenFolders = Permissions[8] == '1';

            //    break;
            //}
         }
         catch (Exception Ex)
         {
            Log.ErrorException("LoadProfile", Ex);
         }
      }

      internal bool Authenticate(string password)
      {
         // TODO Remove this bypass
         return (IsAuthenticated = true);

         return (IsAuthenticated = (password == Password));
      }

      internal bool ChangeDirectory(string absPath)
      {
         bool success = false;
         try
         {
            if (absPath.StartsWith(StartUpDirectory))
            {
               // need to leave the start seperator
               CurrentWorkingDirectory = absPath;
               success = true;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("ChangeDirectory -> CurrentWorkingDirectory;", ex );
            success = false;
         }
         return success;
      }
   }
}
