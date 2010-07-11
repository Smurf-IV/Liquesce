using System.ServiceProcess;

namespace Liquesce
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      static void Main()
      {
         ServiceBase[] servicesToRun = new ServiceBase[] 
                                          { 
                                             new Main() 
                                          };
         ServiceBase.Run(servicesToRun);
      }
   }
}
