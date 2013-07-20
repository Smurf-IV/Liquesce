#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="NotifyIconHandler.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;
using LiquesceFacade;
using LiquesceTray.Properties;
using NLog;

namespace LiquesceTray
{
   public partial class NotifyIconHandler : UserControl
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private LiquesceSvcState lastState = LiquesceSvcState.Unknown;
      private readonly StateChangeHandler stateChangeHandler = new StateChangeHandler();

      public NotifyIconHandler()
      {
         InitializeComponent();
         if (Properties.Settings.Default.UpdateRequired)
         {
            // Thanks go to http://cs.rthand.com/blogs/blog_with_righthand/archive/2005/12/09/246.aspx
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpdateRequired = false;
            Properties.Settings.Default.Save();
         }
         notifyIcon1.BalloonTipTitle = Resources.NotifyIconHandler_NotifyIconHandler_Service_Status;
         // Use last state to prevent balloon tip showing on start !
         SetState(lastState, Resources.NotifyIconHandler_NotifyIconHandler_Application_tray_is_starting);
         DoStatusCheck(0);
         timer1.Start();
      }

      private void exitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         notifyIcon1.Visible = false;
         Application.Exit();
      }

      private void managementApp_Click(object sender, EventArgs e)
      {
         //Application.StartupPath;
         Process process = new Process { StartInfo = { 
                                            WorkingDirectory = Application.StartupPath,
                                            FileName = "Liquesce.exe"
         }
         };

         process.Start();
      }

      private void notifyIcon1_DoubleClick(object sender, EventArgs e)
      {
         managementApp_Click(sender, e);
      }

      private void repeatLastMessage_Click(object sender, EventArgs e)
      {
         notifyIcon1.ShowBalloonTip(5000);
      }

      private void SetState(LiquesceSvcState state, string text)
      {
         notifyIcon1.BalloonTipText = text;
         switch (state)
         {
            case LiquesceSvcState.InWarning:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_Warning;
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
               break;
            case LiquesceSvcState.Unknown:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_Unknown;
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
               break;
            case LiquesceSvcState.Running:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_Running;
               notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
               break;
            case LiquesceSvcState.Stopped:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_Stopped;
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
               break;
            case LiquesceSvcState.InError:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_In_Error;
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
               break;
            default:
               notifyIcon1.Text = Resources.NotifyIconHandler_SetState_Liquesce_State_Unknown;
               Log.Error("SetState has an unknown state value [{0}]", state);
               notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
               break;
         }
         if (state != lastState)
         {
            lastState = state;
            notifyIcon1.ShowBalloonTip(5000);
         }
      }
      private void timer1_Tick(object sender, EventArgs e)
      {
         DoStatusCheck(timer1.Interval / 2);
      }

      private void DoStatusCheck(int milliseconds)
      {
         try
         {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, milliseconds);
            serviceController1.WaitForStatus(ServiceControllerStatus.Running, timeSpan);
            if (LiquesceSvcState.Running != lastState)
            {
               notifyIcon1.Icon = Properties.Resources.OKIcon;
               SetState(LiquesceSvcState.Running, String.Format(Resources.NotifyIconHandler_DoStatusCheck_Started____0_, DateTime.Now) );
               stateChangeHandler.CreateCallBack(SetState);
            }
         }
         catch (System.ServiceProcess.TimeoutException tex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InWarning != lastState)
            {
               Log.WarnException("Service is not in a running state", tex);
               SetState(LiquesceSvcState.InWarning, Resources.NotifyIconHandler_DoStatusCheck_Liquesce_service_is_Stopped);
               notifyIcon1.Icon = Properties.Resources.StopIcon;
            }
         }
         catch (Exception ex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InError != lastState)
            {
               Log.ErrorException("Liquesce service has a general exception", ex);
               notifyIcon1.Icon = Properties.Resources.ErrorIcon;
               SetState(LiquesceSvcState.InError, ex.Message);
            }
         }
      }

      private void notifyIcon1_MouseDown(object sender, MouseEventArgs e)
      {
         if (e.Button == MouseButtons.Right)
         {
            bool visible = ((ModifierKeys & Keys.Control) == Keys.Control);
            notifyIcon1.ContextMenuStrip = visible ? rightClickContextMenuService : rightClickContextMenuNormal;
         }
      }

      private void rightClickContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
      {
         if (LiquesceSvcState.Running == lastState)
         {
             showFreeDiskSpaceToolStripMenuItem.Enabled = true;
             dropperToolStripMenuItem.Enabled = true;
         }
         else
         {
             showFreeDiskSpaceToolStripMenuItem.Enabled = false;
             dropperToolStripMenuItem.Enabled = false;
         }
      }

      private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
      {
         try
         {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
               UseShellExecute = true,
               CreateNoWindow = true,
               WindowStyle = ProcessWindowStyle.Hidden,
               FileName = Path.Combine(Application.StartupPath, @"LiquesceTrayHelper.exe"),
               Arguments = @"stop",
               // Two lines below make the UAC dialog modal to this app
               ErrorDialog = true,
               ErrorDialogParentHandle = this.Handle
            };

            //// if the other process did not have a manifest
            //// then force it to run elevated
            //startInfo.Verb = "runas";
            Process p = Process.Start(startInfo);

            // block this UI until the launched process exits
            // I.e. make it modal
            p.WaitForExit();
         }
         catch (Exception ex)
         {
            Log.ErrorException("stopServiceToolStripMenuItem_Click", ex);
         }
      }

      private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
      {
         try
         {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
               UseShellExecute = true,
               CreateNoWindow  = true,
               WindowStyle = ProcessWindowStyle.Hidden,
               FileName = Path.Combine(Application.StartupPath, @"LiquesceTrayHelper.exe"),
               Arguments = @"start",
               // Two lines below make the UAC dialog modal to this app
               ErrorDialog = true,
               ErrorDialogParentHandle = this.Handle
            };

            //// if the other process did not have a manifest
            //// then force it to run elevated
            //startInfo.Verb = "runas";
            Process p = Process.Start(startInfo);

            // block this UI until the launched process exits
            // I.e. make it modal
            p.WaitForExit();
         }
         catch (Exception ex)
         {
            Log.ErrorException("stopServiceToolStripMenuItem_Click", ex);
         }
      }


      private FreeSpace fsform = null;
      private void showFreeDiskSpaceToolStripMenuItem_Click(object sender, EventArgs e)
      {
          if (fsform != null)
          {
              fsform.Dispose();
          }
          fsform = new FreeSpace();
          fsform.Activate();
          fsform.Show();
          fsform.Focus();
          fsform.BringToFront();

      }

      private DropZone dropperForm = null;
      private void dropperToolStripMenuItem_Click(object sender, EventArgs e)
      {
          if (dropperForm != null)
          {
              dropperForm.Dispose();
          }
          dropperForm = new DropZone();
          dropperForm.Activate();
          dropperForm.Show();
          dropperForm.Focus();
          dropperForm.BringToFront();

      }



   }
}
