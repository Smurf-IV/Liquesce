using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace LiquesceFacade
{
   public class LiquesceProxy : ClientBase<ILiquesce>, ILiquesce
    {
        public LiquesceProxy()
        {
        }

        public LiquesceProxy(string endpointConfigurationName)
            : base(endpointConfigurationName)
        {
        }

        public LiquesceProxy(string endpointConfigurationName, string remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public LiquesceProxy(string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public LiquesceProxy(System.ServiceModel.Channels.Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

      #region Implementation of ILiquesce

      public void Stop()
      {
         base.Channel.Stop();
      }

      public void Start()
      {
         base.Channel.Start();
      }

      public LiquesceSvcState LiquesceState
      {
         get { return base.Channel.LiquesceState; }
      }

      public List<LanManShareDetails> GetPossibleShares()
      {
         return base.Channel.GetPossibleShares();
      }

      public ConfigDetails ConfigDetails
      {
         get { return base.Channel.ConfigDetails; }
         set { base.Channel.ConfigDetails = value; }
      }

      #endregion
    }
}
