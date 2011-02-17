using System.ComponentModel;

namespace ClientLiquesceSvc
{
   [RunInstaller(true)]
   public partial class ProjectInstaller : System.Configuration.Install.Installer
   {
      public ProjectInstaller()
      {
         InitializeComponent();
      }
   }
}
