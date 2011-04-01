using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;


namespace LiquesceFTPSvc
{
   [RunInstaller(true)]
   public partial class Installer1 : System.Configuration.Install.Installer
   {
      public Installer1()
      {
         InitializeComponent();
      }
   }
}
