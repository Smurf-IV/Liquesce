using System.IO;
using System.Runtime.InteropServices;

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
         ulong freeBytesAvailable;
         ulong totalBytes;
         ulong totalFreeBytes;
         GetDiskFreeSpaceEx(ConnectedUser.StartUpDirectory, out freeBytesAvailable, out totalBytes, out totalFreeBytes);
         SendOnControlStream(string.Format("202-freeBytesAvailable[{0}], totalBytes[{1}], totalFreeBytes[{2}]", freeBytesAvailable, totalBytes, totalFreeBytes));
         SendOnControlStream("202 Command not implemented, superfluous at this site.");
      }

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable,
         out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

   }
}
