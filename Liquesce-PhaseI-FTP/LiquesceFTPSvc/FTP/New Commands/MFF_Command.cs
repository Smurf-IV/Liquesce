namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// mff                = "MFF" [ mff-facts ] SP pathname CRLF
      /// mff-facts          = 1*( mff-fact ";" )
      /// mff-fact           = mff-standardfact / mff-osfact / mff-localfact
      /// mff-standardfact   = mff-createtimefact / mff-modifytimefact
      /// mff-createtimefact = "Create" "=" time-val
      /// mff-modifytimefact = "Modify" "=" time-val
      /// mff-osfact         = <IANA assigned OS name> "." token "=" *SCHAR
      /// mff-localfact      = "X." token "=" *SCHAR
      /// http://www.omz13.com/downloads/draft-somers-ftp-mfxx-02.html#MFF
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MFF_Command(string cmdArguments)
      {
         // TODO: Can I Be bothered :-)
         SendOnControlStream("500 Command Not Implemented.");
      }

      private static void MFF_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" MFF Create;Modify;");
      }
   }
}
