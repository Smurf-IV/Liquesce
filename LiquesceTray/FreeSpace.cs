using System;
using System.Linq;
using System.Windows.Forms;
using LiquesceFacade;
using System.ServiceModel;
using System.Runtime.InteropServices;


namespace LiquesceTray
{
   public partial class FreeSpace : Form
   {
      private const int CONTROL_OFFSET_TOP = 3;
      private const int CONTROL_OFFSET_TOP_LABEL = 18;
      private const int CONTROL_OFFSET_LEFT = 3;
      private const int CONTROL_SPACE = 30;

      private const int BAR_SCALE = 1000;

      private ConfigDetails config;

      // bar variables + constants
      private const int BAR_SIZE = 300;
      private int barControlOffsetLeft;
      private ulong maxDiskSize;

      private TextBox[] diskNames;
      // private System.Windows.Forms.TextBox diskLiquesce;
      private TextBox[] totalSpace;
      private TextBox totalSpaceLiquesce;
      private TextBox[] freeSpace;
      private TextBox freeSpaceLiquesce;
      private ProgressBar[] bars;
      private ProgressBar barLiquesce;
      private CheckBox compareDisks;



      public FreeSpace()
      {
         InitializeComponent();
      }

      private void Form1_Load(object sender, EventArgs e)
      {
         if (GetConfig())
         {
            InitializeControls();
            RefreshControls();
         }
         else
            Close();
         // Review comment:
         // Are you sure calling close in the form OnLoad actually closes the form.. The only way to do this with 
         // garanteed resiults is to call it in the OnShown style calback
      }


      private bool GetConfig()
      {
         bool value = true;
         try
         {
            ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFacade");
            ILiquesce remoteIF = factory.CreateChannel();

            config = remoteIF.ConfigDetails;
         }
         catch
         {
            value = false;
         }
         return value;
      }


      private void InitializeControls()
      {
         diskNames = new TextBox[config.SourceLocations.Count()];
         totalSpace = new TextBox[config.SourceLocations.Count()];
         freeSpace = new TextBox[config.SourceLocations.Count()];
         bars = new ProgressBar[config.SourceLocations.Count()];


         int leftSpace = 0;
         // 
         // labelDiskName
         // 
         Label labelDiskName = new Label
                                  {
                                     Location =
                                        new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2),
                                     Name = "labelDiskName",
                                     Size = new System.Drawing.Size(120, CONTROL_OFFSET_TOP_LABEL),
                                     Text = "Disk Name:"
                                  };
         Controls.Add(labelDiskName);
         leftSpace += 123;

         // 
         // labelTotalSpace
         // 
         Label labelTotalSpace = new Label
                                    {
                                       Location =
                                          new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                   CONTROL_OFFSET_TOP + 2),
                                       Name = "labelTotalSpace",
                                       Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL),
                                       Text = "Total Space:"
                                    };
         Controls.Add(labelTotalSpace);
         leftSpace += 83;

         // 
         // labelFreeSpace
         // 
         Label labelFreeSpace = new Label
                                   {
                                      Location =
                                         new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                  CONTROL_OFFSET_TOP + 2),
                                      Name = "labelFreeSpace",
                                      Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL),
                                      Text = "Free Space:"
                                   };
         Controls.Add(labelFreeSpace);
         leftSpace += 83;

         // 
         // compareDisks
         // 
         compareDisks = new CheckBox
                           {
                              AutoSize = true,
                              Location =
                                 new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP),
                              Name = "realPropation",
                              Size = new System.Drawing.Size(123, CONTROL_OFFSET_TOP_LABEL),
                              TabIndex = 0,
                              Text = "Compare Disks",
                              UseVisualStyleBackColor = true,
                              Checked = true
                           };
         Controls.Add(compareDisks);

         // This is reset below
         //leftSpace += 123;



         leftSpace = 0;
         // 
         // textBox diskLiquesce
         // 
         TextBox diskLiquesce = new TextBox
                                                        {
                                                           Location =
                                                              new System.Drawing.Point(
                                                              CONTROL_OFFSET_LEFT + leftSpace,
                                                              CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL),
                                                           Name = "diskLiquesce",
                                                           ReadOnly = true,
                                                           Size = new System.Drawing.Size(120, 20),
                                                           TabIndex = 0,
                                                           Text = config.DriveLetter + ": (Virtual Drive)"
                                                        };
         Controls.Add(diskLiquesce);

         leftSpace += 123;

         // 
         // textBox totalSpaceLiquesce
         // 
         totalSpaceLiquesce = new TextBox
                                 {
                                    Location =
                                       new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL),
                                    Name = "totalSpaceLiquesce",
                                    ReadOnly = true,
                                    Size = new System.Drawing.Size(80, 20),
                                    TabIndex = 0,
                                    TextAlign = HorizontalAlignment.Right
                                 };
         Controls.Add(totalSpaceLiquesce);

         leftSpace += 83;

         // 
         // textBox freeSpaceLiquesce
         // 
         freeSpaceLiquesce = new TextBox
                                {
                                   Location =
                                      new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                               CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL),
                                   Name = "freeSpaceLiquesce",
                                   ReadOnly = true,
                                   Size = new System.Drawing.Size(80, 20),
                                   TabIndex = 0,
                                   TextAlign = HorizontalAlignment.Right
                                };
         Controls.Add(freeSpaceLiquesce);

         leftSpace += 83;


         //
         // progress barLiquesce
         //
         barLiquesce = new ProgressBar();
         barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
         barLiquesce.Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP + CONTROL_OFFSET_TOP_LABEL);
         barLiquesce.Name = "barLiquesce";
         barLiquesce.Size = new System.Drawing.Size(BAR_SIZE, 23);
         barLiquesce.TabIndex = 0;
         Controls.Add(barLiquesce);





         for (int i = 0; i < config.SourceLocations.Count(); i++)
         {
            int ii = i + 1;
            leftSpace = 0;

            // 
            // textBox diskName
            // 
            diskNames[i] = new TextBox
                              {
                                 Location =
                                    new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                             CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 +
                                                             CONTROL_OFFSET_TOP_LABEL),
                                 Name = "diskName" + i,
                                 ReadOnly = true,
                                 Size = new System.Drawing.Size(120, 20),
                                 TabIndex = 0,
                                 Text = config.SourceLocations[i]
                              };
            Controls.Add(diskNames[i]);

            leftSpace += 123;


            ulong availabel;
            ulong total;
            ulong freebytes;
            if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
            {
               if (total > maxDiskSize)
                  maxDiskSize = total;

               // 
               // textBox totalSpace
               // 
               totalSpace[i] = new TextBox
                                  {
                                     Location =
                                        new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                 CONTROL_OFFSET_TOP + CONTROL_SPACE*ii + 2 +
                                                                 CONTROL_OFFSET_TOP_LABEL),
                                     Name = "totalSpace" + i,
                                     ReadOnly = true,
                                     Size = new System.Drawing.Size(80, 20),
                                     TabIndex = 0,
                                     TextAlign = HorizontalAlignment.Right
                                  };
               Controls.Add(totalSpace[i]);
               leftSpace += 83;

               // 
               // textBox freeSpace
               // 
               freeSpace[i] = new TextBox
                                 {
                                    Location =
                                       new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                CONTROL_OFFSET_TOP + CONTROL_SPACE*ii + 2 +
                                                                CONTROL_OFFSET_TOP_LABEL),
                                    Name = "freeSpace" + i,
                                    ReadOnly = true,
                                    Size = new System.Drawing.Size(80, 20),
                                    TabIndex = 0,
                                    TextAlign = HorizontalAlignment.Right
                                 };
               Controls.Add(freeSpace[i]);
               leftSpace += 83;


               //
               // progress bar
               //
               bars[i] = new ProgressBar();
               barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
               bars[i].Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + CONTROL_OFFSET_TOP_LABEL);
               bars[i].Name = "progressBar" + i;
               bars[i].Size = new System.Drawing.Size(BAR_SIZE, 23);
               bars[i].TabIndex = 200 + i;
               Controls.Add(bars[i]);
            }
         }
      }


      private void RefreshControls()
      {
         ulong allAvailabel = 0;
         ulong allTotal = 0;

         for (int i = 0; i < config.SourceLocations.Count(); i++)
         {
            ulong availabel;
            ulong total;
            ulong freebytes;

            if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
            {
               allAvailabel += availabel;
               allTotal += total;

               // 
               // textBox freeSpace
               // 
               totalSpace[i].Text = FormatBytes((long)total);

               // 
               // textBox freeSpace
               // 
               freeSpace[i].Text = FormatBytes((long)availabel);

               //
               // progress bar
               //
               if (compareDisks.Checked)
               {
                  bars[i].Left = barControlOffsetLeft + (int)(BAR_SIZE - ((total * BAR_SIZE) / maxDiskSize));
                  bars[i].Width = (int)((total * BAR_SIZE) / maxDiskSize);
               }
               else
               {
                  bars[i].Left = barControlOffsetLeft;
                  bars[i].Width = BAR_SIZE;
               }
               bars[i].Maximum = BAR_SCALE;
               bars[i].Value = (int)(((total - availabel) * BAR_SCALE) / total);

            }
         }

         totalSpaceLiquesce.Text = FormatBytes((long)allTotal);
         freeSpaceLiquesce.Text = FormatBytes((long)allAvailabel);
         barLiquesce.Maximum = BAR_SCALE;
         barLiquesce.Value = (int)(((allTotal - allAvailabel) * BAR_SCALE) / allTotal);
      }



      private string FormatBytes(long bytes)
      {
         const int scale = 1024;
         string[] orders = new[] { "GB", "MB", "KB", "Bytes" };
         long max = (long)Math.Pow(scale, orders.Length - 1);

         foreach (string order in orders)
         {
            if (bytes > max)
               return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

            max /= scale;
         }
         return "0 Bytes";
      }


      #region DLL Imports

      /// <summary>
      /// </summary>
      /// <param name="lpDirectoryName"></param>
      /// <param name="lpFreeBytesAvailable"></param>
      /// <param name="lpTotalNumberOfBytes"></param>
      /// <param name="lpTotalNumberOfFreeBytes"></param>
      /// <returns></returns>
      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion

      private void timer1_Tick(object sender, EventArgs e)
      {
         RefreshControls();
      }

   }
}
