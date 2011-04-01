namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// specifies security data specific to the chosen AUTH mechanism
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void ADAT_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

      private static void ADAT_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
