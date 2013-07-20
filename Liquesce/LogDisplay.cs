#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LogDisplay.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using NLog;

namespace Liquesce
{
   /// <summary>
   /// 
   /// </summary>
   static public class DisplayLog
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      static public string FindLogLocation(string logLocation)
      {
            OpenFileDialog openFileDialog = new OpenFileDialog
                                               {
                                                  InitialDirectory =
                                                     Path.Combine(
                                                        Environment.GetFolderPath(
                                                           Environment.SpecialFolder.CommonApplicationData), logLocation),
                                                  Filter = "Log files (*.log)|*.log|Archive logs (*.*)|*.*",
                                                  FileName = "*.log",
                                                  FilterIndex = 2,
                                                  Title = "Select name to view contents"
                                               };

            return (openFileDialog.ShowDialog() == DialogResult.OK) ? openFileDialog.FileName : string.Empty;
      }
      /// <summary>
      /// 
      /// </summary>
      static public void LogDisplay(string logLocation)
      {
         try
         {
            logLocation = FindLogLocation(logLocation);
            if (!string.IsNullOrEmpty(logLocation))
            {
               Process word = Process.Start("Wordpad.exe", '"' + logLocation + '"');
               if (word != null)
               {
                  word.WaitForInputIdle();
                  SendKeys.SendWait("^{END}");
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenFile has an exception: ", ex);
         }
      }
   }
}