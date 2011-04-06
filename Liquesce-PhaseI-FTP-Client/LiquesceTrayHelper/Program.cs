using System.Diagnostics;
using System.ServiceProcess;

namespace LiquesceTrayHelper
{
   static class Program
   {
      static void Main(string[] args)
      {
         ServiceController serviceController1 = new ServiceController { ServiceName = "LiquesceSvc" };
         for (int index = 0; index < args.Length; index++)
         {
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
   }
}
