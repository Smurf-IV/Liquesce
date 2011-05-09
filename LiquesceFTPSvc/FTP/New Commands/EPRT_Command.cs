using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// similar to PORT, but supports arbitrary address families rather than only IPv4; specifically intended for IPv6.
      /// LPRT sends addresses as an arbitrary octet string (albeit decimal encoded), 
      /// EPRT sends them as formatted strings, the format of the string being dependent upon the address format. 
      /// EPRT assumes a the use of TCP-style 16-bit port numbers, whereas LPRT is more flexible and supports transport protocols with greater than 16-bit port numbers.
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void EPRT_Command(string cmdArguments)
      {
         SendOnControlStream("502 Command Not Implemented.");
      }

      private static void EPRT_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
