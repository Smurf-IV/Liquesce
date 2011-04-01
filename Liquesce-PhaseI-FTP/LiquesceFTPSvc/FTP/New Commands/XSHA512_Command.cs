using System.Security.Cryptography;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: XSHA512 <File Name>
      /// XSHA512 <File Name>, <EP>
      /// XSHA512 <File Name>, <SP>, <EP>
      ///   SP = Starting Point in bytes (from where to start CRC calculating)
      ///   EP = Ending Point in bytes (where to stop CRC calculating) 
      /// http://help.globalscape.com/help/eft6/FileIntegrityChecking.htm
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void XSHA512_Command(string cmdArguments)
      {
         string[] args = cmdArguments.Split(',');
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(args[0]);
         UseHash(args, Path.Substring(0, Path.Length - 1), new SHA512CryptoServiceProvider());
      }

      private static void XSHA512_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" XSHA512\r\n");
      }
   }
}
