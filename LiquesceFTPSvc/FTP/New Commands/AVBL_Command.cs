namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// AVBL (Available Bytes In Directory)
      /// Requests the number of bytes available for upload for uploads in a specified directory, or the current directory. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=AVBL
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void AVBL_Command(string cmdArguments)
      {
         ulong freeBytesAvailable;
         ulong totalBytes;
         ulong totalFreeBytes;
         GetDiskFreeSpaceEx(GetExactPath(cmdArguments), out freeBytesAvailable, out totalBytes, out totalFreeBytes);
         SendOnControlStream("213 " + freeBytesAvailable);
      }

      private static void AVBL_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" AVBL");
      }
   }
}