namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// disables integrity protection for subsequent commands on control channel
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void CCC_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      private static void CCC_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
