using System;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: 
      /// similar to PORT, but supports arbitrary address and port formats.
      /// LPRT sends addresses as an arbitrary octet string (albeit decimal encoded), 
      /// EPRT sends them as formatted strings, the format of the string being dependent upon the address format. 
      /// EPRT assumes a the use of TCP-style 16-bit port numbers, whereas LPRT is more flexible and supports transport protocols with greater than 16-bit port numbers.
      /// http://tools.ietf.org/html/rfc1639
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void LPRT_Command(string cmdArguments)
      {
         SendOnControlStream("500 Command Not Implemented.");
      }

      private static void LPRT_Support(FTPClientCommander thisClient)
      {
         //
      }
   }
}
