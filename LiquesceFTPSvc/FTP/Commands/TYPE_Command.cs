namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: TYPE type-character [second-type-character]
      /// Sets the type of file to be transferred. type-character can be any of:
      /// * A - ASCII text
      /// * E - EBCDIC text
      /// * I - image (binary data)
      /// * L - local format 
      /// For A and E, the second-type-character specifies how the text should be interpreted. It can be:
      /// * N - Non-print (not destined for printing). This is the default if second-type-character is omitted.
      /// * T - Telnet format control (<CR>, <FF>, etc.)
      /// * C - ASA Carriage Control 
      /// For L, the second-type-character specifies the number of bits per byte on the local system, and may not be omitted. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void TYPE_Command(string CmdArguments)
      {
         if ((CmdArguments = CmdArguments.Trim().ToUpper()) == "A" || CmdArguments == "I")
            SendMessage("200 Type " + CmdArguments + " Accepted.\r\n");
         else 
            SendMessage("500 Unknown Type.\r\n");
      }

   }
}
