using System.Windows.Forms;
using NLog;

namespace LiquesceTray
{
   public partial class NotifyIconHandler : UserControl
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      public NotifyIconHandler()
      {
         InitializeComponent();
      }

      private void exitToolStripMenuItem_Click(object sender, System.EventArgs e)
      {
         notifyIcon1.Visible = false;
         Application.Exit();
      }

      private void managementApp_Click(object sender, System.EventArgs e)
      {
         //Application.StartupPath;
      }

      private void repeatLastMessage_Click(object sender, System.EventArgs e)
      {

      }
   }
}
