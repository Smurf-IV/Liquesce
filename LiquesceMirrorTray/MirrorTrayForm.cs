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
using LiquesceFacade;
using LiquesceMirrorToDo;

namespace LiquesceMirrorTray
{
    public enum MirrorState
    {
        Uninitialized = -1,
        StartingClosing = 0,
        Working = 1,
        WaitingOK = 2,
        Error = 99
    }

    public partial class MirrorTrayForm : Form
    {
        public MirrorState status = MirrorState.StartingClosing;

        public MirrorTrayForm()
        {
            InitializeComponent();
        }

        private void TrayForm_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;

            status = MirrorState.StartingClosing;
            
            //MirrorWorker.Start();

        }


        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = FormWindowState.Normal;
            this.TopMost = true;
            this.Focus();
            this.BringToFront();
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
                        timerReconnector.Start();
                        notifyIcon1.Text = "Liquesce Mirror is not ready.";
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.two_folder_flat_error;
                        break;

                    case MirrorState.Working:
                        progressBar.Style = ProgressBarStyle.Marquee;
                        timerToDoListRefresh.Start();
                        timerReconnector.Stop();
                        notifyIcon1.Text = "Liquesce Mirror has jobs to do.";
                        break;

                    case MirrorState.WaitingOK:
                        progressBar.Style = ProgressBarStyle.Blocks;
                        timerToDoListRefresh.Start();
                        timerReconnector.Stop();
                        notifyIcon1.Text = "Liquesce Mirror waiting for jobs.";
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.two_folder_flat_ok;
                        break;

                    case MirrorState.Error:
                        progressBar.Style = ProgressBarStyle.Blocks;
                        timerToDoListRefresh.Stop();
                        timerReconnector.Stop();
                        notifyIcon1.Text = "Liquesce Mirror got an Error!";
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.two_folder_flat_error;
                        break;

                }
                laststatus = status;
            }

            // do every time
            switch (status)
            {
                case MirrorState.Working:
                    if (icontoggle == false)
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.two_folder_flat_process1;
                    else
                        notifyIcon1.Icon = LiquesceMirrorTray.Properties.Resources.two_folder_flat_process2;
                    icontoggle = !icontoggle;
                    break;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            
            MirrorWorker.Start();


        }

        private void TrayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            MirrorWorker.Stop();
            this.notifyIcon1.Visible = false;
        }
        
        private void timerToDoListRefresh_Tick(object sender, EventArgs e)
        {
            ConnectAndGetFromService();
        }

        private void timerReconnector_Tick(object sender, EventArgs e)
        {
            ConnectAndGetFromService(true);
        }



        public void refreshToDo()
        {
            MirrorWorker.getToDo(this.ToDoList.Items);
        }

        private void ConnectAndGetFromService(bool firststart = false)
        {
            // refresh the gui?
            if (MirrorWorker.refreshToDo == true)
            {
                MirrorWorker.getToDo(this.ToDoList.Items);
                MirrorWorker.refreshToDo = false;
            }

            try
            {
                ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
                ILiquesce remoteIF = factory.CreateChannel();

                MirrorToDoList newWork = remoteIF.MirrorToDo;

                MirrorWorker.addWork(newWork);
                MirrorWorker.getToDo(this.ToDoList.Items);

                if (this.ToDoList.Items.Count == 0)
                {
                    status = MirrorState.WaitingOK;
                }
                else
                {
                    status = MirrorState.Working;
                }
                
            }
            catch
            {
                status = MirrorState.StartingClosing;
            }

        }

    }
}
