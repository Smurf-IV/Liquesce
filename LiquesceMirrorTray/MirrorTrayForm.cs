using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
//using System.Messaging;
//using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using LiquesceFaçade;

namespace LiquesceMirrorTray
{
    public partial class MirrorTrayForm : Form
    {
        public enum MirrorState
        {
            Uninitialized = -1,
            StartingClosing = 0,
            Working = 1,
            WaitingOK = 2
        }

        public MirrorState status = MirrorState.StartingClosing;

        public MirrorTrayForm()
        {
            InitializeComponent();
        }

        private void TrayForm_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;



        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
        }

        private bool icontoggle = false;
        private MirrorState laststatus = MirrorState.Uninitialized;
        private void timer1_Tick(object sender, EventArgs e)
        {
            // only when switched status
            if (status != laststatus)
            {
                switch (status)
                {
                    case MirrorState.StartingClosing:
                        progressBar.Style = ProgressBarStyle.Blocks;
                        timerToDoListRefresh.Stop();
                        notifyIcon1.Text = "Liquesce Mirror is not ready.";
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.Stop;
                        break;

                    case MirrorState.Working:
                        progressBar.Style = ProgressBarStyle.Marquee;
                        timerToDoListRefresh.Start();
                        notifyIcon1.Text = "Liquesce Mirror has jobs to do.";
                        break;

                    case MirrorState.WaitingOK:
                        progressBar.Style = ProgressBarStyle.Blocks;
                        timerToDoListRefresh.Start();
                        notifyIcon1.Text = "Liquesce Mirror waiting for jobs.";
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.OK;
                        break;

                }
                laststatus = status;
            }

            // do every time
            switch (status)
            {
                case MirrorState.Working:
                    if (icontoggle == false)
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.Config;
                    else
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.info;
                    icontoggle = !icontoggle;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //MirrorWorker.Start();
            status = MirrorState.Working;


        }

        private void TrayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            MirrorWorker.Stop();
            this.notifyIcon1.Visible = false;
        }
        
        private void timerToDoListRefresh_Tick(object sender, EventArgs e)
        {
            this.ToDoList.Items.Clear();
            try
            {
                ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
                ILiquesce remoteIF = factory.CreateChannel();

                List<string> removelist = remoteIF.GetMirrorDeleteToDo();
                
                for (int i = 0; i < removelist.Count; i++)
                {
                    this.ToDoList.Items.Add("Delete: " + removelist[i]);
                }

            }
            catch (Exception ex)
            {
                status = MirrorState.StartingClosing;
            }

        }

    }
}
