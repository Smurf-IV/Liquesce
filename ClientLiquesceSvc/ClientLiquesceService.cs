using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using NLog;

namespace ClientLiquesceSvc
{
   public partial class ClientLiquesceService : ServiceBase
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();


      public ClientLiquesceService()
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
         Log.Info("ClientLiquesceService object.");
         InitializeComponent();
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

      protected override void OnStart(string[] args)
      {
         Log.Info("OnStart");
         try
         {
            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up
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
            //_ILiquesceHost = new ServiceHost(typeof(LiquesceFacade));
            //_ILiquesceHostCallBack = new ServiceHost(typeof(LiquesceCallBackFacade));
            //_ILiquesceHost.Open();
            //_ILiquesceHostCallBack.Open();

            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up
            base.OnStart(args);

            Log.Info("Create Management object to hold the listeners etc.");
            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up

            // Queue the main work as a thread pool task as we want this method to finish promptly.
            ThreadPool.QueueUserWorkItem(ThreadProc, this);
         }
         catch (System.Runtime.Remoting.RemotingException e)
         {
            base.EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            OnStop();
            Stop();
            throw;
         }
         catch (Exception ex)
         {
            Log.ErrorException("LiquesceService startup error.", ex);
            base.EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
            OnStop();
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
         Log.Info("ClientLiquesceService object starting.");
         ClientLiquesceService me = stateInfo as ClientLiquesceService;
         try
         {
            Log.Info("Running Assembly Information.");

            ManagementLayer.Instance.Start();
            Log.Info("Blocking thread ManagementLayer.Instance.Start has exited.");
         }
         catch (Exception ex)
         {
            Log.ErrorException("LiquesceService startup error.", ex);
            if (me != null)
            {
               me.OnStop();
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
            RequestAdditionalTime(30000);
            if ( _ILiquesceHost != null )
               _ILiquesceHost.Close();
            if ( _ILiquesceHostCallBack != null )
               _ILiquesceHostCallBack.Close();
            ManagementLayer.Instance.Stop();

            Log.Info("ClientLiquesceService stopped.");
         }
         catch (Exception ex)
         {
            Log.ErrorException("ClientLiquesceService Shutdown error.", ex);
         }
         finally
         {
            Log.Info("base.OnStop()");
            base.OnStop();
         }
      }

   }
}
