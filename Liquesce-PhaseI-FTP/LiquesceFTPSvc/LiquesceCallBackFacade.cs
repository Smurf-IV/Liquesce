using System;
using System.ServiceModel;
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
   class LiquesceFTPCallBackFacade : ILiquesceFTPCallBack
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceFTPCallBackFacade()
      {
         Log.Debug("Object Created");
      }
      public void Subscribe(Guid id)
      {
         Log.Debug("Calling Subscribe");
         ManagementLayer.Subscribe(id);
      }

      public void Unsubscribe(Guid id)
      {
         Log.Debug("Calling Unsubscribe");
         ManagementLayer.Unsubscribe(id);
      }
   }
}
