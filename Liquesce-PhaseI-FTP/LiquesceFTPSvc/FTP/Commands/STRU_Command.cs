namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: STRU structure-character
      /// Sets the file structure for transfer to one of:
      /// * F - File (no structure)
      /// * R - Record structure
      /// * P - Page structure 
      /// The default structure is File. 
      /// Returns
      /// 200 - The command completed successfully.
      /// 421 - The server is off-line or is going off-line.
      /// 504 - Command not implemented for that parameter.
      /// 530 - No user is currently authenticated on the command channel.
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void STRU_Command(string cmdArguments)
      {
         // TODO: This is a mandatory command so will need the support it requires
         // http://www.freesoft.org/CIE/RFC/959/14.htm
         SendOnControlStream( (cmdArguments.ToUpper()!="F") ? 
            "504 - Command not implemented for that parameter."
            :"200 - The command completed successfully.");
         // The use of record structures is not mandatory. 
         // A user with no record structure in his file should be able to store and retrieve his file at any HOST.
         // A user wishing to transmit a record structured file must send the appropriate FTP 'STRU' command (the default assumption is no record structure). 
         // A serving HOST need not accept record structures, but it must inform the user of this fact by sending an appropriate reply. 
         // Any record structure information in the data stream may subsequently be discarded by the receiver.
      }
   }
}
