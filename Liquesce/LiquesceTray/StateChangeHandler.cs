using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiquesceFaçade;
using System.ServiceModel;

namespace LiquesceTray
{

   class StateChangeHandler : ILiquesceCallback.ILiquesceCallback
   {
      private ILiquesceCallback.LiquesceClient client;
      private Guid guid = Guid.NewGuid();

      public delegate void SetStateDelegate(LiquesceSvcState state, string text);
      private SetStateDelegate setStateDelegate;

      public void CreateCallBack( SetStateDelegate setStateDelegate)
      {
         InstanceContext context = new InstanceContext(this);
         client = new ILiquesceCallback.LiquesceClient(context);
         client.Subscribe(guid);
         this.setStateDelegate = setStateDelegate;
      }

      public void RemoveCallback()
      {
         if (client != null)
         {
            client.Unsubscribe(guid);
            client = null;
            setStateDelegate = null;
         }
      }

      public void Update(LiquesceSvcState state, string message)
      {
         SetStateDelegate handler = setStateDelegate;
         if ( handler != null )
            handler(state, message);
      }
   }
}
