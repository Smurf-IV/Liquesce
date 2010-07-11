using System.ServiceProcess;

namespace Liquesce
{
   public partial class Main : ServiceBase
   {
      public Main()
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
