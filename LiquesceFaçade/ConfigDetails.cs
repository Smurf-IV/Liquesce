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
      public string DriveLetter; // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      public ushort ThreadCount;
      public int LockTimeout = short.MaxValue; // Useful if you are getting locks in the multiple threads - Can be set to -1 for infinite
      public bool DebugMode;
      public string VolumeLabel;
      public UInt64 HoldOffBufferBytes = 1L<<10<<10<<10; // ==1GB;
      public UInt32 BufferReadSize = 4 << 10;   // == 4K Standard OS build block size
      public List<string> SourceLocations;
      // TODO: Extend to include encypted ACL's and things

   }
}