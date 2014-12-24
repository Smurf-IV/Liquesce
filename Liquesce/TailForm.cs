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
using System.Collections.Generic;
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
      private Color lastColor;

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
               List<string> linesToProcess = new List<string>();
               long currentLength = 0;
               using (StreamReader reader =
                  new StreamReader(new FileStream(logLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024,
                     FileOptions.SequentialScan)))
               {
                  //if the file size has not changed, idle

                  currentLength = reader.BaseStream.Length;
                  if (currentLength == previousSeekPosition)
                  {
                     return;
                  }
                  textBox1._Paint = false; // turn off flag to ignore WM_PAINT messages
                  if (currentLength < previousSeekPosition)
                  {
                     textBox1.Clear();
                     previousSeekPosition = 0;
                  }
                  else //seek to the last max offset
                  {
                     reader.BaseStream.Seek(previousSeekPosition, SeekOrigin.Begin);
                  }
                  // Read out of the file until the EOF
                  int linesToBeDone = 0;
                  while (!reader.EndOfStream
                     && (++linesToBeDone < 100) // Give the display a chance to display something rather than looking like it has hung !
                     )
                  {
                     linesToProcess.Add(reader.ReadLine());
                  }
                  previousSeekPosition = reader.BaseStream.Position;
               }
               foreach (string line in linesToProcess)
               {
                  int textLength = textBox1.TextLength;
                  textBox1.Select(textLength, 0);
                  if (line.Length > 29)
                  {
                     string trim = line.Substring(28, 2).Trim();
                     switch (trim[0])
                     {
                        case 'F':
                           textBox1.SelectionColor = Color.DarkViolet;
                           break;

                        case 'E':
                           textBox1.SelectionColor = Color.Red;
                           break;

                        case 'W':
                           textBox1.SelectionColor = Color.RoyalBlue;
                           break;

                        case 'I':
                           textBox1.SelectionColor = Color.Black;
                           break;

                        case 'D':
                           textBox1.SelectionColor = Color.DarkGray;
                           break;

                        case 'T':
                           textBox1.SelectionColor = Color.DimGray;
                           break;

                        default:
                           // Leave it as is
                           textBox1.SelectionColor = lastColor;
                           break;
                     }
                  }
                  lastColor = textBox1.SelectionColor;
                  textBox1.AppendText(line + Environment.NewLine);
               }
            }
         }
         catch
         {
         }
         finally
         {
            textBox1._Paint = true;// restore flag so we can paint the control
         }
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