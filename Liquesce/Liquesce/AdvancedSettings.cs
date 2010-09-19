using System.Windows.Forms;
using LiquesceFaçade;

namespace Liquesce
{
   public partial class AdvancedSettings : Form
   {
      public AdvancedSettings()
      {
         InitializeComponent();
      }

      public ConfigDetails AdvancedConfigDetails { get; set; }

      private void AdvancedSettings_Load(object sender, System.EventArgs e)
      {
         if (AdvancedConfigDetails != null)
         {
            ThreadCount.Value = AdvancedConfigDetails.ThreadCount;
            LockTimeoutmSec.Value = AdvancedConfigDetails.LockTimeout;
            DokanDebugMode.Checked = AdvancedConfigDetails.DebugMode;
            HoldOffMBytes.Value = AdvancedConfigDetails.HoldOffBufferBytes / (1024*1024);
            BufferReadSizeKBytes.Value = AdvancedConfigDetails.BufferReadSize / 1024;
            DelayDokanStartmSec.Value = AdvancedConfigDetails.DelayStartMilliSec;
         }
      }

      private void button1_Click(object sender, System.EventArgs e)
      {
         if (AdvancedConfigDetails != null)
         {
            AdvancedConfigDetails.ThreadCount = (ushort) ThreadCount.Value;
            AdvancedConfigDetails.LockTimeout = (int) LockTimeoutmSec.Value;
            AdvancedConfigDetails.DebugMode = DokanDebugMode.Checked;
            AdvancedConfigDetails.HoldOffBufferBytes = (ulong) (HoldOffMBytes.Value * (1024 * 1024));
            AdvancedConfigDetails.BufferReadSize = (uint) (BufferReadSizeKBytes.Value * 1024);
            AdvancedConfigDetails.DelayStartMilliSec = (uint) DelayDokanStartmSec.Value;
         }
      }
   }
}
