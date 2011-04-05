using System.Net.Sockets;
using System.Text;

namespace LiquesceFTPSvc.FTP
{
   internal static class UTF8PathNames
   {
      private static readonly byte[] CRLN = Encoding.UTF8.GetBytes("\r\n");
      public static void WriteCRLN(this NetworkStream stream)
      {
         stream.WriteBuffer(CRLN);
      }

      public static void WriteInfoCRLN(this NetworkStream stream, string info)
      {
         stream.WriteInfo(info);
         stream.WriteCRLN();
      }

      public static void WritePathNameCRLN(this NetworkStream stream, bool useUTF8, string name)
      {
         stream.WritePathName(useUTF8, name);
         stream.WriteCRLN();
      }

      internal static void WriteBuffer(this NetworkStream stream, byte[] buffer)
      {
         stream.Write(buffer, 0, buffer.Length);
      }

      internal static void WriteInfo(this NetworkStream stream, string info)
      {
         stream.WriteBuffer(Encoding.UTF8.GetBytes(info));
      }

      internal static void WritePathName(this NetworkStream stream, bool useUTF8, string name)
      {
         stream.WriteBuffer(useUTF8 ? Encoding.UTF8.GetBytes(name) : Encoding.ASCII.GetBytes(name));
      }
   }
}
