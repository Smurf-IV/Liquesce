using System.Security.Cryptography;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: XSHA256 <File Name>
      /// XSHA256 <File Name>, <EP>
      /// XSHA256 <File Name>, <SP>, <EP>
      ///   SP = Starting Point in bytes (from where to start CRC calculating)
      ///   EP = Ending Point in bytes (where to stop CRC calculating) 
      /// http://help.globalscape.com/help/eft6/FileIntegrityChecking.htm
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XSHA256_Command(string cmdArguments)
      {
         string[] args = cmdArguments.Split(',');
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(args[0]);
         UseHash(args, Path.Substring(0, Path.Length - 1), new SHA256CryptoServiceProvider());
      }

      private static void XSHA256_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" XSHA256\r\n");
      }
   }
}
