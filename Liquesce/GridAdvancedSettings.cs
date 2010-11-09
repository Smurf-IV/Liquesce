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
         ResizeDescriptionArea(ref propertyGrid1, 6); // okay for most
      }

      private bool ResizeDescriptionArea(ref PropertyGrid grid, int nNumLines)
      {
         try
         {
            System.Reflection.PropertyInfo pi = grid.GetType().GetProperty("Controls");
            System.Windows.Forms.Control.ControlCollection cc = (System.Windows.Forms.Control.ControlCollection)pi.GetValue(grid, null); 
            
            foreach (Control c in cc)
            {
               Type ct = c.GetType();
               string sName = ct.Name;

               if (sName == "DocComment")
               {
                  pi = ct.GetProperty("Lines");
                  if (pi != null)
                  {
                     int i = (int)pi.GetValue(c, null);
                     pi.SetValue(c, nNumLines, null);
                  }

                  System.Reflection.FieldInfo fi = ct.BaseType.GetField("userSized",
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                  if (fi != null) 
                     fi.SetValue(c, true);
                  break;
               }
            }

            return true;
         }
         catch
         {
            return false;
         }

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
            cd.eAllocationMode = apd.eAllocationMode;
            cd.HoldOffBufferBytes = (apd.HoldOffMBytes * (1024 * 1024));
            cd.BufferReadSize = (apd.BufferReadSizeKBytes * 1024);
            cd.DelayStartMilliSec = apd.DelayDokanStartmSec;
            cd.ServiceLogLevel = apd.ServiceLogLevel;
         }
      }

   }
}
