using System;
using LiquesceFaçade;

namespace LiquesceSvc
{
   public class LiquesceFaçade : ILiquesce
   {
      public void Stop()
      {
         ManagementLayer.Instance.Stop();
      }

      public bool Start()
      {
         return ManagementLayer.Instance.Start();
      }

      public LiquesceSvcState State
      {
         get { return ManagementLayer.Instance.State; }
      }

      public ConfigDetails ConfigDetails
      {
         get { return ManagementLayer.Instance.ConfigDetails; }
         set { ManagementLayer.Instance.ConfigDetails = value; }
      }

   }
}
