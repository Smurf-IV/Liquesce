using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace LiquesceFTPFacade
{
   [Serializable]
   public enum LiquesceFTPSvcState
   {
      Unknown,
      Running, // Used to indicate that all is well
      InError,
      InWarning,
      Stopped
   }

   [ServiceContract]
   public interface ILiquesceFTP
   {
      [OperationContract(IsOneWay = true)]
      void Stop();

      [OperationContract(IsOneWay = true)]
      void Start();

      LiquesceFTPSvcState State
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
