namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: CWD remote-directory
      /// Makes the given directory be the current directory on the remote host. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      private void CWD_Command(string CmdArguments)
      {
         string dir = GetExactPath(CmdArguments);

         if (ConnectedUser.ChangeDirectory(dir))
            SendOnControlStream("250 CWD command successful.");
         else 
            SendOnControlStream("550 System can't find directory '" + dir + "'.");
      }

   }
}
