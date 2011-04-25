using System.Collections.Generic;
using System.Threading;
using ClientLiquesceFTPTray.Dokan;

namespace ClientLiquesceFTPTray
{
   internal static class Handlers
   {
      public static readonly Dictionary<string, HandleMappingThread> ClientMappings = new Dictionary<string, HandleMappingThread>();
   }

   class HandleMappingThread
   {
      private DokanManagement mapManager;

      public bool Start(ClientShareDetail csd)
      {
         if (mapManager != null)
            mapManager.Stop();
         
         mapManager = new DokanManagement {csd = csd};
         ThreadPool.QueueUserWorkItem(mapManager.Start, mapManager);
         int repeatWait = 10;
         while (!mapManager.IsRunning
            && (repeatWait-- > 0)
            )
         {
            Thread.Sleep(250);
         }

         return mapManager.IsRunning;
      }

      public bool Stop()
      {
         bool runningState;
         if (mapManager != null)
         {
            mapManager.Stop();
            int repeatWait = 10;
            while (mapManager.IsRunning
               && (repeatWait-- > 0)
               )
            {
               Thread.Sleep(250);
            }
            runningState = mapManager.IsRunning;
         }
         else
         {
            runningState = true;
         }
         return ! runningState;
      }
   }
}
