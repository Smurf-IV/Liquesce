#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="TabMainForm.cs" company="Smurf-IV">
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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Liquesce.Tabs;
using LiquesceFacade;
using LiquesceTray;

namespace Liquesce
{
   public partial class TabMainForm : Form
   {
      private readonly ConfigDetails cd = new ConfigDetails();

      public TabMainForm()
      {
         InitializeComponent();
         DoubleBuffered = true;
         ResizeRedraw = true;

         if (Properties.Settings.Default.UpdateRequired)
         {
            // Thanks go to http://cs.rthand.com/blogs/blog_with_righthand/archive/2005/12/09/246.aspx
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.UpdateRequired = false;
            Properties.Settings.Default.Save();
         }
         WindowLocation.GeometryFromString(Properties.Settings.Default.WindowLocation, this);
         new DealWithTheCfgChanging().ReadConfigDetails(ref cd);
         foreach (TabPage control in tabControl1.TabPages)
         {
            foreach (ITab tab in control.Controls.OfType<ITab>())
            {
               tab.cd = cd;
               break;
            }
         }
      }

      // Stop the Tab control from flickering during resize !
      protected override CreateParams CreateParams
      {
         get
         {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
            cp.ExStyle |= 0x00080000;  // Turn on WS_EX_LAYERED
            cp.Style &= ~0x02000000;  // Turn off WS_CLIPCHILDREN
            return cp;
         }
      }

      private readonly StringFormat stringFormat = new StringFormat
      {
         Alignment = StringAlignment.Center,
         LineAlignment = StringAlignment.Center
      };

      private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
      {
         string tabName = tabControl1.TabPages[e.Index].Text;
         //Find if it is selected, this one will be hightlighted...
         if (e.Index == tabControl1.SelectedIndex)
         {
            e.Graphics.FillRectangle(SystemBrushes.GradientActiveCaption, e.Bounds);
         }
         e.Graphics.DrawString(tabName, Font, SystemBrushes.ControlText, tabControl1.GetTabRect(e.Index), stringFormat);
         DrawTabPageBorder(tabControl1.SelectedIndex, SystemBrushes.GradientActiveCaption, e.Graphics);
      }

      private const int highlightWidth = 2;

      private void DrawTabPageBorder(int index, Brush findHighlightColor, Graphics graphics)
      {
         graphics.SmoothingMode = SmoothingMode.HighQuality;
         Rectangle pageBounds = tabControl1.TabPages[index].Bounds;
         if (highlightWidth > 1)
         {
            pageBounds.Inflate(highlightWidth - 1, highlightWidth - 1);
         }
         using (Pen borderPen = new Pen(findHighlightColor, highlightWidth))
         {
            graphics.DrawRectangle(borderPen, pageBounds);
         }
      }

      private void LandingZone_FormClosing(object sender, FormClosingEventArgs e)
      {
         // persist our geometry string.
         Properties.Settings.Default.WindowLocation = WindowLocation.GeometryToString(this);
         Properties.Settings.Default.Save();
      }

      private void TabMainForm_Shown(object sender, System.EventArgs e)
      {
         BringToFront();
         Activate();
      }
   }
}