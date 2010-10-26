using System;
using System.ServiceModel;

namespace LiquesceFacade
{
   public interface IClientStateChange
   {
      [OperationContract(IsOneWay = true)]
      void Update(LiquesceSvcState state, string message);
   }

   [ServiceContract(
      CallbackContract = typeof(IClientStateChange),
      SessionMode = SessionMode.Required)
   ]
   public interface IClientLiquesceCallBack
   {

      [OperationContract(IsOneWay = true)]
      void Subscribe(Guid id);

      [OperationContract(IsOneWay = true)]
      void Unsubscribe(Guid id);
   }
}
