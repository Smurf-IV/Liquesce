using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   [ServiceBehavior(
      InstanceContextMode = InstanceContextMode.Single,
           ConcurrencyMode = ConcurrencyMode.Multiple,
           IncludeExceptionDetailInFaults = true
           )
   ]
   class LiquesceCallBackFaçade : ILiquesceCallBack
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceCallBackFaçade()
      {
         Log.Debug("Object Created");
      }
      public void Subscribe(Guid id)
      {
         Log.Debug("Calling Subscribe");
         ManagementLayer.Instance.Subscribe(id);
      }

      public void Unsubscribe(Guid id)
      {
         Log.Debug("Calling Unsubscribe");
         ManagementLayer.Instance.Unsubscribe(id);
      }
   }
}
