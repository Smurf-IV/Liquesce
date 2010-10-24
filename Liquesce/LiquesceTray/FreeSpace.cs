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

        private ConfigDetails config;

        private System.Windows.Forms.TextBox[] diskNames;
        private System.Windows.Forms.TextBox[] totalSpace;
        private System.Windows.Forms.TextBox[] freeSpace;
        private System.Windows.Forms.ProgressBar[] bars;



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
            // labelBars
            // 
            //System.Windows.Forms.Label labelBars = new System.Windows.Forms.Label();
            //labelBars.Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + 2);
            //labelBars.Name = "labelBars";
            //labelBars.Size = new System.Drawing.Size(200, CONTROL_OFFSET_TOP_LABEL-2);
            //labelBars.Text = "Free Space:";
            //this.Controls.Add(labelBars);
            leftSpace += 123;




            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                leftSpace = 0;

                // 
                // textBox diskName
                // 
                diskNames[i] = new System.Windows.Forms.TextBox();
                diskNames[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * i + 2 + CONTROL_OFFSET_TOP_LABEL);
                diskNames[i].Name = "diskName" + i.ToString();
                diskNames[i].ReadOnly = true;
                diskNames[i].Size = new System.Drawing.Size(120, 20);
                diskNames[i].TabIndex = 0;
                diskNames[i].Text = config.SourceLocations[i];
                this.Controls.Add(diskNames[i]);

                leftSpace += 123;


                ulong availabel;
                ulong total;
                ulong freebytes;

                if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
                {
                    // 
                    // textBox freeSpace
                    // 
                    totalSpace[i] = new System.Windows.Forms.TextBox();
                    totalSpace[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * i + 2 + CONTROL_OFFSET_TOP_LABEL);
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
                    freeSpace[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * i + 2 + CONTROL_OFFSET_TOP_LABEL);
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
                    bars[i].Location = new System.Drawing.Point(CONTROL_OFFSET_LEFT + leftSpace, CONTROL_OFFSET_TOP + CONTROL_SPACE * i + CONTROL_OFFSET_TOP_LABEL);
                    bars[i].Name = "progressBar" + i.ToString();
                    bars[i].Size = new System.Drawing.Size(200, 23);
                    bars[i].TabIndex = 200 + i;
                    this.Controls.Add(bars[i]);
                }
            }
        }


        private void RefreshControls()
        {
            for (int i = 0; i < config.SourceLocations.Count(); i++)
            {
                ulong availabel;
                ulong total;
                ulong freebytes;

                if (GetDiskFreeSpaceEx(config.SourceLocations[i], out availabel, out total, out freebytes))
                {
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
                    const int BAR_SCALE = 1000;
                    bars[i].Maximum = BAR_SCALE;
                    bars[i].Value = (int)(((total - availabel) * BAR_SCALE) / total);
                }
            }
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
