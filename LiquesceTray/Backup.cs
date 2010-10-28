using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiquesceFacade;
using System.ServiceModel;
using System.IO;

namespace LiquesceTray
{
    public partial class Backup : Form
    {

        public ConfigDetails config;
        
        public Backup()
        {
            InitializeComponent();
        }

        private void buttonConsistency_Click(object sender, EventArgs e)
        {
            // controls stuff
            progress.Style = ProgressBarStyle.Marquee;
            progress.Enabled = true;
            buttonRemoveInconsistent.Enabled = false;
            buttonRemoveMissing.Enabled = false;
            buttonConsistency.Enabled = false;

            // work
            string searchpath = config.DriveLetter + ":\\" + BackupFileManager.HIDDEN_BACKUP_FOLDER;
            BackupFileManager.Clear();
            BackupFileManager.FindFoldersAndFiles(searchpath);

            // controls stuff out
            ResetControls();

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

        private void Backup_Load(object sender, EventArgs e)
        {
            GetConfig();
            BackupFileManager.Init(this);
        }

        private void buttonRemoveMissing_Click(object sender, EventArgs e)
        {
            buttonRemoveMissing.Enabled = false;
            buttonRemoveInconsistent.Enabled = false;
            buttonConsistency.Enabled = false;

            progress.Enabled = true;


            BackupFileManager.RemoveMissing();
            BackupFileManager.missingFi.Clear();
            BackupFileManager.missingFo.Clear();
            listMissing.Items.Clear();


            ResetControls();
        }


        private void ResetControls()
        {
            progress.Style = ProgressBarStyle.Continuous;
            progress.Value = 0;
            progress.Enabled = false;

            if (listMissing.Items.Count == 0)
                buttonRemoveMissing.Enabled = false;
            else
                buttonRemoveMissing.Enabled = true;
            if (listInconsistent.Items.Count == 0)
                buttonRemoveInconsistent.Enabled = false;
            else
                buttonRemoveInconsistent.Enabled = true;
            buttonConsistency.Enabled = true;
        }

    }
}
