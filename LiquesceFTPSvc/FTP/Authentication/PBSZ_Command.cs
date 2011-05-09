namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// used to negotiate maximum buffer size for encrypted data
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void PBSZ_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void PBSZ_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
