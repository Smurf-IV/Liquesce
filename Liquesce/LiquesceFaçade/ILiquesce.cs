using System;
using System.Collections.Generic;
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

   [ServiceContract]
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

      [OperationContract]
      List<LanManShareDetails> GetPossibleShares();

      ConfigDetails ConfigDetails
      {
         [OperationContract]
         get;
         [OperationContract(IsOneWay = true)]
         set;
      }
   }
}
