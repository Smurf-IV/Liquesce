using System.Security.Cryptography;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: XMD5 <File Name>
      /// XMD5 <File Name>, <EP>
      /// XMD5 <File Name>, <SP>, <EP>
      ///   SP = Starting Point in bytes (from where to start CRC calculating)
      ///   EP = Ending Point in bytes (where to stop CRC calculating) 
      /// http://help.globalscape.com/help/eft6/FileIntegrityChecking.htm
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XMD5_Command(string cmdArguments)
      {
         string[] args = cmdArguments.Split(',');
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(args[0]);
         UseHash(args, Path.Substring(0, Path.Length - 1), new MD5CryptoServiceProvider());
      }

      private static void XMD5_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" XMD5\r\n");
      }
   }
}
