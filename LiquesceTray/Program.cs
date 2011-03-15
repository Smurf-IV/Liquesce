using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
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
            if (args.Length > 0)
            {
               TrayHelper(args);
            }
            else
            {
               // Create a mutex name for this App + user.
               CheckAndRunSingleApp();
            }
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
         string MutexName = string.Format("{0} [{1}]", Path.GetFileName(Application.ExecutablePath), Environment.UserName);
         bool GrantedOwnership;
         using (Mutex AppUserMutex = new Mutex(true, MutexName, out GrantedOwnership))
         {
            if (GrantedOwnership)
            {
               Application.EnableVisualStyles();
               Application.SetCompatibleTextRenderingDefault(false);
               nih = new NotifyIconHandler();
               Application.Run(new HiddenFormToAcceptCloseMessage());
            }
            else
            {
               MessageBox.Show(MutexName + " is already running");
            }
         }
      }

      private static void TrayHelper(IList<string> args)
      {
         if (args == null) 
            throw new ArgumentNullException("args");
         try
         {
            int argsLength = args.Count;

            ServiceController serviceController1 = new ServiceController { ServiceName = "LiquesceSvc" };
            for (int index = 0; index < argsLength; index++)
            {
               Log.Debug("Arg[{0}]={1}", index, args[index]);
               switch (args[index].ToLower())
               {
                  case "-debug":
                     Debugger.Launch();
                     break;
                  case "stop":
                     serviceController1.Stop();
                     break;
                  case "start":
                     serviceController1.Start();
                     break;
               }
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("TrayHelper threw an Exception", ex);
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
