using System.Security.Cryptography;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: XSHA1 <File Name>
      /// XSHA1 <File Name>, <EP>
      /// XSHA1 <File Name>, <SP>, <EP>
      ///   SP = Starting Point in bytes (from where to start CRC calculating)
      ///   EP = Ending Point in bytes (where to stop CRC calculating) 
      /// http://help.globalscape.com/help/eft6/FileIntegrityChecking.htm
      /// </summary>
      /// <example>
      /// FTP Client Log Example 
      /// COMMAND:> XCRC "/Program Files/MSN Gaming Zone/Windows/chkrzm.exe" 0 42575 
      /// </example>
      /// <param name="cmdArguments"></param>
      private void XSHA1_Command(string cmdArguments)
      {
         // TODO: Sort out usage of UTF8 and the quotes etc.
         string[] args = cmdArguments.Split(',');
         UseHash(args, GetExactPath(args[0]), new SHA1CryptoServiceProvider());
      }

      private static void XSHA1_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" XSHA1");
      }
   }
}
