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

#endregion Copyright (C)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
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
         DoubleBuffered = true;
      }

      private TailForm tailForm;

      private void btnServiceTail_Click(object sender, EventArgs e)
      {
         try
         {
            if (tailForm == null)
            {
               string logLocation = DisplayLog.FindLogLocation(@"LiquesceSvc\Logs\LiquesceSvc.log");
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

      /// <summary>
      ///
      /// </summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      /// <remarks>
      /// Wanted to do an emailer, but that then prevent the user from creating issues,
      /// and adding their own text.
      /// Also the user needs to know their SMTP / Gmail server, account and passord details,
      /// everytime.
      /// </remarks>
      private void btnZipAndEmail_Click(object sender, EventArgs e)
      {
         string zipFileName = Path.Combine(Path.GetTempPath(), DateTime.Now.ToString("u").Replace(":", "_")) + ".zip";
         // Note: The resulting .zip file will produce a file called 
         //       [Content_Types].xml when it is unZIPped, which contains content 
         //       information about the types of files included in the .zip file.
         using (Package zipPackage = Package.Open(zipFileName, FileMode.Create))
         {
            foreach (FileInfo fileInfo in GetFilesToCompress())
            {
               string zipURI = string.Concat("/", fileInfo.Name.Replace(" ", "_"));
               Uri partURI = new Uri(zipURI, UriKind.Relative);
               PackagePart packagePart = zipPackage.CreatePart(partURI, System.Net.Mime.MediaTypeNames.Application.Zip,
                  CompressionOption.Maximum);
               using (FileStream fileStream = fileInfo.OpenRead())
               {
                  fileStream.CopyTo(packagePart.GetStream());
               }
            }
         }
         Clipboard.SetText(zipFileName);
         Clipboard.SetText(zipFileName);
         MessageBox.Show(this,
            "The following filename has been copied into the clipboard for easy reference / access" + Environment.NewLine +
            zipFileName  + Environment.NewLine  + Environment.NewLine +
            "You will need to delete this after use.",
            "Current Zip FileName"
            );
      }

      private IEnumerable<FileInfo> GetFilesToCompress()
      {
         foreach (FileInfo fileInfo in new DirectoryInfo(DisplayLog.FindLogLocation(@"LiquesceSvc")).EnumerateFiles("*", SearchOption.AllDirectories))
         {
            yield return fileInfo;
         }
         foreach (FileInfo fileInfo in new DirectoryInfo(DisplayLog.FindLogLocation(@"Liquesce")).EnumerateFiles("*", SearchOption.AllDirectories))
         {
            yield return fileInfo;
         }
      }
   }
}