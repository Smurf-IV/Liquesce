#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Smurf-IV
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
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LiquesceTrayHelper
{
   static class Program
   {
      static void Main(string[] args)
      {
         try
         {
            AppDomain.CurrentDomain.UnhandledException += logUnhandledException;
         }
         catch (Exception ex)
         {
            try
            {
               MessageBox.Show(ex.Message, "Liquesce Service Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
         }
         try
         {
            foreach (string t in args)
            {
               switch (t.ToLower())
               {
                  case "-debug":
                     Debugger.Launch();
                     break;
                  case "stop":
                     {
                        ServiceController serviceController1 = new ServiceController {ServiceName = "LiquesceSvc"};
                        if (serviceController1.Status != ServiceControllerStatus.Stopped)
                           serviceController1.Stop();
                     }
                     break;
                  case "start":
                     {
                        ServiceController serviceController1 = new ServiceController {ServiceName = "LiquesceSvc"};
                        if (serviceController1.Status != ServiceControllerStatus.Running)
                           serviceController1.Start();
                     }
                     break;
                  case "disablesmb2":
                     {
                        RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\LanmanServer\Parameters");
                        rk.SetValue("Smb2", 0);
                     }
                     break;
                  case "disableoplocks":
                     {
                        RegistryKey rk = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\LanmanServer\Parameters");
                        rk.SetValue("EnableOplocks", 0);
                     }
                     break;
               }
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Liquesce Service Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private static void logUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         try
         {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show(ex != null ? ex.Message : "Unhandled Excpetion", "Liquesce Service Control",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         catch
         {
         }
      }
   }
}
