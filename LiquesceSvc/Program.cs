#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Smurf-IV">
//
//  Copyright (C) 2010-2014 Simon Coghlan (Aka Smurf-IV)
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
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;
using NLog;

namespace LiquesceSvc
{
   internal static class Program
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      /// <remarks>
      /// Option to either start from SCM or from Console to allow debugging
      /// </remarks>
      private static void Main(string[] args)
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
            Log.Fatal("=====================================================================");
            Log.Fatal("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version);
            LiquesceService runner = new LiquesceService();
            if ((args.Length > 0) && ("/debug" == args[0].ToLower()))
            {
               // main service object
               LiquesceService.RunningAsService = false;
               runner.StartService(args);
               Console.WriteLine("Press Q to quit");
               Application.Run();
               runner.StopService();
               // We called the static run, so call the static exit
               Application.Exit();
            }
            else
            {
               LiquesceService.RunningAsService = true;
               ServiceBase.Run(new ServiceBase[] { runner });
            }
         }
         catch (Exception ex)
         {
            Log.FatalException("Exception has not been caught by the rest of the application!", ex);
            throw; // Force the log out to the logUnhandledException handler
         }
         finally
         {
            Log.Fatal("File Closing");
            Log.Fatal("=====================================================================");
         }
      }

      private static void logUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         try
         {
            string cs = Assembly.GetExecutingAssembly().GetName().Name;
            EventLog eventLog = new EventLog();
            if (!EventLog.SourceExists(cs))
            {
               EventLog.CreateEventSource(cs, "Application");
            }

            EventLog.WriteEntry(cs, e.ExceptionObject.ToString(), EventLogEntryType.Error);
            Log.Fatal("Unhandled exception.\r\n{0}", e.ExceptionObject);
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
               Log.FatalException("Exception details", ex);
               EventLog.WriteEntry(cs, ex.ToString(), EventLogEntryType.Error);
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