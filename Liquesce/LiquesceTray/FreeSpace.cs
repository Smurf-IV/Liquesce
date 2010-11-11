using System;
using System.Linq;
using System.Windows.Forms;
using LiquesceFacade;
using System.ServiceModel;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace LiquesceTray
{
    public partial class FreeSpace : Form
    {
        private const int CONTROL_OFFSET_TOP = 3;
        private const int CONTROL_OFFSET_TOP_LABEL = 18;
        private const int CONTROL_OFFSET_LEFT = 3;
        private const int CONTROL_SPACE = 30;


        private const int COLUMN_NAME_SIZE = 120;
        private const int COLUMN_NAME_INDEX = 0;

        private const int COLUMN_TOTAL_SIZE = 80;
        private const int COLUMN_TOTAL_INDEX = 1;

        private const int COLUMN_FREE_SIZE = 80;
        private const int COLUMN_FREE_INDEX = 4;

        private const int COLUMN_BACKUP_SIZE = 80;
        private const int COLUMN_BACKUP_INDEX = 3;

        private const int COLUMN_DATA_SIZE = 80;
        private const int COLUMN_DATA_INDEX = 2;

        private const int COLUMN_BAR_SIZE = BAR_SIZE;
        private const int COLUMN_BAR_INDEX = 5;

        private const int COLUMN_CHECK_1_SIZE = 100;
        private const int COLUMN_CHECK_1_INDEX = 5;
        private const int COLUMN_CHECK_2_SIZE = 200;
        private const int COLUMN_CHECK_2_INDEX = 6;

        private const int TABLE_SIZE = COLUMN_NAME_SIZE + COLUMN_TOTAL_SIZE + COLUMN_FREE_SIZE + COLUMN_BACKUP_SIZE + COLUMN_DATA_SIZE + COLUMN_BAR_SIZE + 24;
        private const int TABLE_CELL_CNT = 6;

        private const int BAR_SCALE = 1000;

        private ConfigDetails config;

        // bar variables + constants
        private const int BAR_SIZE = 300;
        private int barControlOffsetLeft;
        private ulong maxDiskSize;

        private TextBox[] diskNames;
        private TextBox[] totalSpace;
        private TextBox totalSpaceLiquesce;
        private TextBox[] freeSpace;
        private TextBox freeSpaceLiquesce;
        private TextBox[] backup;
        private TextBox backupLiquesce;
        private TextBox[] data;
        private TextBox dataLiquesce;
        private DoubleProgressBar[] bars;
        private DoubleProgressBar barLiquesce;
        private CheckBox scaledMode;
        private CheckBox rightAligned;
        private TableLayoutPanel[] tableLayouts;

        private ulong[] backupValues;

        private ulong[] oldFree;

        private List<long>[] averageRate;

        private Thread th;



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

                th = new Thread(threadBackupSize);
                th.Start();

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
            backup = new TextBox[config.SourceLocations.Count()];
            data = new TextBox[config.SourceLocations.Count()];
            bars = new DoubleProgressBar[config.SourceLocations.Count()];

            oldFree = new ulong[config.SourceLocations.Count()];
            averageRate = new List<long>[config.SourceLocations.Count()];
            for (int i = 0; i < oldFree.Count(); i++)
            {
                oldFree[i] = 0;
                averageRate[i] = new List<long>();
            }

            backupValues = new ulong[config.SourceLocations.Count()];


            //------------------------------------------------------------------------------------------------
            // 
            // tableLayout
            // 
            TableLayoutPanel tableLayout = new TableLayoutPanel {ColumnCount = TABLE_CELL_CNT + 1};
           tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_NAME_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_TOTAL_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_FREE_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_BACKUP_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_DATA_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_CHECK_1_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_CHECK_2_SIZE));
            tableLayout.Location = new System.Drawing.Point(3, 3);
            tableLayout.Name = "tableLayout";
            tableLayout.RowCount = 1;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayout.Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
            tableLayout.TabIndex = 0;
            flowLayout.Controls.Add(tableLayout);



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
            tableLayout.Controls.Add(labelDiskName, COLUMN_NAME_INDEX, 0);
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
                                           Text = "Disk Size:",
                                           Anchor = AnchorStyles.Bottom
                                       };
            tableLayout.Controls.Add(labelTotalSpace, COLUMN_TOTAL_INDEX, 0);
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
            tableLayout.Controls.Add(labelFreeSpace, COLUMN_FREE_INDEX, 0);
            leftSpace += 83;

            // 
            // labelBackup
            // 
            Label labelBackup = new Label
            {
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                            CONTROL_OFFSET_TOP + 2),
                Name = "labelBackup",
                Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL),
                Text = "Backup Size:",
                Anchor = AnchorStyles.Bottom
            };
            tableLayout.Controls.Add(labelBackup, COLUMN_BACKUP_INDEX, 0);
            leftSpace += 83;

            // 
            // labelData
            // 
            Label labelData = new Label
            {
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                            CONTROL_OFFSET_TOP + 2),
                Name = "labelData",
                Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL),
                Text = "Data Size:",
                Anchor = AnchorStyles.Bottom
            };
            tableLayout.Controls.Add(labelData, COLUMN_DATA_INDEX, 0);
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
            tableLayout.Controls.Add(scaledMode, COLUMN_CHECK_1_INDEX, 0);

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
            tableLayout.Controls.Add(rightAligned, COLUMN_CHECK_2_INDEX, 0);

            // This is reset below
            //leftSpace += 123;



            //------------------------------------------------------------------------------------------------
            // 
            // tableLayout
            // 
            tableLayout = new TableLayoutPanel {ColumnCount = TABLE_CELL_CNT};
           tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_NAME_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_TOTAL_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_FREE_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_BACKUP_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_DATA_SIZE));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_BAR_SIZE));
            tableLayout.Location = new System.Drawing.Point(3, 3);
            tableLayout.Name = "tableLayout";
            tableLayout.RowCount = 1;
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayout.Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
            tableLayout.TabIndex = 0;
            flowLayout.Controls.Add(tableLayout);

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
            tableLayout.Controls.Add(diskLiquesce, COLUMN_NAME_INDEX, 0);

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
            tableLayout.Controls.Add(totalSpaceLiquesce, COLUMN_TOTAL_INDEX, 0);

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
            tableLayout.Controls.Add(freeSpaceLiquesce, COLUMN_FREE_INDEX, 0);

            leftSpace += 83;


            // 
            // textBox backupLiquesce
            // 
            backupLiquesce = new TextBox
            {
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                            CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL),
                Name = "backupLiquesce",
                ReadOnly = true,
                Size = new System.Drawing.Size(80, 20),
                TabIndex = 0,
                TextAlign = HorizontalAlignment.Right
            };
            tableLayout.Controls.Add(backupLiquesce, COLUMN_BACKUP_INDEX, 0);

            leftSpace += 83;


            // 
            // textBox rateLiquesce
            // 
            dataLiquesce = new TextBox
            {
                Location =
                   new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                            CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL),
                Name = "rateLiquesce",
                ReadOnly = true,
                Size = new System.Drawing.Size(80, 20),
                TabIndex = 0,
                TextAlign = HorizontalAlignment.Right
            };
            tableLayout.Controls.Add(dataLiquesce, COLUMN_DATA_INDEX, 0);

            leftSpace += 83;


            //
            // progress barLiquesce
            //
            barLiquesce = new DoubleProgressBar {Name = "barLiquesce", 
            //barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
            //barLiquesce.Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP + CONTROL_OFFSET_TOP_LABEL);
           //barLiquesce.TabIndex = 0;
                                                 Size = new System.Drawing.Size(BAR_SIZE, 20)
            };
            tableLayout.Controls.Add(barLiquesce, COLUMN_BAR_INDEX, 0);





            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                int ii = i + 1;
                leftSpace = 0;


                //------------------------------------------------------------------------------------------------
                // 
                // tableLayout
                // 
                tableLayouts[i] = new TableLayoutPanel {ColumnCount = TABLE_CELL_CNT};
               tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_NAME_SIZE));
                tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_TOTAL_SIZE));
                tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_FREE_SIZE));
                tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_BACKUP_SIZE));
                tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_DATA_SIZE));
                tableLayouts[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, COLUMN_BAR_SIZE));
                tableLayouts[i].Location = new System.Drawing.Point(3, 3);
                tableLayouts[i].Name = "tableLayouts" + i;
                tableLayouts[i].RowCount = 1;
                tableLayouts[i].RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                tableLayouts[i].Size = new System.Drawing.Size(TABLE_SIZE, CONTROL_SPACE);
                tableLayouts[i].TabIndex = 0;
                flowLayout.Controls.Add(tableLayouts[i]);


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
                tableLayouts[i].Controls.Add(diskNames[i], COLUMN_NAME_INDEX, 0);

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
                    tableLayouts[i].Controls.Add(totalSpace[i], COLUMN_TOTAL_INDEX, 0);
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
                    tableLayouts[i].Controls.Add(freeSpace[i], COLUMN_FREE_INDEX, 0);
                    leftSpace += 83;


                    // 
                    // textBox backup
                    // 
                    backup[i] = new TextBox
                    {
                        Location =
                           new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace,
                                                    CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 +
                                                    CONTROL_OFFSET_TOP_LABEL),
                        Name = "backup" + i,
                        ReadOnly = true,
                        Size = new System.Drawing.Size(80, 20),
                        TabIndex = 0,
                        TextAlign = HorizontalAlignment.Right
                    };
                    tableLayouts[i].Controls.Add(backup[i], COLUMN_BACKUP_INDEX, 0);
                    leftSpace += 83;


                    // 
                    // textBox rate
                    // 
                    data[i] = new TextBox
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
                    tableLayouts[i].Controls.Add(data[i], COLUMN_DATA_INDEX, 0);
                    leftSpace += 83;


                    //
                    // progress bar
                    //
                    bars[i] = new DoubleProgressBar();
                    barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
                    bars[i].Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + CONTROL_OFFSET_TOP_LABEL);
                    bars[i].Name = "progressBar" + i;
                    bars[i].Size = new System.Drawing.Size(BAR_SIZE, 20);
                    bars[i].TabIndex = 200 + i;
                    tableLayouts[i].Controls.Add(bars[i], COLUMN_BAR_INDEX, 0);
                }
            }
        }



        private void RefreshControls()
        {
            ulong allAvailabel = 0;
            ulong allTotal = 0;
            long allRate = 0;
            ulong allBackup = 0;

            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                ulong availabel;
                ulong total;
                ulong freebytes;

                if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
                {
                    allAvailabel += availabel;
                    allTotal += total;
                    allBackup += backupValues[i];

                    long thisRate = 0;
                    thisRate = ((long)oldFree[i] - (long)availabel) / 4;
                    if (oldFree[i] == 0)
                        thisRate = 0;
                    averageRate[i].Add(thisRate);

                    thisRate = 0;
                    for (int ii = 0; ii < averageRate[i].Count; ii++)
                    {
                        thisRate += averageRate[i][ii];
                    }
                    thisRate /= averageRate[i].Count;

                    // remove oldest value
                    if (averageRate[i].Count >= 5)
                        averageRate[i].RemoveAt(0);

                    // save the free space for next calculation
                    oldFree[i] = freebytes;

                    // summ rate for liquesce drive
                    allRate += thisRate;

                    // 
                    // textBox totalSpace
                    // 
                    totalSpace[i].Text = FormatBytes((long)total);

                    // 
                    // textBox freeSpace
                    // 
                    freeSpace[i].Text = FormatBytes((long)availabel);

                    // 
                    // textBox backup
                    // 
                    backup[i].Text = FormatBytes((long)backupValues[i]);

                    // 
                    // textBox data
                    // 
                    data[i].Text = FormatBytes((long)(total - availabel - backupValues[i]));

                    //
                    // progress bar
                    //
                    if (scaledMode.Checked)
                    {
                        if (rightAligned.Checked)
                            bars[i].Anchor = AnchorStyles.Right | AnchorStyles.Top;
                        else
                            bars[i].Anchor = AnchorStyles.Left | AnchorStyles.Top;

                        bars[i].Width = (int)((total * BAR_SIZE) / maxDiskSize);
                    }
                    else
                    {
                        bars[i].Anchor = AnchorStyles.Left | AnchorStyles.Top;
                        bars[i].Width = BAR_SIZE;
                    }

                    bars[i].Maximum = BAR_SCALE;
                    bars[i].Value1 = (int)(((total - availabel - backupValues[i]) * BAR_SCALE) / total);
                    bars[i].Value2 = (int)(((total - availabel) * BAR_SCALE) / total);

                    if (availabel < config.HoldOffBufferBytes)
                    {
                        bars[i].ErrorStatus = DoubleProgressBar.ErrorStatusType.Error;
                    }
                    else if (availabel < config.HoldOffBufferBytes * 2)
                    {
                        bars[i].ErrorStatus = DoubleProgressBar.ErrorStatusType.Warn;
                    }
                    else
                    {
                        bars[i].ErrorStatus = DoubleProgressBar.ErrorStatusType.NoError;
                    }

                    if (thisRate < 0)
                    {
                        bars[i].Rate = DoubleProgressBar.RateType.Negative;
                    }
                    else if (thisRate > 0)
                    {
                        bars[i].Rate = DoubleProgressBar.RateType.Positive;
                    }
                    else
                    {
                        bars[i].Rate = DoubleProgressBar.RateType.No;
                    }
                }
            }

            totalSpaceLiquesce.Text = FormatBytes((long)allTotal);
            freeSpaceLiquesce.Text = FormatBytes((long)allAvailabel);
            backupLiquesce.Text = FormatBytes((long)allBackup);
            dataLiquesce.Text = FormatBytes((long)(allTotal - allAvailabel - allBackup));
            barLiquesce.Maximum = BAR_SCALE;
            barLiquesce.Value1 = (int)(((allTotal - allAvailabel - allBackup) * BAR_SCALE) / allTotal);
            barLiquesce.Value2 = (int)(((allTotal - allAvailabel) * BAR_SCALE) / allTotal);

            if (allRate < 0)
            {
                barLiquesce.Rate = DoubleProgressBar.RateType.Negative;
            }
            else if (allRate > 0)
            {
                barLiquesce.Rate = DoubleProgressBar.RateType.Positive;
            }
            else
            {
                barLiquesce.Rate = DoubleProgressBar.RateType.No;
            }

        }



        private string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new[] { "TB", "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }

        private string FormatBytesRate(long bytes)
        {
            const int scale = 1024;
            string[] orders = new[] { "TB/s", "GB/s", "MB/s", "KB/s", "Bytes/s" };
            long max = (long)Math.Pow(scale, orders.Length - 1);
            string sign = "+";

            if (bytes < 0)
            {
                // negative number
                bytes = bytes * -1;
                sign = "-";
            }

            foreach (string order in orders)
            {
                if (bytes > max)
                    return sign + string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes/s";
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


        public static long FolderSize(string directory, bool deep)
        {
            long sizeInBytes = 0;
            try
            {
                if (Directory.Exists(directory))
                {
                    DirectoryInfo dir = new DirectoryInfo(directory);
                   sizeInBytes += dir.GetFiles().Sum(f => f.Length);
                   if (deep)
                    {
                       sizeInBytes += dir.GetDirectories().Sum(d => FolderSize(d.FullName, deep));
                    }
                }
            }
            catch { }
            return sizeInBytes;
        }


        private void threadBackupSize()
        {
            while (true)
            {
                for (int i = 0; i < config.SourceLocations.Count; i++)
                {
                    backupValues[i] = (ulong)FolderSize(config.SourceLocations[i] + "\\" + BackupFileManager.HIDDEN_BACKUP_FOLDER, true);
                }

                Thread.Sleep(timer1.Interval);
            }
        }

        private void FreeSpace_FormClosed(object sender, FormClosedEventArgs e)
        {
            th.Abort();
        }
    }
}
