namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Client / Server Identification (CSID)
      /// The CSID command is used to identify the client software to the server. 
      /// The response is used to identify the server to the client and provide important information about the server to the client. 
      /// This command supersedes the CLNT command. When the command is sent to the server, the client must identify itself:
      /// * CSID Name=FTP Voyager; Version=15.0.0.2;
      /// "Name" and "Version" are the only parameters supported. 
      /// The server responds with information helpful to the client, for example:
      /// * 200 Name=Serv-U; Version=7.0.0.5; OS=Windows XP; OSVer=5.1.2600; CaseSensitive=0; DirSep=/;
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=CSID
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void CSID_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void CSID_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" CSID");
      }
   }
}