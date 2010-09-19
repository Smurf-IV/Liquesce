using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Forms;
using LiquesceFaçade;
using NLog;

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
         // Use last state to prevent balloon tip showing on start !
         SetState(lastState, "Application tray is starting");
         DoStatusCheck(0);
         timer1.Start();
      }

      private void exitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         notifyIcon1.Visible = false;
         Application.Exit();
      }

      private void managementApp_Click(object sender, EventArgs e)
      {
         //Application.StartupPath;
         Process process = new Process { StartInfo = { 
                                            WorkingDirectory = Application.StartupPath,
                                            FileName = "Liquesce.exe"
         }
         };

         process.Start();
      }

      private void repeatLastMessage_Click(object sender, EventArgs e)
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
            case LiquesceSvcState.Stopped:
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
               break;
            case LiquesceSvcState.InError:
               notifyIcon1.BalloonTipIcon = ToolTipIcon.Error;
               break;
            default:
               Log.Error("SetState has an unknown state value [{0}]", state);
               notifyIcon1.BalloonTipIcon = ToolTipIcon.None;
               break;
         }
         if (state != lastState)
         {
            lastState = state;
            notifyIcon1.ShowBalloonTip(5000);
         }
      }
      private void timer1_Tick(object sender, EventArgs e)
      {
         DoStatusCheck(timer1.Interval / 2);
      }

      private void DoStatusCheck(int milliseconds)
      {
         try
         {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, milliseconds);
            serviceController1.WaitForStatus(ServiceControllerStatus.Running, timeSpan);
            if (LiquesceSvcState.Running != lastState)
            {
               notifyIcon1.Icon = Properties.Resources.OKIcon;
               SetState(LiquesceSvcState.Running, String.Format("Started @ {0}", DateTime.Now) );
               stateChangeHandler.CreateCallBack(SetState);
            }
         }
         catch (System.ServiceProcess.TimeoutException tex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InWarning != lastState)
            {
               Log.WarnException("Service is not in a running state", tex);
               SetState(LiquesceSvcState.InWarning, "Liquesce service is Stopped");
               notifyIcon1.Icon = Properties.Resources.StopIcon;
            }
         }
         catch (Exception ex)
         {
            stateChangeHandler.RemoveCallback();
            // Be nice to the log
            if (LiquesceSvcState.InError != lastState)
            {
               Log.ErrorException("Liquesce service has a general exception", ex);
               notifyIcon1.Icon = Properties.Resources.ErrorIcon;
               SetState(LiquesceSvcState.InError, ex.Message);
            }
         }
      }

   }
}
