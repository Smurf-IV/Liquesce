﻿using System.Security.Principal;
using System.ServiceProcess;
using NLog;

namespace ClientLiquesceSvc
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
         ServiceBase.Run(new ServiceBase[] { new ClientLiquesceService() });
         Log.Error("========================Clean=Exit===================================");
      }
   }
}