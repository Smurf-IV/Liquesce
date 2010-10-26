using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LiquesceFacade
{
   [DataContract]
   public class ClientShareDetails
   {
      [DataMember(IsRequired = true)]
      public string DomainUserIdentity;
      [DataMember(IsRequired = true)]
      public string TargetPath; // *** Strip off trailing backslash - it isn't supported

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      // Also the V 0.6 of Dokan is supposed to be able to use Mount points so this can then be reused for that..
      [DataMember(IsRequired = true)]
      public string DriveLetter;

      [DataMember(IsRequired = true)]
      public string VolumeLabel;
   }

   /// <summary>
   /// Class used by the Client service to enable the shares to be found an a per user basis.
   /// </summary>
   [DataContract]
   public class ClientConfigDetails
   {
      [DataMember]
      public List<ClientShareDetails> SharesToRestore = new List<ClientShareDetails>();

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
