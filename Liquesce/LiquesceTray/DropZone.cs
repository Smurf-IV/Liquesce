using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using LiquesceFacade;
using System.IO;

namespace LiquesceTray
{
    public partial class DropZone : Form
    {
        private ConfigDetails config;

        public DropZone()
        {
            InitializeComponent();
        }

        private void Dropper_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Dropper_DragDrop(object sender, DragEventArgs e)
        {
            string[] strFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            GetAllRoots(strFiles[0]);
        }

        private void DropZone_Load(object sender, EventArgs e)
        {
            GetConfig();

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


        private void GetAllRoots(string liquescePath)
        {
            textBox1.Text = liquescePath;

            listBox1.Items.Clear();

            // check if path is on liquesce drive
            if (liquescePath[0].ToString() == config.DriveLetter)
            {
                // cut drive letter and :
                string relative = liquescePath.Substring(2);

                for (int i = 0; i < config.SourceLocations.Count; i++)
                {
                    string root = config.SourceLocations[i];

                    if (File.Exists(root + relative))
                    {
                        listBox1.Items.Add(root + relative);
                    }

                    if (Directory.Exists(root + relative))
                    {
                        listBox1.Items.Add(root + relative);
                    }
                }
            }
            else
            {
                listBox1.Items.Add("File is not in Liquesce Drive.");
            }
        }

    }
}
