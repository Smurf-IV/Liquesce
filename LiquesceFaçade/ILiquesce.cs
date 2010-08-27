using System;
using System.ServiceModel;

namespace LiquesceFaçade
{
   [Serializable]
   public enum LiquesceSvcState
   {
      Unknown,
      Running, // Used to indicate that all is well
      InError,
      InWarning
   }

   [ServiceContract]
   public interface ILiquesce
   {
      [OperationContract]
      void Stop();

      [OperationContract]
      void Start();

      LiquesceSvcState State
      {
         [OperationContract]
         get;
      }

      
      ConfigDetails ConfigDetails
      {
         [OperationContract]
         get;
         [OperationContract]
         set;
      }
   }
}
