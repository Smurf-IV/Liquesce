using System;
using System.ServiceModel;
using System.ServiceProcess;
using System.Windows.Forms;
using LiquesceFaçade;
using NLog;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace LiquesceTray
{
   public partial class NotifyIconHandler : UserControl
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private LiquesceSvcState lastState = LiquesceSvcState.Unknown;
      private readonly StateChangeHandler stateChangeHandler = new StateChangeHandler();

      public NotifyIconHandler()
      {
         InitializeComponent();
         notifyIcon1.BalloonTipTitle = "Service Status";
         timer1.Start();
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
         notifyIcon1.ShowBalloonTip(5000);
      }

      private void SetState(LiquesceSvcState state, string text)
      {
         notifyIcon1.BalloonTipText = text;
         switch (state)
         {
            case LiquesceSvcState.InWarning:
            case LiquesceSvcState.Unknown:
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Warning;
               break;
            case LiquesceSvcState.Running:
               notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
               break;
            case LiquesceSvcState.InError:
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
               break;
            default:
               throw new ArgumentOutOfRangeException("state");
         }
         if (state != lastState)
         {
            lastState = state;
            notifyIcon1.ShowBalloonTip(5000);
         }
      }

      private void timer1_Tick(object sender, System.EventArgs e)
      {
         try
         {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, timer1.Interval / 2);
            serviceController1.WaitForStatus(ServiceControllerStatus.Running, timeSpan);
            if ((notifyIcon1.Icon == Properties.Resources.StopIcon)
               || (notifyIcon1.Icon == Properties.Resources.ErrorIcon)
               )
            {
               notifyIcon1.Icon = Properties.Resources.OKIcon;
               SetState(LiquesceSvcState.Running, String.Format("Started @ {0}", DateTime.Now) );
               stateChangeHandler.CreateCallBack(SetState);
            }
         }
         catch (TimeoutException tex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InWarning != lastState)
            {
               Log.WarnException("Service is not in a running state", tex);
               SetState(LiquesceSvcState.InWarning, "Stopped");
               notifyIcon1.Icon = Properties.Resources.StopIcon;
            }
         }
         catch (Exception ex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InError != lastState)
            {
               Log.ErrorException("Service has a general exception", ex);
               notifyIcon1.Icon = Properties.Resources.ErrorIcon;
               SetState(LiquesceSvcState.InError, ex.Message);
            }
         }
      }

   }
}
