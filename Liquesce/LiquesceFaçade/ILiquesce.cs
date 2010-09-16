using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace LiquesceFaçade
{
   [Serializable]
   public enum LiquesceSvcState
   {
      Unknown,
      Running, // Used to indicate that all is well
      InError,
      InWarning,
      Stopped
   }

   public interface IStateChange
   {
      [OperationContract(IsOneWay = true)]
      void Update(LiquesceSvcState state, string message);
   }

   [ServiceContract(
      CallbackContract = typeof(IStateChange), 
      SessionMode = SessionMode.Required)
   ]
   public interface ILiquesce
   {
      [OperationContract(IsOneWay = true)]
      void Stop();

      [OperationContract(IsOneWay = true)]
      void Start();

      LiquesceSvcState State
      {
         [OperationContract]
         get;
      }

      [OperationContract(IsOneWay = true)]
      void Subscribe( Guid id );

      [OperationContract(IsOneWay = true)]
      void Unsubscribe(Guid id);

      ConfigDetails ConfigDetails
      {
         [OperationContract]
         get;
         [OperationContract(IsOneWay = true)]
         set;
      }
   }

   //---each client connected to the service has a GUID---
   [DataContract]
   public class Client
   {
      [DataMember]
      public Guid id { get; set; }
   }
}
