using System;
using System.Diagnostics;
using System.Reflection;
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

      protected override void OnStart(string[] args)
      {
         Log.Info("OnStart");
         try
         {
            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up
            foreach ( string arg in args )
            {
               Log.Debug( arg );
               switch (arg.ToUpper())
               {
                  case "-DEBUG":
                     Log.Info( "Launching debugger." );
                     Debugger.Launch( );
                     break;
               }
            }
            // This line obtains the location of the config file by assuming that
            // it is in the same directory as the location of the assembly that we
            // are inside (then appending ".config"). This is needed in the service
            // since the working directory will be c:\windows\system32. 
            string configPath = Assembly.GetExecutingAssembly().Location + ".config";
            Log.Info("Loading configuration from " + configPath);
            RemotingStartHelper.Configure(configPath, false, this);

            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up
            base.OnStart(args);

            Log.Info("Create Management object to hold the listeners etc.");
            RequestAdditionalTime(30000); // let the SCM know that this part could take a while due to other services starting up

            // Queue the main work as a thread pool task as we want this method to finish promptly.
			   ThreadPool.QueueUserWorkItem(ThreadProc, this);
         }
         catch ( System.Runtime.Remoting.RemotingException e )
         {
            base.EventLog.WriteEntry(e.Message, EventLogEntryType.Error);
            OnStop();
            Stop();
            throw;
         }
         catch ( Exception ex )
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
         Log.Info("LiquesceService object starting.");
         LiquesceService me = stateInfo as LiquesceService;
         try
         {
            Log.Info("Running Assembly Information.");

            if ( !ManagementLayer.Instance.Start() )
            {
               Log.Error("ManagementLayer startup error.");
               me.OnStop();
               ((ServiceBase)me)/*.base*/.Stop();
            }

            Log.Info("LiquesceService started.");
         }
         catch ( Exception ex )
         {
            Log.ErrorException("LiquesceService startup error.", ex);
            me.OnStop();
            ((ServiceBase)me)/*.base*/.Stop();
            throw;
         }
      }

      /// <summary>
      /// "Play nice" with exceptions so that the service can exit when asked (Forced)
      /// </summary>
      protected override void OnStop( )
      {
         try
         {
            Log.Info("Stop the ManagementLayer and remove");
            RequestAdditionalTime(30000);
            base.Stop();
            ManagementLayer.Instance.Stop();

            Log.Info("LiquesceService stopped.");
         }
         catch (Exception ex)
         {
            Log.ErrorException("LiquesceService Shutdown error.", ex);
         }
         finally
         {
            base.OnStop();
         }
      }

   }
}
