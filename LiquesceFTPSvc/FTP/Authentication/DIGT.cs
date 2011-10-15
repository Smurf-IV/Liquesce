namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Send communications Digest
      /// </summary>
      /// http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.21.8560&rep=rep1&type=pdf
      /// <param name="cmdArguments"></param>
      private void DIGT_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void DIGT_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" DIGT");
      }
   }
}