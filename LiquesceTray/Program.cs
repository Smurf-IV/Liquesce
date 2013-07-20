#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Smurf-IV">
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
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using LiquesceTray.Properties;
using NLog;

namespace LiquesceTray
{
   static class Program
   {

      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static NotifyIconHandler nih;
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         try
         {
            AppDomain.CurrentDomain.UnhandledException += logUnhandledException;
         }
         catch (Exception ex)
         {
            try
            {
               Log.FatalException("Failed to attach unhandled exception handler...", ex);
            }
            catch
            {
            }
         }
         try
         {
            Log.Error("=====================================================================");
            Log.Error("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version);
            CheckAndRunSingleApp();
         }
         catch (Exception ex)
         {
            Log.FatalException("Exception has not been caught by the rest of the application!", ex);
            MessageBox.Show(ex.Message, "Uncaught Exception - Exiting !");
         }
         finally
         {
            if ((nih != null)
               && (nih.notifyIcon1 != null)
               )
            {
               nih.notifyIcon1.Visible = false;
            }

            Log.Error("File Closing");
            Log.Error("=====================================================================");
         }
      }

      private static void CheckAndRunSingleApp()
      {
         string mutexName = string.Format("{0} [{1}]", Path.GetFileName(Application.ExecutablePath), Environment.UserName);
         bool grantedOwnership;
         using (Mutex appUserMutex = new Mutex(true, mutexName, out grantedOwnership))
         {
            if (grantedOwnership)
            {
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);
               nih = new NotifyIconHandler();
               Application.Run(new HiddenFormToAcceptCloseMessage());
            }
            else
            {
               MessageBox.Show(mutexName + Resources.Program_CheckAndRunSingleApp__is_already_running);
            }
         }
      }

      private static void logUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         try
         {
            Log.Fatal("Unhandled exception.\r\n{0}", e.ExceptionObject);
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
               Log.FatalException("Exception details", ex);
            }
            else
            {
               Log.Fatal("Unexpected exception.");
            }
         }
         catch
         {
         }
      }
   }
}
