using System;
using System.ServiceModel;
using LiquesceFacade;
using NLog;

namespace LiquesceSvc
{
   [ServiceBehavior(
      InstanceContextMode = InstanceContextMode.Single,
           ConcurrencyMode = ConcurrencyMode.Multiple,
           IncludeExceptionDetailInFaults = true
           )
   ]
   class LiquesceCallBackFacade : ILiquesceCallBack
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceCallBackFacade()
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
