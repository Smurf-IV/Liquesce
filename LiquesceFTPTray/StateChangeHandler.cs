using System;
using System.ServiceModel;
using LiquesceFTPFacade;
using NLog;

namespace LiquesceFTPTray
{
   public class StateChangeHandler : LiquesceFTPCallbackSvcRef.ILiquesceFTPCallBackCallback
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private LiquesceFTPCallbackSvcRef.LiquesceFTPCallBackClient client;
      private readonly Guid guid = Guid.NewGuid();

      public delegate void SetStateDelegate(LiquesceFTPSvcState state, string text);
      private SetStateDelegate setStateDelegate;

      public void CreateCallBack( SetStateDelegate newDelegate)
      {
         try
         {
            InstanceContext context = new InstanceContext(this);
            client = new LiquesceFTPCallbackSvcRef.LiquesceFTPCallBackClient(context);
            client.Subscribe(guid);
            setStateDelegate = newDelegate;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateCallBack:", ex);
            Update(LiquesceFTPSvcState.InError, ex.Message);
            client = null;
            setStateDelegate = null;
         }
      }

      public void RemoveCallback()
      {
         try
         {
            if (client != null)
            {
               client.Unsubscribe(guid);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("RemoveCallback:", ex);
            Update(LiquesceFTPSvcState.InError, ex.Message);
         }
         finally
         {
            client = null;
            setStateDelegate = null;
         }
      }

      #region Implementation of ILiquesceFTPCallback

      public void Update(LiquesceFTPSvcState state, string message)
      {
         SetStateDelegate handler = setStateDelegate;
         if (handler != null)
            handler(state, message);
      }

      #endregion

   }
}
