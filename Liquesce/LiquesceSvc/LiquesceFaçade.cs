using System.ServiceModel;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
   public class LiquesceFaçade : ILiquesce
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceFaçade()
      {
         Log.Debug("Object Created");
      }

      public void Stop()
      {
         Log.Debug("Calling Stop");
         ManagementLayer.Instance.Stop();
      }

      public bool Start()
      {
         Log.Debug("Calling Start");
         return ManagementLayer.Instance.Start();
      }

      public LiquesceSvcState State
      {
         get
         {
            Log.Debug("Calling State");
            return ManagementLayer.Instance.State;
         }
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
