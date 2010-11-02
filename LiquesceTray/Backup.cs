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
using System.Threading;

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
            buttonCancel.Enabled = true;

            // work
            string backuppath = config.DriveLetter + ":\\" + BackupFileManager.HIDDEN_BACKUP_FOLDER;
            if (Directory.Exists(backuppath))
            {
                BackupFileManager.searchpath = backuppath;
                BackupFileManager.Clear();
                Thread th = new Thread(new ThreadStart(BackupFileManager.FindFoldersAndFiles));
                th.Start();
            }
            else
            {
                textCurrent.Text = "[Can't find folder " + backuppath + "]";
                ResetControls();
            }
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
            if ( config.eAllocationMode != ConfigDetails.AllocationModes.backup )
                MessageBox.Show(
                    "Warning:\n" + 
                    "Backup Mode is not configured for the Liquesce Service. If you don't switch to \".backup\", your saved " + 
                    "files can become inconsistent. Use the Liquesce Configuration App to reconfigure the Service. The Backup " + 
                    "Consistency Checker can work without the backup mode enabled but use it with care and switch to backup Mode.", 
                    "Backup Mode is not configured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void buttonRemoveMissing_Click(object sender, EventArgs e)
        {
            buttonRemoveMissing.Enabled = false;
            buttonRemoveInconsistent.Enabled = false;
            buttonConsistency.Enabled = false;
            buttonCancel.Enabled = true;

            progress.Enabled = true;


            // work
            Thread th = new Thread(new ThreadStart(BackupFileManager.RemoveMissing));
            th.Start();
        }


        public void ResetControls()
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

            buttonCancel.Enabled = false;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            BackupFileManager.cancel = true;
            while (BackupFileManager.cancel == true)
            {
                Thread.Sleep(100);
            }
            ResetControls();
        }

        private void buttonRemoveInconsistent_Click(object sender, EventArgs e)
        {
            buttonRemoveMissing.Enabled = false;
            buttonRemoveInconsistent.Enabled = false;
            buttonConsistency.Enabled = false;
            buttonCancel.Enabled = true;

            progress.Enabled = true;


            // work
            Thread th = new Thread(new ThreadStart(BackupFileManager.RemoveInconsistent));
            th.Start();
        }
    }
}
