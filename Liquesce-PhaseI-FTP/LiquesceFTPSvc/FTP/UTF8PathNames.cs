using System.Net.Sockets;
using System.Text;
using NLog;

namespace LiquesceFTPSvc.FTP
{
   internal static class UTF8PathNames
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      private static readonly byte[] CRLN = Encoding.ASCII.GetBytes("\r\n");

      private static void WriteCRLN(this NetworkStream stream)
      {
         stream.WriteBuffer(CRLN);
      }

      public static void WriteAsciiInfoCRLN(this NetworkStream stream, string info)
      {
         stream.WriteAsciiInfo(info);
         stream.WriteCRLN();
      }

      public static void WritePathNameCRLN(this NetworkStream stream, bool useUTF8, string name)
      {
         stream.WritePathName(useUTF8, name);
         stream.WriteCRLN();
      }

      private static void WriteBuffer(this NetworkStream stream, byte[] buffer)
      {
         stream.Write(buffer, 0, buffer.Length);
      }

      internal static NetworkStream WriteAsciiInfo(this NetworkStream stream, string info)
      {
         stream.WriteBuffer(Encoding.ASCII.GetBytes(info));
         return stream;
      }

      private static void WritePathName(this NetworkStream stream, bool useUTF8, string name)
      {
         if (Log.IsTraceEnabled)
            Log.Trace(useUTF8 ? Encoding.UTF8.GetBytes(name) : Encoding.ASCII.GetBytes(name));
         stream.WriteBuffer(useUTF8 ? Encoding.UTF8.GetBytes(name) : Encoding.ASCII.GetBytes(name));
      }
   }
}
