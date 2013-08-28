using System;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable CheckNamespace
namespace StringBuffers
// ReSharper restore CheckNamespace
{
   /// <summary>
   /// Simple obfustication to try and stop obvious decompilation, but still use a good algorithm.
   /// </summary>
   static public class StringBuffers
   {
      // Use some strings that an application will already have in its string list.
      private static readonly byte[] key = Encoding.ASCII.GetBytes(@"System.Runtime.CompilerServices ");
      private static readonly byte[] iv = Encoding.ASCII.GetBytes(@"FileDescription ");
    
      public static string ToBuffer(this string text)
      {
         using (SymmetricAlgorithm algorithm = new RijndaelManaged())
         {
            ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
            byte[] inputbuffer = Encoding.Unicode.GetBytes(text);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Convert.ToBase64String(outputBuffer);
         }
      }

      public static string FromBuffer(this string text)
      {
         using (SymmetricAlgorithm algorithm = new RijndaelManaged())
         {
            ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
            byte[] inputbuffer = Convert.FromBase64String(text);
            byte[] outputBuffer = transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length);
            return Encoding.Unicode.GetString(outputBuffer);
         }
      }
   }
}
