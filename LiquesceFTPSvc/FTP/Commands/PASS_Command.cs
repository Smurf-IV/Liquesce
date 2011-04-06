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
            SendOnControlStream("503 Invalid User Name!");
            passed = false;
         }
         else if (ConnectedUser.Authenticate(CmdArguments))
         {
            passed = true;
            SendOnControlStream("230 Authentication Successful. Welcome to the \'Liquesce Pooling Service\'.");
         }
         else
         {
            SendOnControlStream("530 Authentication Failed!");
            passed = false;
         }
         return passed;
      }

      
   }
}
