#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Smurf-IV">
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
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;
using NLog;

namespace LiquesceSvc
{
   static class Program
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      /// 
      
      static void Main(string[] args)
      {
         Log.Error("=====================================================================");
         Log.Error("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
         var runner = new LiquesceService();
         if ((args.Length > 0) && ("/debug" == args[0].ToLower()))
         {
            // main service object
            LiquesceService.RunningAsService = false;
            runner.StartService(args);
            Console.WriteLine("Press Q to quit");
            Application.Run();
            runner.StopService();
            // We called the static run, so call the static exit
            Application.Exit();
         }
         else
         {
            LiquesceService.RunningAsService = true;
            ServiceBase.Run(new ServiceBase[] { runner });
         }
         Log.Error("========================Clean=Exit===================================");
      }
      /*
      static void Main()
      {
         Log.Error("=====================================================================");
         Log.Error("File Re-opened: Ver :" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
         var runner = new LiquesceService();
         {
            LiquesceService.RunningAsService = true;
            ServiceBase.Run(new ServiceBase[] { runner });
         }
         Log.Error("========================Clean=Exit===================================");
      }
       * */
   }
}
