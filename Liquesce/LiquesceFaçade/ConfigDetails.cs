using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace LiquesceFaçade
{
   /// <summary>
   /// This is the class that will dump out the details to the XML File.
   /// </summary>
   [DataContract]
   public class ConfigDetails 
   {
      [DataMember(IsRequired = true)]
      public uint DelayStartMilliSec = (uint) short.MaxValue;
      
      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      // Also the V 0.6 of Dokan is supposed to be able to use Mount points so this can then be reused for that..
      [DataMember(IsRequired = true)]
      public string DriveLetter ="N";
      
      [DataMember]
      public ushort ThreadCount = 5;
      
      [DataMember]
      public int LockTimeout = short.MaxValue; // Useful if you are getting locks in the multiple threads - Can be set to -1 for infinite
      
      [DataMember]
      public bool DebugMode = false;
      
      [DataMember(IsRequired = true)]
      public string VolumeLabel = "Demo";
      
      [DataMember]
      public UInt64 HoldOffBufferBytes = 1L << 10 << 10 << 10; // ==1GB;
      
      [DataMember]
      public UInt32 BufferReadSize = 4 << 10;   // == 4K Standard OS build block size
      
      [DataMember(IsRequired = true)]
      public List<string> SourceLocations;

      [DataMember]
      public string ServiceLogLevel = "Debug"; // NLog's LogLevel.Debug.ToString()

      public List<string> KnownSharePaths; // Not exported
   }
}