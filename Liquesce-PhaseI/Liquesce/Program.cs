using System;
using System.Reflection;
using System.Windows.Forms;
using NLog;

namespace Liquesce
{
   static class Program
   {
      
      private static readonly Logger Log = LogManager.GetLogger("Program");
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
         }
         catch (Exception ex)
         {
            Log.Fatal("Exception has not been caught by the rest of the application!", ex);
            MessageBox.Show(ex.Message, "Uncaught Exception - Exiting !");
         }
         finally
         {
            Log.Error("File Closing");
            Log.Error("=====================================================================");
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
