using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiquesceFaçade;
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
        private int barControlOffsetLeft = 0;
        private ulong maxDiskSize = 0;

        private System.Windows.Forms.TextBox[] diskNames;
        private System.Windows.Forms.TextBox diskLiquesce;
        private System.Windows.Forms.TextBox[] totalSpace;
        private System.Windows.Forms.TextBox totalSpaceLiquesce;
        private System.Windows.Forms.TextBox[] freeSpace;
        private System.Windows.Forms.TextBox freeSpaceLiquesce;
        private System.Windows.Forms.ProgressBar[] bars;
        private System.Windows.Forms.ProgressBar barLiquesce;
        private System.Windows.Forms.CheckBox compareDisks;



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
                this.Close();
        }


        private bool GetConfig()
        {
            bool value = true;
            try
            {
                ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
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
            ulong availabel;
            ulong total;
            ulong freebytes;


            diskNames = new System.Windows.Forms.TextBox[config.SourceLocations.Count()];
            totalSpace = new System.Windows.Forms.TextBox[config.SourceLocations.Count()];
            freeSpace = new System.Windows.Forms.TextBox[config.SourceLocations.Count()];
            bars = new System.Windows.Forms.ProgressBar[config.SourceLocations.Count()];


            int leftSpace = 0;
            // 
            // labelDiskName
            // 
            System.Windows.Forms.Label labelDiskName = new System.Windows.Forms.Label();
            labelDiskName.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2);
            labelDiskName.Name = "labelDiskName";
            labelDiskName.Size = new System.Drawing.Size(120, CONTROL_OFFSET_TOP_LABEL);
            labelDiskName.Text = "Disk Name:";
            this.Controls.Add(labelDiskName);
            leftSpace += 123;

            // 
            // labelTotalSpace
            // 
            System.Windows.Forms.Label labelTotalSpace = new System.Windows.Forms.Label();
            labelTotalSpace.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2);
            labelTotalSpace.Name = "labelTotalSpace";
            labelTotalSpace.Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL);
            labelTotalSpace.Text = "Total Space:";
            this.Controls.Add(labelTotalSpace);
            leftSpace += 83;

            // 
            // labelFreeSpace
            // 
            System.Windows.Forms.Label labelFreeSpace = new System.Windows.Forms.Label();
            labelFreeSpace.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2);
            labelFreeSpace.Name = "labelFreeSpace";
            labelFreeSpace.Size = new System.Drawing.Size(80, CONTROL_OFFSET_TOP_LABEL);
            labelFreeSpace.Text = "Free Space:";
            this.Controls.Add(labelFreeSpace);
            leftSpace += 83;

            // 
            // compareDisks
            // 
            compareDisks = new System.Windows.Forms.CheckBox();
            compareDisks.AutoSize = true;
            compareDisks.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP);
            compareDisks.Name = "realPropation";
            compareDisks.Size = new System.Drawing.Size(123, CONTROL_OFFSET_TOP_LABEL);
            compareDisks.TabIndex = 0;
            compareDisks.Text = "Compare Disks";
            compareDisks.UseVisualStyleBackColor = true;
            compareDisks.Checked = true;
            this.Controls.Add(compareDisks);
            leftSpace += 123;



            leftSpace = 0;
            // 
            // textBox diskLiquesce
            // 
            System.Windows.Forms.TextBox diskLiquesce = new System.Windows.Forms.TextBox();
            diskLiquesce.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL);
            diskLiquesce.Name = "diskLiquesce";
            diskLiquesce.ReadOnly = true;
            diskLiquesce.Size = new System.Drawing.Size(120, 20);
            diskLiquesce.TabIndex = 0;
            diskLiquesce.Text = config.DriveLetter + ": (Virtual Drive)";
            this.Controls.Add(diskLiquesce);

            leftSpace += 123;

            // 
            // textBox totalSpaceLiquesce
            // 
            totalSpaceLiquesce = new System.Windows.Forms.TextBox();
            totalSpaceLiquesce.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL);
            totalSpaceLiquesce.Name = "totalSpaceLiquesce";
            totalSpaceLiquesce.ReadOnly = true;
            totalSpaceLiquesce.Size = new System.Drawing.Size(80, 20);
            totalSpaceLiquesce.TabIndex = 0;
            totalSpaceLiquesce.TextAlign = System.Windows.Forms.HorizontalAlignment.Right; ;
            this.Controls.Add(totalSpaceLiquesce);

            leftSpace += 83;

            // 
            // textBox freeSpaceLiquesce
            // 
            freeSpaceLiquesce = new System.Windows.Forms.TextBox();
            freeSpaceLiquesce.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2 + CONTROL_OFFSET_TOP_LABEL);
            freeSpaceLiquesce.Name = "freeSpaceLiquesce";
            freeSpaceLiquesce.ReadOnly = true;
            freeSpaceLiquesce.Size = new System.Drawing.Size(80, 20);
            freeSpaceLiquesce.TabIndex = 0;
            freeSpaceLiquesce.TextAlign = System.Windows.Forms.HorizontalAlignment.Right; ;
            this.Controls.Add(freeSpaceLiquesce);

            leftSpace += 83;

            
            //
            // progress barLiquesce
            //
            barLiquesce = new System.Windows.Forms.ProgressBar();
            barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
            barLiquesce.Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP+ CONTROL_OFFSET_TOP_LABEL);
            barLiquesce.Name = "barLiquesce";
            barLiquesce.Size = new System.Drawing.Size(BAR_SIZE, 23);
            barLiquesce.TabIndex = 0;
            this.Controls.Add(barLiquesce);





            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                int ii = i + 1;
                leftSpace = 0;

                // 
                // textBox diskName
                // 
                diskNames[i] = new System.Windows.Forms.TextBox();
                diskNames[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 + CONTROL_OFFSET_TOP_LABEL);
                diskNames[i].Name = "diskName" + i.ToString();
                diskNames[i].ReadOnly = true;
                diskNames[i].Size = new System.Drawing.Size(120, 20);
                diskNames[i].TabIndex = 0;
                diskNames[i].Text = config.SourceLocations[i];
                this.Controls.Add(diskNames[i]);

                leftSpace += 123;


                if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
                {
                    if (total > maxDiskSize)
                        maxDiskSize = total;

                    // 
                    // textBox totalSpace
                    // 
                    totalSpace[i] = new System.Windows.Forms.TextBox();
                    totalSpace[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 + CONTROL_OFFSET_TOP_LABEL);
                    totalSpace[i].Name = "totalSpace" + i.ToString();
                    totalSpace[i].ReadOnly = true;
                    totalSpace[i].Size = new System.Drawing.Size(80, 20);
                    totalSpace[i].TabIndex = 0;
                    totalSpace[i].TextAlign = System.Windows.Forms.HorizontalAlignment.Right; ;
                    this.Controls.Add(totalSpace[i]);

                    leftSpace += 83;

                    // 
                    // textBox freeSpace
                    // 
                    freeSpace[i] = new System.Windows.Forms.TextBox();
                    freeSpace[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + 2 + CONTROL_OFFSET_TOP_LABEL);
                    freeSpace[i].Name = "freeSpace" + i.ToString();
                    freeSpace[i].ReadOnly = true;
                    freeSpace[i].Size = new System.Drawing.Size(80, 20);
                    freeSpace[i].TabIndex = 0;
                    freeSpace[i].TextAlign = System.Windows.Forms.HorizontalAlignment.Right; ;
                    this.Controls.Add(freeSpace[i]);

                    leftSpace += 83;

                    
                    //
                    // progress bar
                    //
                    bars[i] = new System.Windows.Forms.ProgressBar();
                    barControlOffsetLeft = CONTROL_OFFSET_LEFT + leftSpace;
                    bars[i].Location = new System.Drawing.Point(barControlOffsetLeft, CONTROL_OFFSET_TOP + CONTROL_SPACE * ii + CONTROL_OFFSET_TOP_LABEL);
                    bars[i].Name = "progressBar" + i.ToString();
                    bars[i].Size = new System.Drawing.Size(BAR_SIZE, 23);
                    bars[i].TabIndex = 200 + i;
                    this.Controls.Add(bars[i]);
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
                    if (compareDisks.Checked == true)
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
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
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
        /// The CreateFile function creates or opens a file, file stream, directory, physical disk, volume, console buffer, tape drive,
        /// communications resource, mailslot, or named pipe. The function returns a handle that can be used to access an object.
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <param name="dwDesiredAccess"> access to the object, which can be read, write, or both</param>
        /// <param name="dwShareMode">The sharing mode of an object, which can be read, write, both, or none</param>
        /// <param name="SecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether or not the returned handle can
        /// be inherited by child processes. Can be null</param>
        /// <param name="dwCreationDisposition">An action to take on files that exist and do not exist</param>
        /// <param name="dwFlagsAndAttributes">The file attributes and flags. </param>
        /// <param name="hTemplateFile">A handle to a template file with the GENERIC_READ access right. The template file supplies file attributes
        /// and extended attributes for the file that is being created. This parameter can be null</param>
        /// <returns>If the function succeeds, the return value is an open handle to a specified file. If a specified file exists before the function
        /// all and dwCreationDisposition is CREATE_ALWAYS or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function
        /// succeeds. If a file does not exist before the call, GetLastError returns 0 (zero).
        /// If the function fails, the return value is INVALID_HANDLE_VALUE. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshControls();
        }

    }
}
