using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace LiquesceFTPSvc
{
   static class Program
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      static void Main(string[] args)
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
         try
         {
            Log.Error("=====================================================================");
            Log.Error("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var runner = new LiquesceFTPSvc()
                           {
                              RunningAsService = !Environment.UserInteractive
                           };
            if (!runner.RunningAsService)
            {
               Console.WriteLine("Starting Service in Conole mode with launch user access rights only !");
               Console.WriteLine("You are about to be bombarded with the internal logging of this application");
               Console.WriteLine("Press Q to quit");
               Thread.Sleep(1000);
               // Have to create a logger target as this will not have any conf file to use
               LoggingConfiguration config = LogManager.Configuration;
               ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
               consoleTarget.Layout += " ${exception:format=ToString}";
               config.AddTarget("consoleTarget", consoleTarget);
               LoggingRule rule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
               config.LoggingRules.Add(rule);
               LogManager.Configuration = config;
               Log.Fatal("Press Q to quit");
               // main service object
               runner.StartService(args);
               bool exit = false;
               do
               {
                  switch (Console.ReadKey().KeyChar)
                  {
                     case 'Q':
                     case 'q':
                        exit = true;
                        break;
                  }
               } while (!exit);
               runner.StopService();
               // We called the static run, so call the static exit
               Application.Exit();
            }
            else
            {
               ServiceBase.Run(new ServiceBase[] { runner });
            }
         }
         catch (Exception ex)
         {
            Log.Fatal("Exception has not been caught by the rest of the application!", ex);
         }
         finally
         {
            Log.Error("File Closing");
            Log.Error("=====================================================================");
         }
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

   }
}
