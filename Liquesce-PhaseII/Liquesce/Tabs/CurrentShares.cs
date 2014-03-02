#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Welcome.cs" company="Smurf-IV">
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

using System.Linq;
using System.Text;
using System.Windows.Forms;
using LiquesceFacade;

namespace Liquesce.Tabs
{
   public partial class CurrentShares : UserControl, ITab
   {
      public CurrentShares()
      {
         InitializeComponent();
         DoubleBuffered = true;
         // Converted from the CurrentShares.rtf text via notepad :-)
         richTextBox1.Rtf = @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fnil\fcharset0 Tahoma;}{\f1\fnil\fcharset2 Symbol;}}
{\colortbl ;\red255\green0\blue0;}
{\*\generator Msftedit 5.41.21.2510;}\viewkind4\uc1\pard\lang2057\f0\fs18 The current active 'Mount Points' owned by Liquesce will need to be \cf1\b active\b0  \cf0 for this page to work correctly.\par
\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent0{\pntxtb\'B7}}\fi-360\li360 Once mounts are working, create shared folder(s) via windows.\par
{\pntext\f1\'B7\tab}Next, Press refresh, and check that the share details shown here are correct.\par
{\pntext\f1\'B7\tab}Once correct, press save in the Service Settings tab. \par
{\pntext\f1\'B7\tab}When Liquesce restarts it will then re-enable these shares.\par
\pard\sa200\sl276\slmult1\lang9\i Note:\line\pard{\pntext\f1\'B7\tab}{\*\pn\pnlvlblt\pnf1\pnindent0{\pntxtb\'B7}}\fi-360\li360\sa200\sl276\slmult1 Initial display will be from the last stored Liquesce configuration.\i0\par
}";
      }

      private ConfigDetails cd1;
      public ConfigDetails cd 
      {
         set
         {
            cd1 = value;
            PopulateShareList();
         }
         private get { return cd1; }
      }

      private void PopulateShareList()
      {
         // Populate the fields
         foreach (object[] row in cd.MountDetails
            .SelectMany(mt => mt.SharesToRestore
               .SelectMany(share =>share.UserAccessRules
                  .Select(fsare => new[] {
                     share.Path,
                     share.Name + " : " + share.Description,
                     GetAceInformation(fsare)
                  }
                  )
                  ).Cast<object[]>())
               )
         {
            dataGridView1.Rows.Add(row);
         }
      }


      private void btnRefresh_Click(object sender, System.EventArgs e)
      {
         try
         {
            dataGridView1.Rows.Clear();
            Enabled = false;
            UseWaitCursor = true;

            LanManShareHandler lmsHandler = new LanManShareHandler();
            foreach (MountDetail mt in cd.MountDetails)
            {
               mt.SharesToRestore = lmsHandler.MatchDriveLanManShares(mt.DriveLetter);
            }
            PopulateShareList();
         }
         finally
         {
            Enabled = true;
            UseWaitCursor = false;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Value = 0;
         }
      }

      private string GetAceInformation(UserAccessRuleExport fsare)
      {
         StringBuilder info = new StringBuilder(fsare.DomainUserIdentity);
         info.Append(" : ").Append(fsare.AccessMask);
         return info.ToString();
      }
   }
}