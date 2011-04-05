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
         SendOnControlStream("257 \"" + ConnectedUser.CurrentWorkingDirectory.Replace('\\', '/') + "\"");
      }

      
   }
}
