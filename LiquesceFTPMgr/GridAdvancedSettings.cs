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
         int nNumLines = 6;
         try
         {
            System.Reflection.PropertyInfo pi = propertyGrid1.GetType().GetProperty("Controls");
            Control.ControlCollection cc = (Control.ControlCollection)pi.GetValue(propertyGrid1, null);

            foreach (Control c in cc)
            {
               Type ct = c.GetType();
               string sName = ct.Name;

               if (sName == "DocComment")
               {
                  pi = ct.GetProperty("Lines");
                  if (pi != null)
                  {
#pragma warning disable 168
                     int i = (int)pi.GetValue(c, null);
#pragma warning restore 168
                     pi.SetValue(c, nNumLines, null);
                  }

                  if (ct.BaseType != null)
                  {
                     System.Reflection.FieldInfo fi = ct.BaseType.GetField("userSized",
                                                                           System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                     if (fi != null)
                        fi.SetValue(c, true);
                  }
                  break;
               }
            }
         }
         catch
         {
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
            Enum.TryParse(apd.AllocationMode, out cd.AllocationMode);
            cd.HoldOffBufferBytes = (apd.HoldOffMBytes * (1024 * 1024));
            cd.BufferReadSize = (apd.BufferReadSizeKBytes * 1024);
            cd.ServiceLogLevel = apd.ServiceLogLevel;
         }
      }

   }
}
