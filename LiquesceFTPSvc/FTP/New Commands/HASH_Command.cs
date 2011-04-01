using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: HASH <File Name>
      /// http://tools.ietf.org/html/draft-ietf-ftpext2-hash-01
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void HASH_Command(string cmdArguments)
      {
         string[] args = cmdArguments.Split(',');
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(args[0]);
         Path = Path.Substring(0, Path.Length - 1);
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
         {
            StringBuilder sb = new StringBuilder();
            using (FileStream fs = fi.OpenRead())
            {
               MD5 md5 = new MD5CryptoServiceProvider();
               foreach (byte hex in md5.ComputeHash(fs))
                  sb.Append(hex.ToString("x2"));
               SendMessage(String.Format("213 MD5 0-{0} {1} {2}\r\n", fi.Length, sb.ToString(), fi.Name));
            }
         }
         else
            SendMessage("550 File does not exist.\r\n");
      }

      private static void HASH_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" HASH MD5*\r\n");
      }
   }
}
