using System;
using System.ServiceModel;
using LiquesceFaçade;
using NLog;

namespace LiquesceTray
{
   public class StateChangeHandler : LiquesceCallbackReference.ILiquesceCallBackCallback
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private LiquesceCallbackReference.LiquesceCallBackClient client;
      private readonly Guid guid = Guid.NewGuid();

      public delegate void SetStateDelegate(LiquesceSvcState state, string text);
      private SetStateDelegate setStateDelegate;

      public void CreateCallBack( SetStateDelegate newDelegate)
      {
         try
         {
            InstanceContext context = new InstanceContext(this);
            client = new LiquesceCallbackReference.LiquesceCallBackClient(context);
            client.Subscribe(guid);
            setStateDelegate = newDelegate;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateCallBack:", ex);
            Update(LiquesceSvcState.InError, ex.Message);
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
            Update(LiquesceSvcState.InError, ex.Message);
         }
         finally
         {
            client = null;
            setStateDelegate = null;
         }
      }

      #region Implementation of ILiquesceCallback

      public void Update(LiquesceSvcState state, string message)
      {
         SetStateDelegate handler = setStateDelegate;
         if (handler != null)
            handler(state, message);
      }

      #endregion
   }
}
