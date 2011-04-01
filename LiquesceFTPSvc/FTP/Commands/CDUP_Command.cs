namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: CDUP
      /// Makes the parent of the current directory be the current directory. 
      /// </summary>
      void CDUP_Command()
      {
         string[] pathParts = ConnectedUser.CurrentWorkingDirectory.Split('\\');
         if (pathParts.Length > 1)
         {
            ConnectedUser.CurrentWorkingDirectory = "";
            for (int i = 0; i < (pathParts.Length - 2); i++)
            {
               ConnectedUser.CurrentWorkingDirectory += pathParts[i] + "\\";
            }
         }

         SendMessage("250 CDUP_Command command successful.\r\n");
      }

 
   }
}
