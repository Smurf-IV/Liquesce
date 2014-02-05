﻿using System;
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
         string Path = GetExactPath(cmdArguments);
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
         {
            StringBuilder sb = new StringBuilder();
            using (FileStream fs = fi.OpenRead())
            {
               MD5 md5 = new MD5CryptoServiceProvider();
               foreach (byte hex in md5.ComputeHash(fs))
                  sb.Append(hex.ToString("x2"));
               SendOnControlStream(String.Format("213 MD5 0-{0} {1} {2}", fi.Length, sb.ToString(), fi.Name));
            }
         }
         else
            SendOnControlStream("550 File does not exist.");
      }

      private static void HASH_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" HASH MD5*");
      }
   }
}