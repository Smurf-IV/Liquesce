#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="DropZone.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 fpFragon
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
using System.ServiceModel;
using LiquesceFacade;
using System.IO;

namespace LiquesceTray
{
   public partial class DropZone : Form
   {
      private ConfigDetails config;

      public DropZone()
      {
         InitializeComponent();
      }

      private void Dropper_DragEnter(object sender, DragEventArgs e)
      {
         e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
      }

      private void Dropper_DragDrop(object sender, DragEventArgs e)
      {
         string[] strFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
         GetAllRoots(strFiles[0]);
      }

      private void DropZone_Load(object sender, EventArgs e)
      {
         GetConfig();

      }

      private bool GetConfig()
      {
         bool value = true;
         try
         {
            EndpointAddress endpointAddress = new EndpointAddress("net.pipe://localhost/LiquesceFacade");
            NetNamedPipeBinding namedPipeBindingpublish = new NetNamedPipeBinding();
            LiquesceProxy proxy = new LiquesceProxy(namedPipeBindingpublish, endpointAddress);

            config = proxy.ConfigDetails;
         }
         catch
         {
            value = false;
         }
         return value;
      }


      private void GetAllRoots(string liquescePath)
      {
         textBox1.Text = liquescePath;

         listBox1.Items.Clear();

         // check if path is on liquesce drive
         if (liquescePath[0].ToString() == config.DriveLetter)
         {
            // cut drive letter and :
            string relative = liquescePath.Substring(2);

            for (int i = 0; i < config.SourceLocations.Count; i++)
            {
               string root = config.SourceLocations[i];

               if (File.Exists(root + relative))
               {
                  listBox1.Items.Add(root + relative);
               }

               if (Directory.Exists(root + relative))
               {
                  listBox1.Items.Add(root + relative);
               }
            }
         }
         else
         {
            listBox1.Items.Add("File is not in Liquesce Drive.");
         }
      }

   }
}
