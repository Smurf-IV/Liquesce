﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;
using NLog;

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
         Log.Error("=====================================================================");
         Log.Error("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
         var runner = new LiquesceFTPSvc();
         if ((args.Length > 0) && ("/debug" == args[0].ToLower()))
         {
            // main service object
            LiquesceFTPSvc.RunningAsService = false;
            runner.StartService(args);
            Console.WriteLine("Press Q to quit");
            Application.Run();
            runner.StopService();
            // We called the static run, so call the static exit
            Application.Exit();
         }
         else
         {
            LiquesceFTPSvc.RunningAsService = true;
            ServiceBase.Run(new ServiceBase[] { runner });
         }
         Log.Error("========================Clean=Exit===================================");
      }
   }
}