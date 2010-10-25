using System.Collections.Generic;
using System.ServiceModel;

namespace LiquesceFaçade
{
   [ServiceContract]
   public interface IClientLiquesceSvc
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
   }
}
