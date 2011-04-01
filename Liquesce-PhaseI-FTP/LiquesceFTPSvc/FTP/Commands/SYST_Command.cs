namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: SYST
      /// Returns a word identifying the system, the word "Type:", 
      /// and the default transfer type (as would be set by the TYPE command). 
      /// For example: UNIX Type: L8 
      /// </summary>
      private void SYST_Command()
      {
         SendMessage("215 Windows_NT\r\n");
      }

      
   }
}
