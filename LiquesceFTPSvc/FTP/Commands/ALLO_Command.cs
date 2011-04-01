namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: ALLO size [R max-record-size]
      /// Allocates sufficient storage space to receive a file. If the maximum size of a record also needs to be known, 
      /// that is sent as a second numeric parameter following a space, the capital letter "R", and another space. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void ALLO_Command(string cmdArguments)
      {
         SendMessage("500 Command Not Implemented.\r\n");
      }

   }
}
