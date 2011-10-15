namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// At startup time, the client could send a new command APSV ("all passive"); 
      /// A server that implements this option would always do a passive open. 
      /// A new reply code 151 would be issued in response to all file transfer requests not preceded by a PORT or PASV command; t
      /// This message would contain the port number to use for that transfer.
      /// </summary>
      /// http://community.roxen.com/developers/idocs/rfc/rfc1579.html
      /// <param name="cmdArguments"></param>
      private void APSV_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void APSV_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" APSV");
      }
   }
}