using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: XCRC <File Name>
      /// XCRC <File Name>, <EP>
      /// XCRC <File Name>, <SP>, <EP>
      ///   SP = Starting Point in bytes (from where to start CRC calculating)
      ///   EP = Ending Point in bytes (where to stop CRC calculating) 
      /// http://help.globalscape.com/help/eft6/FileIntegrityChecking.htm
      /// </summary>
      /// <example>
      /// FTP Client Log Example 
      /// COMMAND:> XCRC "/Program Files/MSN Gaming Zone/Windows/chkrzm.exe" 0 42575 
      /// </example>
      /// <param name="cmdArguments"></param>
      private void XCRC_Command(string cmdArguments)
      {
         // TODO: Sort out usage of UTF8 and the quotes etc.
         string[] args = cmdArguments.Split(',');
         UseHash(args, GetExactPath(args[0]), new Crc32());
      }

      private static void XCRC_Support(FTPClientCommander thisClient)
      {
         // thisClient.SendOnControlStream(" XCRC");
      }

      private void UseHash(IList<string> args, string Path, HashAlgorithm hashAlgorithm)
      {
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
         {
            StringBuilder hash = new StringBuilder();
            using (FileStream fs = fi.OpenRead())
            {
               Stream inputStream = fs;
               int start = 0;
               int end = 0;
               if (args.Count == 2)
               {
                  if (!int.TryParse(args[1], out end))
                  {
                     SendOnControlStream("550 Requested action not taken: EP is incorrect");
                     return;
                  }
               }
               if (args.Count > 2)
               {
                  if (!int.TryParse(args[1], out start))
                  {
                     SendOnControlStream("550 Requested action not taken: SP is incorrect");
                     return;
                  }
                  if (int.TryParse(args[2], out end))
                  {
                     if ((start >= end)
                         || (end >= fs.Length)
                        )
                     {
                        SendOnControlStream("550 Requested action not taken: EP is incorrect");
                        return;
                     }
                  }
               }
               if (args.Count > 1)
               {
                  int numBytes = end - start;
                  MemoryStream memstream = new MemoryStream(numBytes);
                  fs.Read(memstream.GetBuffer(), start, numBytes);
                  memstream.Position = 0;
                  inputStream = memstream;
               }
               foreach (byte hex in hashAlgorithm.ComputeHash(inputStream))
               {
                  hash.Append(hex.ToString("x2"));
               }
            }
            SendOnControlStream("213 " + hash.ToString());
         }
         else
            SendOnControlStream("550 File does not exist.");
      }

   }


   /// <summary>
   /// Stolen from http://damieng.com/blog/2006/08/08/calculating_crc32_in_c_and_net
   /// </summary>
   internal sealed class Crc32 : HashAlgorithm
   {
      private const UInt32 DefaultPolynomial = 0xedb88320;
      private const UInt32 DefaultSeed = 0xffffffff;

      private UInt32 hash;
      private readonly UInt32 seed;
      private readonly UInt32[] table;
      private static UInt32[] defaultTable;

      public Crc32()
      {
         table = InitializeTable(DefaultPolynomial);
         seed = DefaultSeed;
         Initialize();
      }

      public Crc32(UInt32 polynomial, UInt32 seed)
      {
         table = InitializeTable(polynomial);
         this.seed = seed;
         Initialize();
      }

      public override void Initialize()
      {
         hash = seed;
      }

      protected override void HashCore(byte[] buffer, int start, int length)
      {
         hash = CalculateHash(table, hash, buffer, start, length);
      }

      protected override byte[] HashFinal()
      {
         byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
         HashValue = hashBuffer;
         return hashBuffer;
      }

      public override int HashSize
      {
         get { return 32; }
      }

      public static UInt32 Compute(byte[] buffer)
      {
         return ~CalculateHash(InitializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
      }

      public static UInt32 Compute(UInt32 seed, byte[] buffer)
      {
         return ~CalculateHash(InitializeTable(DefaultPolynomial), seed, buffer, 0, buffer.Length);
      }

      public static UInt32 Compute(UInt32 polynomial, UInt32 seed, byte[] buffer)
      {
         return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
      }

      private static UInt32[] InitializeTable(UInt32 polynomial)
      {
         if (polynomial == DefaultPolynomial && defaultTable != null)
            return defaultTable;

         UInt32[] createTable = new UInt32[256];
         for (int i = 0; i < 256; i++)
         {
            UInt32 entry = (UInt32)i;
            for (int j = 0; j < 8; j++)
               if ((entry & 1) == 1)
                  entry = (entry >> 1) ^ polynomial;
               else
                  entry = entry >> 1;
            createTable[i] = entry;
         }

         if (polynomial == DefaultPolynomial)
            defaultTable = createTable;

         return createTable;
      }

      private static UInt32 CalculateHash(UInt32[] table, UInt32 seed, byte[] buffer, int start, int size)
      {
         UInt32 crc = seed;
         for (int i = start; i < size; i++)
            unchecked
            {
               crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
            }
         return crc;
      }

      private byte[] UInt32ToBigEndianBytes(UInt32 x)
      {
         return new byte[] {
			(byte)((x >> 24) & 0xff),
			(byte)((x >> 16) & 0xff),
			(byte)((x >> 8) & 0xff),
			(byte)(x & 0xff)
		};
      }
   }
}
