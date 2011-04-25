using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientLiquesceFTPTray
{
   [Serializable]
   public class ClientConfigDetails
   {
      // ReSharper disable UnusedAutoPropertyAccessor.Global
      // ReSharper disable MemberCanBePrivate.Global

      public List<ClientShareDetail> SharesToRestore = new List<ClientShareDetail>();
      // ReSharper restore MemberCanBePrivate.Global
      // ReSharper restore UnusedAutoPropertyAccessor.Global
   }
}
