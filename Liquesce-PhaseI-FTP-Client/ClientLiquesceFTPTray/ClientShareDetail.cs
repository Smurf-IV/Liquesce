using System;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray
{
   [Serializable]
   public class ClientShareDetail
   {
      // ReSharper disable UnusedAutoPropertyAccessor.Global
      // ReSharper disable MemberCanBePrivate.Global

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      // Also the V 0.6 of Dokan is supposed to be able to use Mount points so this can then be reused for that..
      public string DriveLetter = @"S";

      public string TargetMachineName = @"localhost";
      public ushort Port = 21;
      public FtpSecurityProtocol SecurityProtocol = FtpSecurityProtocol.None;

      public string UserName = @"anonymous";

      public string Password = @"anonymous@Home.net";

      public string TargetShareName = @"/";

      public string VolumeLabel = @"ClientLiquesceFTP";

      // Used to send data over the wire, this is not recommended to be over int.maxvalue / 2
      // In here as different targets may have different capabilities
      // Set the minimum to be 4096 bytes
      public UInt32 BufferWireTransferSize = 8192;

      // ReSharper restore MemberCanBePrivate.Global
      // ReSharper restore UnusedAutoPropertyAccessor.Global
   }
}