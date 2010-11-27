using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LiquesceFacade
{
   [DataContract]
   public class ClientShareDetail
   {
      [DataMember(IsRequired = true)]
      public string TargetMachineName = "localhost";

      [DataMember(IsRequired = true)]
      public string DomainUserIdentity = "Everyone";
      [DataMember(IsRequired = true)]
      public string TargetShareName = "Dokan";

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      // Also the V 0.6 of Dokan is supposed to be able to use Mount points so this can then be reused for that..
      [DataMember(IsRequired = true)]
      public string DriveLetter = "S";

      [DataMember(IsRequired = true)]
      public string VolumeLabel = "LShare";

      // Used to send data over the wire, this is not recommended to be over int.maxvalue / 2
      // In here as different targets may have different capabilities
      // Set the minimum to be 4096 bytes
      [DataMember]
      public int BufferWireTransferSize = UInt16.MaxValue;

      // This will make the drive show up as a network share in explorer
      // And prevent recycle bin and other shares from being created on it.
      [DataMember]
      public bool ShowAsNetworkDrive = true;

   }

   /// <summary>
   /// Class used by the Client service to enable the shares to be found an a per user basis.
   /// </summary>
   [DataContract]
   public class ClientConfigDetails
   {
      [DataMember]
      public List<ClientShareDetail> SharesToRestore = new List<ClientShareDetail>();

      #region Dokan Specific
      [DataMember]
      public ushort ThreadCount = 1;
      [DataMember]
      public bool DebugMode = false;
      [DataMember]
      public string ServiceLogLevel = "Debug"; // NLog's LogLevel.Debug.ToString() 
      #endregion
   }
}
