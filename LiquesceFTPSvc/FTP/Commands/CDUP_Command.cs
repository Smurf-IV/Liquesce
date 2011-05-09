namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: CDUP
      /// Makes the parent of the current directory be the current directory. 
      /// </summary>
      void CDUP_Command()
      {
         CWD_Command("..");
      }

 
   }
}
