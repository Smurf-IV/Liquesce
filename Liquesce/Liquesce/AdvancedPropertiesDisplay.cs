using System;
using System.ComponentModel;
using LiquesceFaçade;

namespace Liquesce
{
   //AdvancedConfigDetails.LockTimeout = (int) LockTimeoutmSec.Value;
   //AdvancedConfigDetails.DebugMode = DokanDebugMode.Checked;
   //AdvancedConfigDetails.HoldOffBufferBytes = (ulong) (HoldOffMBytes.Value * (1024 * 1024));
   //AdvancedConfigDetails.BufferReadSize = (uint) (BufferReadSizeKBytes.Value * 1024);
   //AdvancedConfigDetails.DelayStartMilliSec = (uint) DelayDokanStartmSec.Value;

   public class AdvancedPropertiesDisplay
   {
      public AdvancedPropertiesDisplay(ConfigDetails cd)
      {
         if (cd != null)
         {
            ThreadCount = cd.ThreadCount;
            LockTimeoutmSec = cd.LockTimeout;
            DokanDebugMode = cd.DebugMode;
            HoldOffMBytes = cd.HoldOffBufferBytes / (1024 * 1024);
            BufferReadSizeKBytes = cd.BufferReadSize / 1024;
            DelayDokanStartmSec = cd.DelayStartMilliSec;
         }
      }

      public uint DelayDokanStartmSec { get; set; }

      public uint BufferReadSizeKBytes { get; set; }

      public ulong HoldOffMBytes { get; set; }

      public bool DokanDebugMode { get; set; }

      public int LockTimeoutmSec { get; set; }

      [DescriptionAttribute("0 is automatic, use 1 for problem finding scenario's"),
      DefaultValue(5),
      DisplayName("Thread Count")
      ]
      public ushort ThreadCount { get; set; }

   }
}