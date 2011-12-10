using System;
using System.ServiceModel;

namespace LiquesceFacade
{
   public class LiquesceCallBackProxy : DuplexClientBase<ILiquesceCallBack>, ILiquesceCallBack
   {

      public LiquesceCallBackProxy(InstanceContext callbackInstance)
         : base(callbackInstance)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName)
         : base(callbackInstance, endpointConfigurationName)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress)
         : base(callbackInstance, endpointConfigurationName, remoteAddress)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
         : base(callbackInstance, endpointConfigurationName, remoteAddress)
      {
      }

      public LiquesceCallBackProxy(InstanceContext callbackInstance, System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress)
         : base(callbackInstance, binding, remoteAddress)
      {
      }

      #region Implementation of ILiquesceCallBack

      public void Subscribe(Client id)
      {
         try
         {
            base.Channel.Subscribe(id);
         }
         catch (EndpointNotFoundException ex)
         {

            throw new ApplicationException("Liquesce service is off. Please start the Liquesce server first then try to connect",ex);
         }
         catch (CommunicationException ex)
         {

            throw new ApplicationException("Liquesce Service is off. Please start the Liquesce server first then try to connect", ex);
         }
      }

      public void Unsubscribe(Client id)
      {
         base.Channel.Unsubscribe(id);
      }

      #endregion
   }
}
