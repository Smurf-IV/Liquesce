namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: AUTH TLS
      /// identifies the authentication/security mechanism to be used
      /// Explicit FTPS is an extension to the FTP standard that allows clients to request that the FTP session be encrypted. 
      /// This is done by sending the "AUTH TLS" command. 
      /// The server has the option of allowing or denying connections that do not request TLS. 
      /// This protocol extension is defined in the proposed standard: RFC 4217.
      /// https://secure.wikimedia.org/wikipedia/en/wiki/FTPS#Explicit
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void AUTH_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void AUTH_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
