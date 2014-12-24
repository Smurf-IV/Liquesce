#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Service.cs" company="Smurf-IV">
//
//  Copyright (C) 2013-2014 Simon Coghlan (Aka Smurf-IV)
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
#endregion Copyright (C)

using System;
using System.ComponentModel;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

using LiquesceFacade;
using NLog;

namespace Liquesce.Tabs
{
   public partial class Service : UserControl, ITab
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public Service()
      {
         InitializeComponent();
         DoubleBuffered = true;
      }

      private void Service_Load(object sender, EventArgs e)
      {
         propertyGrid1.SelectedObject = new AdvancedPropertiesDisplay(cd);

         Utils.ResizeDescriptionArea(ref propertyGrid1, 5);
      }

      private void btnStopStart_Click(object sender, EventArgs e)
      {
         try
         {
            if (DialogResult.Yes ==
                MessageBox.Show(this,
                   "Performing this action will \"Remove the Mounted drive(s)\" on this machine.\nAll open files on the mounts, will be forceably closed by this.\n\nDo you wish to continue ?",
                   "Caution..", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
               UseWaitCursor = true;
               Enabled = false;
               pgrsService.Visible = true;
               pgrsService.Style = ProgressBarStyle.Marquee;
               BackgroundWorker bw = new BackgroundWorker();
               bw.DoWork += delegate
               {
                  ServiceController serviceController1 = new ServiceController {ServiceName = "LiquesceSvc"};
                  if (serviceController1.Status != ServiceControllerStatus.Stopped)
                  {
                     Log.Info("Calling SCM stop");
                     serviceController1.Stop();
                     Log.Info("Now wait for the stopped state");
                     serviceController1.WaitForStatus(ServiceControllerStatus.Stopped);
                     Log.Info("Now start, may need a small sleep to allow things to settle 1");
                     Thread.Sleep(Math.Max(1000, 2500 - (int) cd.DelayStartMilliSec));
                  }
                  Log.Info("Calling SCM start");
                  serviceController1.Start();
                  Log.Info("Now wait for the stopped state");
                  serviceController1.WaitForStatus(ServiceControllerStatus.Running);
                  Log.Info("Now start, may need a small sleep to allow things to settle 2");
                  Thread.Sleep(Math.Max(1000, 2500 - (int) cd.DelayStartMilliSec));
               };
               // what to do when worker completes its task
               bw.RunWorkerCompleted += delegate(object sender1, RunWorkerCompletedEventArgs rwceArgs)
               {
                  if (rwceArgs.Error != null)
                  {
                     Log.ErrorException("btnStopStart threw:", rwceArgs.Error);
                  }
                  UseWaitCursor = false;
                  Enabled = true;
                  pgrsService.Visible = false;
                  pgrsService.Style = ProgressBarStyle.Continuous;
               };
               bw.RunWorkerAsync();
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("commitToolStripMenuItem_Click: Unable to attach to the service, even tho it is running", ex);
            MessageBox.Show(this, ex.Message, "Has the firewall blocked the communications ?", MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
         }
      }

      private void btnSave_Click(object sender, EventArgs e)
      {
         Log.Info("Save the new details");
         foreach (MountDetail detail in cd.MountDetails)
         {
            detail.UseInplaceRenaming = cd.UseInplaceRenaming;
         }
         cd.WriteOutConfigDetails();
      }

      public ConfigDetails cd { set; private get; }
   }
}