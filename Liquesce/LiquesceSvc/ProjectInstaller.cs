using System.ComponentModel;
using System.Configuration.Install;


namespace LiquesceSvc
{
   [RunInstaller(true)]
   public partial class ProjectInstaller : Installer
   {
      public ProjectInstaller()
      {
         InitializeComponent();
      }
   }
}
