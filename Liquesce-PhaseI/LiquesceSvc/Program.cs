using System.ServiceProcess;
using NLog;

namespace LiquesceSvc
{
   static class Program
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      static void Main()
      {
         Log.Error("=====================================================================");
         Log.Error("File Re-opened: " + System.DateTime.UtcNow);
         ServiceBase.Run(new ServiceBase[] { new LiquesceService() });
         Log.Error("========================Clean=Exit===================================");
      }
   }
}
