namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax:  USER username
      /// Send this command to begin the login process. 
      /// username should be a valid username on the system, or "anonymous" to initiate an anonymous login. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      private void USER_Command(string CmdArguments)
      {
         if (!string.IsNullOrEmpty(CmdArguments))
         {
            SendMessage("331 Password required!\r\n");
            ConnectedUser.LoadProfile(CmdArguments.ToUpper());
         }
      }

   }
}
