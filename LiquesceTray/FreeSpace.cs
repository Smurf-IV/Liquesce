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

        private const int COLUMN_0 = 120;
        private const int COLUMN_1 = 80;
        private const int COLUMN_2 = 80;
        private const int COLUMN_3 = BAR_SIZE;
        private const int COLUMN_CHECK_1 = 100;
        private const int COLUMN_CHECK_2 = 200;
        private const int TABLE_SIZE = COLUMN_0 + COLUMN_1 + COLUMN_2 + COLUMN_3 + 12;

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
        private CheckBox scaledMode;
        private CheckBox rightAligned;
        private TableLayoutPanel[] tableLayouts;



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
            tableLayouts = new TableLayoutPanel[config.SourceLocations.Count()];
            diskNames = new TextBox[config.SourceLocations.Count()];
            totalSpace = new TextBox[config.SourceLocations.Count()];
            freeSpace = new TextBox[config.SourceLocations.Count()];
            bars = new ProgressBar[config.SourceLocations.Count()];

            //------------------------------------------------------------------------------------------------
            // 
            // tableLayout
            // 
            System.Windows.Forms.TableLayoutPanel tableLayout = new System.Windows.Forms.TableLayoutPanel();
            tableLayout.ColumnCount = 5;
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_0));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_1));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_2));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_CHECK_1));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_CHECK_2));
            tableLayout.Location = new System.Drawing.Point(3, 3);
            tableLayout.Name = "tableLayout";
            tableLayout.RowCount = 1;
            tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayout.Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
            tableLayout.TabIndex = 0;
            this.flowLayout.Controls.Add(tableLayout);



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
                                         Text = "Disk Name:",
                                         Anchor = AnchorStyles.Bottom
                                     };
            tableLayout.Controls.Add(labelDiskName, 0, 0);
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
                                           Text = "Total Space:",
                                           Anchor = AnchorStyles.Bottom
                                       };
            tableLayout.Controls.Add(labelTotalSpace, 1, 0);
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
                                          Text = "Free Space:",
                                          Anchor = AnchorStyles.Bottom
                                      };
            tableLayout.Controls.Add(labelFreeSpace, 2, 0);
            leftSpace += 83;

            // 
            // scaledMode
            // 
            scaledMode = new CheckBox
            {
                AutoSize = true,
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP),
                Name = "scaledMode",
                Size = new System.Drawing.Size(123, CONTROL_OFFSET_TOP_LABEL),
                TabIndex = 0,
                Text = "Scaled Mode",
                UseVisualStyleBackColor = true,
                Checked = true,
                Anchor = AnchorStyles.Bottom
            };
            tableLayout.Controls.Add(scaledMode, 3, 0);

            // 
            // rightAligned
            // 
            rightAligned = new CheckBox
            {
                AutoSize = true,
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP),
                Name = "rightAligned",
                Size = new System.Drawing.Size(123, CONTROL_OFFSET_TOP_LABEL),
                TabIndex = 0,
                Text = "Right Aligned",
                UseVisualStyleBackColor = true,
                Checked = true,
                Anchor = (AnchorStyles.Right | AnchorStyles.Bottom)
            };
            tableLayout.Controls.Add(rightAligned, 4, 0);

            // This is reset below
            //leftSpace += 123;



            //------------------------------------------------------------------------------------------------
            // 
            // tableLayout
            // 
            tableLayout = new System.Windows.Forms.TableLayoutPanel();
            tableLayout.ColumnCount = 4;
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_0));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_1));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_2));
            tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_3));
            tableLayout.Location = new System.Drawing.Point(3, 3);
            tableLayout.Name = "tableLayout";
            tableLayout.RowCount = 1;
            tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayout.Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
            tableLayout.TabIndex = 0;
            this.flowLayout.Controls.Add(tableLayout);

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
            tableLayout.Controls.Add(diskLiquesce,0,0);

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
            tableLayout.Controls.Add(totalSpaceLiquesce, 1, 0);

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
            tableLayout.Controls.Add(freeSpaceLiquesce, 2, 0);

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
            tableLayout.Controls.Add(barLiquesce, 3, 0);





            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                int ii = i + 1;
                leftSpace = 0;


                //------------------------------------------------------------------------------------------------
                // 
                // tableLayout
                // 
                tableLayouts[i] = new System.Windows.Forms.TableLayoutPanel();
                tableLayouts[i].ColumnCount = 4;
                tableLayouts[i].ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_0));
                tableLayouts[i].ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, COLUMN_1));
                tableLayouts[i].ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_2));
                tableLayouts[i].ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize, COLUMN_3));
                tableLayouts[i].Location = new System.Drawing.Point(3, 3);
                tableLayouts[i].Name = "tableLayouts" + i.ToString();
                tableLayouts[i].RowCount = 1;
                tableLayouts[i].RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
                tableLayouts[i].Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
                tableLayouts[i].TabIndex = 0;
                this.flowLayout.Controls.Add(tableLayouts[i]);


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
                tableLayouts[i].Controls.Add(diskNames[i], 0, 0);

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
                                                                       CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 +
                                                                       CONTROL_OFFSET_TOP_LABEL),
                                           Name = "totalSpace" + i,
                                           ReadOnly = true,
                                           Size = new System.Drawing.Size(80, 20),
                                           TabIndex = 0,
                                           TextAlign = HorizontalAlignment.Right
                                       };
                    tableLayouts[i].Controls.Add(totalSpace[i], 1, 0);
                    leftSpace += 83;

                    // 
                    // textBox freeSpace
                    // 
                    freeSpace[i] = new TextBox
                                      {
                                          Location =
                                             new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                                      CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 +
                                                                      CONTROL_OFFSET_TOP_LABEL),
                                          Name = "freeSpace" + i,
                                          ReadOnly = true,
                                          Size = new System.Drawing.Size(80, 20),
                                          TabIndex = 0,
                                          TextAlign = HorizontalAlignment.Right
                                      };
                    tableLayouts[i].Controls.Add(freeSpace[i], 2, 0);
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
                    tableLayouts[i].Controls.Add(bars[i], 3, 0);
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
                    if (scaledMode.Checked)
                    {
                        //tableLayouts[i].ColumnStyles[4].Width = (int)((total * BAR_SIZE) / maxDiskSize);
                        //tableLayouts[i].ColumnStyles[3].Width = (int)(BAR_SIZE - ((total * BAR_SIZE) / maxDiskSize));
                        //bars[i].Left = barControlOffsetLeft + (int)(BAR_SIZE - ((total * BAR_SIZE) / maxDiskSize));
                        if (rightAligned.Checked)
                            bars[i].Anchor = AnchorStyles.Right;
                        else
                            bars[i].Anchor = AnchorStyles.Left;

                        bars[i].Width = (int)((total * BAR_SIZE) / maxDiskSize);
                    }
                    else
                    {
                        //tableLayouts[i].ColumnStyles[3].Width = 0;
                        //tableLayouts[i].ColumnStyles[4].Width = BAR_SIZE;
                        //bars[i].Left = barControlOffsetLeft;
                        bars[i].Anchor = AnchorStyles.Left;
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

        private void flowLayout_Paint(object sender, PaintEventArgs e)
        {

        }

    }
}
