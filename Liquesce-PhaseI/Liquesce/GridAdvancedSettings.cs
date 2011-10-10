#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="GridAdvancedSettings.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 fpDragon
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
using System.Windows.Forms;
using LiquesceFacade;

namespace Liquesce
{
   public partial class GridAdvancedSettings : Form
   {
      private AdvancedPropertiesDisplay apd;

      public GridAdvancedSettings()
      {
         InitializeComponent();
      }

      private void GridAdvancedSettings_Load(object sender, EventArgs e)
      {
         Utils.ResizeDescriptionArea(ref propertyGrid1, 6); // okay for most
      }


      private ConfigDetails cd;
      public ConfigDetails AdvancedConfigDetails
      {
         get { return cd; }
         set
         {
            cd = value;
            apd = new AdvancedPropertiesDisplay(cd);
            propertyGrid1.SelectedObject = apd;
         }
      }

      private void button1_Click(object sender, System.EventArgs e)
      {
         if (cd != null)
         {
            cd.ThreadCount = apd.ThreadCount;
            cd.LockTimeout = apd.LockTimeoutmSec;
            cd.DebugMode = apd.DokanDebugMode;
            Enum.TryParse(apd.AllocationMode, out cd.AllocationMode);
            cd.HoldOffBufferBytes = (apd.HoldOffMBytes * (1024 * 1024));
            cd.BufferReadSize = (apd.BufferReadSizeKBytes * 1024);
            cd.ServiceLogLevel = apd.ServiceLogLevel;
            cd.CacheLifetimeSeconds = apd.CacheLifetimeSeconds;
         }
      }

   }
}
