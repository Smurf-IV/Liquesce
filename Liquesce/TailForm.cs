#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="TailForm.cs" company="Smurf-IV">
//
//  Copyright (C) 2012-2014 Smurf-IV
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Liquesce.Properties;
using LiquesceTray;

namespace Liquesce
{
   public partial class TailForm : Form
   {
      private readonly string logLocation;
      private long previousSeekPosition;

      public TailForm(string logLocation)
      {
         this.logLocation = logLocation;
         InitializeComponent();
         WindowLocation.GeometryFromString(Settings.Default.TailWindowLocation, this);
      }

      private void timer1_Tick(object sender, EventArgs e)
      {
         if (!Visible
            || String.IsNullOrEmpty(logLocation)
            )
         {
            return;
         }
         try
         {
            lock (this)
            {
               using (StreamReader reader =
                  new StreamReader(new FileStream(logLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
               {
                  //if the file size has not changed, idle

                  long b = reader.BaseStream.Length;
                  if (b != previousSeekPosition)
                  {
                     if (b > previousSeekPosition) //seek to the last max offset
                     {
                        reader.BaseStream.Seek(previousSeekPosition, SeekOrigin.Begin);
                     }
                     //read out of the file until the EOF
                     textBox1.Text += reader.ReadToEnd();

                     //update the last max offset
                     previousSeekPosition = reader.BaseStream.Position;

                     //  Make sure new text is visible
                     textBox1.SelectionStart = textBox1.Text.Length;
                     textBox1.ScrollToCaret();
                  }
               }
            }
         }
         catch { }
      }

      private void TailForm_Shown(object sender, EventArgs e)
      {
         Text += " " + logLocation;
         timer1.Enabled = true;
      }

      private void TailForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         Hide();
         timer1.Enabled = false;
         // persist our geometry string.
         Settings.Default.TailWindowLocation = WindowLocation.GeometryToString(this);
         Settings.Default.Save();
      }

      private void toolStripMenuItem1_Click(object sender, EventArgs e)
      {
         lock (this)
         {
            textBox1.Clear();
         }
      }

      private void toolStripMenuItem2_Click(object sender, EventArgs e)
      {
         timer1.Enabled = false;
         textBox1.BackColor = SystemColors.ControlLight;
      }

      private void toolStripMenuItem3_Click(object sender, EventArgs e)
      {
         timer1.Enabled = true;
         textBox1.BackColor = SystemColors.Window;
      }
   }
}