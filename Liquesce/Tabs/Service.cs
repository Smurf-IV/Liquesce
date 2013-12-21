#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Service.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Simon Coghlan (Aka Smurf-IV)
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
using System.ServiceModel;
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
            if (DialogResult.Yes == MessageBox.Show(this, "Performing this action will \"Remove the Mounted drive(s)\" on this machine.\n All open files will be forceably closed by this.\nDo you wish to continue ?", "Caution..", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
               pgrsService.Visible = true;
               pgrsService.Style = ProgressBarStyle.Marquee;

               EndpointAddress endpointAddress = new EndpointAddress("net.pipe://localhost/LiquesceFacade");
               NetNamedPipeBinding namedPipeBindingpublish = new NetNamedPipeBinding();
               LiquesceProxy proxy = new LiquesceProxy(namedPipeBindingpublish, endpointAddress);
               Log.Info("Didn't go bang so stop");
               proxy.Stop();
               Log.Info("Now start, may need a small sleep to allow things to settle");
               Thread.Sleep(Math.Max(1000, 2500 - (int)cd.DelayStartMilliSec));
               proxy.Start();
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("commitToolStripMenuItem_Click: Unable to attach to the service, even tho it is running", ex);
            MessageBox.Show(this, ex.Message, "Has the firewall blocked the communications ?", MessageBoxButtons.OK,
                            MessageBoxIcon.Stop);
         }
         finally
         {
            pgrsService.Visible = false;
            pgrsService.Style = ProgressBarStyle.Continuous;
         }
      }

      private void btnSave_Click(object sender, EventArgs e)
      {
         Log.Info("Save the new details");
         cd.WriteOutConfigDetails();
      }

      public ConfigDetails cd { set; private get; }
   }
}
