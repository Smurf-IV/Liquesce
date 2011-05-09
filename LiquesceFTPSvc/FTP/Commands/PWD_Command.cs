using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: PWD
      /// Returns the name of the current directory on the remote host. 
      /// </summary>
      private void PWD_Command()
      {
         string pwd = Path.DirectorySeparatorChar + ConnectedUser.CurrentWorkingDirectory.Substring(ConnectedUser.StartUpDirectory.Length);
         ClientSocket.WriteAsciiInfo("257 ").WritePathNameCRLN(UseUTF8, pwd);
      }

      
   }
}
