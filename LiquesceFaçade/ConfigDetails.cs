using System;
using System.Collections.Generic;

namespace LiquesceFaçade
{
   /// <summary>
   /// This is the class that will dump out the details to the XML File.
   /// Also marked as Serializable to allow it to be passed over the interface.
   /// </summary>
   [Serializable]
   public class ConfigDetails 
   {
      public uint DelayStartMilliSec;
      public char DriveLetter;
      public ushort ThreadCount;
      public bool DebugMode;
      public string VolumeLabel;
      public UInt64 HoldOffBufferBytes = 10L<<10<<10<<10; // ==10GB;
      public List<string> SourceLocations;
      // TODO: Extend to include encypted ACL's and things

   }
}