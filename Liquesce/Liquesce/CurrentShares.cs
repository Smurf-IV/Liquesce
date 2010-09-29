using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Windows.Forms;
using LiquesceFaçade;

namespace Liquesce
{
   public partial class CurrentShares : Form
   {
      private ConfigDetails shareDetails;
      readonly List<LanManShareDetails> lmsd = new List<LanManShareDetails>();

      public CurrentShares()
      {
         InitializeComponent();
      }

      public ConfigDetails ShareDetails
      {
         get { return shareDetails; }
         set { shareDetails = value; }
      }

      private void CurrentShares_Shown(object sender, EventArgs e)
      {
         if (shareDetails == null)
            Close();
         else
         {
            StartShareLookup();
         }
      }

      private void StartShareLookup()
      {
         dataGridView1.Rows.Clear();
         lmsd.Clear();
         Enabled = false;
         UseWaitCursor = true;
         mountedPoints.Text = shareDetails.VolumeLabel + " (" + shareDetails.DriveLetter + ")";
         findSharesWorker.RunWorkerAsync();
      }

      private void Store_Click(object sender, EventArgs e)
      {
         StartShareLookup();
      }

      private void CurrentShares_FormClosing(object sender, FormClosingEventArgs e)
      {
         findSharesWorker.CancelAsync();

      }

      #region Thread Stuff
      private void findSharesWorker_DoWork(object sender, DoWorkEventArgs e)
      {
         BackgroundWorker worker = sender as BackgroundWorker;
         if (worker == null)
            return;

         // TODO: Phase 2 will have a foreach onthe drive letter
         lmsd.AddRange(LanManShareHandler.MatchDriveLanManShares(shareDetails.DriveLetter));
      }

      private void findSharesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
      {
         foreach (string[] row in
            lmsd.SelectMany(share => (share.DirSec.GetAccessRules(true, false,
                                                                  typeof (System.Security.Principal.NTAccount)).Cast
                                        <FileSystemAccessRule>().Select(fsar => new[]
                                                                                   {
                                                                                      share.Path,
                                                                                      share.Name + " : " +
                                                                                      share.Description,
                                                                                      GetAceInformation(fsar)
                                                                                   }))))
         {
            dataGridView1.Rows.Add(row);
         }
         Enabled = true;
         UseWaitCursor = false;
         progressBar1.Style = ProgressBarStyle.Continuous;
         progressBar1.Value = 0;
      }

      private string GetAceInformation(FileSystemAccessRule ace)
      {
         StringBuilder info = new StringBuilder(ace.IdentityReference.Value);
         info.Append(" : ").Append(ace.FileSystemRights.ToString());
         return info.ToString();
      }

      #endregion

   }
}
