#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Logging.cs" company="Smurf-IV">
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
using System.IO;
using System.Windows.Forms;
using NLog;

namespace Liquesce.Tabs
{
   public partial class Logging : UserControl
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public Logging()
      {
         InitializeComponent();
      }

      private TailForm tailForm;

      private void btnServiceTail_Click(object sender, EventArgs e)
      {
         try
         {
            if (tailForm == null)
            {
               string logLocation = Path.Combine(DisplayLog.FindLogLocation(@"LiquesceSvc\Logs"), "LiquesceSvc.log");
               if (!string.IsNullOrEmpty(logLocation))
               {
                  tailForm = new TailForm(logLocation);
                  tailForm.Show(this);
                  tailForm.FormClosing += delegate 
                  { 
                     tailForm = null;
                     BringToFront();
                  };
               }
            }
            else
            {
               tailForm.BringToFront();
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenFile has an exception: ", ex);
         }
      }

      private void commandLinkButton1_Click(object sender, EventArgs e)
      {
         DisplayLog.LogDisplay(@"LiquesceSvc\Logs");
      }

      private void commandLinkButton2_Click(object sender, EventArgs e)
      {
         DisplayLog.LogDisplay(@"Liquesce\Logs");
      }

      private void Logging_Load(object sender, EventArgs e)
      {
         btnServiceTail.Focus();
      }
   }
}
