namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// specifies protection level for data channel. Following levels are defined:
      /// * C (Clear) – data channel is subject neither to encryption nor integrity protection
      /// * S (Safe) – integrity protection applied to data channel
      /// * E (Confidential) – encryption applied to data channel
      /// * P (Private) – both encryption and integrity protection applied to data channel
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void PROT_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      private static void PROT_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
