using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using LiquesceFTPFacade;
using NLog;

namespace LiquesceFTPSvc
{
   [ServiceBehavior(
      InstanceContextMode = InstanceContextMode.Single,
           ConcurrencyMode = ConcurrencyMode.Multiple,
           IncludeExceptionDetailInFaults = true
           )
   ]
   public class LiquesceFTPFacade : ILiquesceFTP
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceFTPFacade()
      {
         Log.Debug("Object Created");
      }

      public void Stop()
      {
         Log.Debug("Calling Stop");
         ManagementLayer.Instance.Stop();
      }

      public void Start()
      {
         Log.Debug("Calling Start");
         // Queue the main work as a thread pool task as we want this method to finish promptly.
         ThreadPool.QueueUserWorkItem(ManagementLayer.Instance.Start);
      }

      public LiquesceFTPSvcState State
      {
         get
         {
            Log.Debug("Calling State");
            return ManagementLayer.Instance.State;
         }
      }

      public List<LanManShareDetails> GetPossibleShares()
      {
          Log.Debug("Calling GetPossibleShares");
          return ManagementLayer.Instance.GetPossibleShares();
      }

       
      public ConfigDetails ConfigDetails
      {
         get
         {
            Log.Debug("Calling get_ConfigDetails");
            return ManagementLayer.Instance.CurrentConfigDetails;
         }
         set
         {
            Log.Debug("Calling set_ConfigDetails");
            ManagementLayer.Instance.CurrentConfigDetails = value;
         }
      }

   }
}
