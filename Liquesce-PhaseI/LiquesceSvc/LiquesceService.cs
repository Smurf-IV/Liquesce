#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceService.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using NLog;

namespace LiquesceSvc
{
   public partial class LiquesceService : ServiceBase
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();


      public LiquesceService()
      {
         try
         {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
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
         Log.Info("LiquesceService object.");
         InitializeComponent();
      }

      public static bool RunningAsService
      { 
         get; 
         set; 
      }

      private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
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

      private static ServiceHost _ILiquesceHost;
      private static ServiceHost _ILiquesceHostCallBack;

      public void StartService(string[] args)
      { 
         OnStart(args); 
      }

      public void StopService()
      { 
         OnStop(); 
      }

      protected override void OnStart(string[] args)
      {
         Log.Info("OnStart");
         try
         {
            if (RunningAsService)
               RequestAdditionalTime(30000);
               // let the SCM know that this part could take a while due to other services starting up
            foreach (string arg in args)
            {
               Log.Debug(arg);
               switch (arg.ToUpper())
               {
                  case "-DEBUG":
                     Log.Info("Launching debugger.");
                     Debugger.Launch();
                     break;
               }
            }
            _ILiquesceHost = new ServiceHost(typeof(LiquesceFacade));
            _ILiquesceHostCallBack = new ServiceHost(typeof(LiquesceCallBackFacade));
            _ILiquesceHost.Open();
            _ILiquesceHostCallBack.Open();

            if (RunningAsService)
            {
               RequestAdditionalTime(30000);
                  // let the SCM know that this part could take a while due to other services starting up
               base.OnStart(args);

               Log.Info("Create Management object to hold the listeners etc.");
               RequestAdditionalTime(30000);
                  // let the SCM know that this part could take a while due to other services starting up
            }
            // Queue the main work as a thread pool task as we want this method to finish promptly.
            ThreadPool.QueueUserWorkItem(ThreadProc, this);
         }
         catch (Exception ex)
         {
            /*
Windows Server 2003/Windows XP - use the HttpCfg.exe tool
Windows 7/Windows Server 2008 - configure these settings with the Netsh.exe tool (you need to deal with UAC here). The steps are mentioned below:
1. Go to Start > Accessories > Command Prompt > Right-Click (Run as Administrator)
2. Execute this at the command prompt:
HTTP could not register URL http://+:8731/Design_Time_Addresses/LiquesceSvc/LiquesceCallBackFacade/. Your process does not have access rights to this namespace (see http://go.microsoft.com/fwlink/?LinkId=70353 for details).
    netsh http add urlacl url=http://+:8000/OrderManagerService user=DOMAIN\username
    8000 here is your port number, you can replace this with a port number of  your choice (using which your WCF service is hosted)            
             */
            Log.ErrorException("LiquesceService startup error.", ex);
               base.EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
               OnStop();
               if (RunningAsService)
                  Stop();
            throw;
         }
      }

      /// <summary>
      /// This thread routine performs the task of initializing the ManagementLayer.
      /// </summary>
      /// <param name="stateInfo">Object passed into the method for context info.</param>
      static void ThreadProc(Object stateInfo)
      {
         Log.Info("LiquesceService object starting.");
         LiquesceService me = stateInfo as LiquesceService;
         try
         {
            Log.Info("Running Assembly Information.");

            ManagementLayer.Instance.Start(null);
            Log.Info("Blocking thread ManagementLayer.Instance.Start has exited.");
         }
         catch (Exception ex)
         {
            Log.ErrorException("LiquesceService startup error.", ex);
            if (me != null)
            {
               me.OnStop();
               if (RunningAsService)
                  ((ServiceBase)me)/*.base*/.Stop();
            }
            throw;
         }
      }

      /// <summary>
      /// "Play nice" with exceptions so that the service can exit when asked (Forced)
      /// </summary>
      protected override void OnStop()
      {
         try
         {
            Log.Info("Stop the ManagementLayer and remove");
            if (RunningAsService)
               RequestAdditionalTime(30000);
            
            // Then stop the host calling in
            if ( _ILiquesceHost != null )
               _ILiquesceHost.Close();
            // Now stop the drives
            ManagementLayer.Instance.Stop();

            // Now stop the callbacks that would be firing out stating the stop has just occurred
            if (_ILiquesceHostCallBack != null)
               _ILiquesceHostCallBack.Close();

            Log.Info("LiquesceService stopped.");
         }
         catch (Exception ex)
         {
            Log.ErrorException("LiquesceService Shutdown error.", ex);
         }
         finally
         {
            Log.Info("base.OnStop()");
            if (RunningAsService)
               base.OnStop();
         }
      }

   }
}
