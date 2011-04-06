using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace LiquesceFacade
{
   public interface IStateChange
   {
      [OperationContract(IsOneWay = true)]
      void Update(LiquesceSvcState state, string message);
   }

   [ServiceContract(
      CallbackContract = typeof(IStateChange),
      SessionMode = SessionMode.Required)
   ]
   public interface ILiquesceCallBack
   {

      [OperationContract(IsOneWay = true)]
      void Subscribe(Guid id);

      [OperationContract(IsOneWay = true)]
      void Unsubscribe(Guid id);
   }

   // Each client connected to the service has a GUID
   [DataContract]
   public class Client
   {
      [DataMember]
      public Guid id { get; set; }
   }
}
