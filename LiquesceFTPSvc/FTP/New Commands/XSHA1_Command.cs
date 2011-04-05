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
      /// <param name="cmdArguments"></param>
      private void XSHA1_Command(string cmdArguments)
      {
         string[] args = cmdArguments.Split(',');
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(args[0]);
         UseHash(args, Path.Substring(0, Path.Length - 1), new SHA1CryptoServiceProvider());
      }

      private static void XSHA1_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" XSHA1");
      }
   }
}
