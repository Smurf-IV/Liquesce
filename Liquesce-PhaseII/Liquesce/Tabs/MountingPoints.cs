#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="MountingPoints.cs" company="Smurf-IV">
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
using System.Windows.Forms;
using LiquesceFacade;

namespace Liquesce.Tabs
{
   public partial class MountingPoints : UserControl, ITab
   {
      public MountingPoints()
      {
         InitializeComponent();
      }

      private ConfigDetails cd1;
      public ConfigDetails cd
      {
         set 
         { 
            cd1 = value;
            edit1.cd = cd;
            PopulateMountList();
         }
         private get { return cd1; }
      }

      private void PopulateMountList()
      {
         listExistingMounts.Items.Add(string.Format("{0} ({1})", cd1.VolumeLabel, cd1.DriveLetter));
         listExistingMounts.SelectedIndex = 0;
      }

      private void listExistingMounts_SelectedIndexChanged(object sender, EventArgs e)
      {
         edit1.SelectedIndex( listExistingMounts.SelectedIndex );
      }

      private void btnNew_Click(object sender, EventArgs e)
      {
         edit1.Visible = true;
         pnlStart.Visible = false;
      }

      private void btnEdit_Click(object sender, EventArgs e)
      {
         edit1.Visible = true;
         pnlStart.Visible = false;
      }

      private void btnDelete_Click(object sender, EventArgs e)
      {
         edit1.Visible = true;
         pnlStart.Visible = false;
      }

      private void pnlBase_Leave(object sender, EventArgs e)
      {
         // restore the "Landing visibility"
         pnlStart.Visible = true;
         edit1.Visible = false;
      }


   }
}
