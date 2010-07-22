using System;
using System.Windows.Forms;
using NLog;

namespace Liquesce
{
   static class Program
   {
      
      private static readonly Logger log = LogManager.GetLogger("Program");
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
               log.FatalException("Failed to attach unhandled exception handler...", ex);
            }
            catch (Exception)
            {
            }
         }
         try
         {
            log.Error("=====================================================================");
            log.Error(String.Concat("File Re-opened: ", DateTime.UtcNow));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
         }
         catch (Exception ex)
         {
            log.Fatal("Exception has not been caught by the rest of the application!", ex);
            MessageBox.Show(ex.Message, "Uncaught Exception - Exiting !");
         }
         finally
         {
            log.Error("File Closing");
            log.Error("=====================================================================");
         }
      }
      private static void logUnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         try
         {
            log.Fatal("Unhandled exception.\r\n{0}", e.ExceptionObject);
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
               log.FatalException("Exception details", ex);
            }
            else
            {
               log.Fatal("Unexpected exception.");
            }
         }
         catch (Exception)
         {
         }
      }
   }
}
