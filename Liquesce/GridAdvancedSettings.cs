using System;
using System.Windows.Forms;
using LiquesceFacade;

namespace Liquesce
{
   public partial class GridAdvancedSettings : Form
   {
      private AdvancedPropertiesDisplay apd;

      public GridAdvancedSettings()
      {
         InitializeComponent();
      }

      private void GridAdvancedSettings_Load(object sender, EventArgs e)
      {
         Utils.ResizeDescriptionArea(ref propertyGrid1, 6); // okay for most
      }


      private ConfigDetails cd;
      public ConfigDetails AdvancedConfigDetails
      {
         get { return cd; }
         set
         {
            cd = value;
            apd = new AdvancedPropertiesDisplay(cd);
            propertyGrid1.SelectedObject = apd;
         }
      }

      private void button1_Click(object sender, System.EventArgs e)
      {
         if (cd != null)
         {
            cd.ThreadCount = apd.ThreadCount;
            cd.LockTimeout = apd.LockTimeoutmSec;
            cd.DebugMode = apd.DokanDebugMode;
            Enum.TryParse(apd.AllocationMode, out cd.AllocationMode);
            cd.HoldOffBufferBytes = (apd.HoldOffMBytes * (1024 * 1024));
            cd.BufferReadSize = (apd.BufferReadSizeKBytes * 1024);
            cd.ServiceLogLevel = apd.ServiceLogLevel;
            cd.CacheLifetimeSeconds = apd.CacheLifetimeSeconds;
         }
      }

   }
}
