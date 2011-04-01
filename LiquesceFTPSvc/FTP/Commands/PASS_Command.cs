using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: PASS password
      /// After sending the USER command, send this command to complete the login process. 
      /// (Note, however, that an ACCT command may have to be used on some systems.) 
      /// </summary>
      /// <param name="CmdArguments"></param>
      /// <returns>true if the password is correct for the USER</returns>
      private bool PASS_Command(string CmdArguments)
      {
         bool passed;
         if (String.IsNullOrEmpty(ConnectedUser.UserName))
         {
            SendMessage("503 Invalid User Name\r\n");
            passed = false;
         }
         else if (ConnectedUser.Authenticate(CmdArguments))
         {
            passed = true;
            SendMessage("230 Authentication Successful\r\n");
         }
         else
         {
            SendMessage("530 Authentication Failed!\r\n");
            passed = false;
         }
         return passed;
      }

      
   }
}
