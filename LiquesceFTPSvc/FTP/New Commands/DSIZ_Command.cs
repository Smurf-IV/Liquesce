namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// DSIZ (Directory Size)
      /// Requests the size of a specified directory, or the current directory, including subfolders. 
      /// http://www.rhinosoft.com/respcode.asp?Prod=su&cmd=DSIZ
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void DSIZ_Command(string cmdArguments)
      {
         ulong freeBytesAvailable;
         ulong totalBytes;
         ulong totalFreeBytes;
         GetDiskFreeSpaceEx(GetExactPath(cmdArguments), out freeBytesAvailable, out totalBytes, out totalFreeBytes);
         SendOnControlStream("213 " + totalBytes);
      }

      private static void DSIZ_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" DSIZ");
      }
   }
}