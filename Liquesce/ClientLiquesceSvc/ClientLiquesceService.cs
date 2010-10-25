using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ClientLiquesceSvc
{
   public partial class ClientLiquesceService : ServiceBase
   {
      public ClientLiquesceService()
      {
         InitializeComponent();
      }

      protected override void OnStart(string[] args)
      {
      }

      protected override void OnStop()
      {
      }
   }
}
