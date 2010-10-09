using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using LiquesceFaçade;

namespace Liquesce
{
   public partial class CurrentShares : Form
   {
      private ConfigDetails shareDetails;
      List<LanManShareDetails> lmsd;

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
            FindShares();
         }
      }

      private void Store_Click(object sender, EventArgs e)
      {
         FindShares();
      }

      private void FindShares()
      {
         dataGridView1.Rows.Clear();
         Enabled = false;
         UseWaitCursor = true;
         mountedPoints.Text = shareDetails.VolumeLabel + " (" + shareDetails.DriveLetter + ")";
         ChannelFactory<ILiquesce> factory = new ChannelFactory<ILiquesce>("LiquesceFaçade");
         ILiquesce remoteIF = factory.CreateChannel();
         lmsd = remoteIF.GetPossibleShares();

         foreach (string[] row in lmsd.SelectMany(share =>
                                                  share.UserAccessRules.Select(fsare => new string[]
                                                                                         {
                                                                                            share.Path, 
                                                                                            share.Name + " : " + share.Description, 
                                                                                            GetAceInformation(fsare)
                                                                                         })))
         {
            dataGridView1.Rows.Add(row);
         }
         Enabled = true;
         UseWaitCursor = false;
         progressBar1.Style = ProgressBarStyle.Continuous;
         progressBar1.Value = 0;
      }

      #region Thread Stuff
      private void findSharesWorker_DoWork(object sender, DoWorkEventArgs e)
      {
         BackgroundWorker worker = sender as BackgroundWorker;
         if (worker == null)
            return;

      }


      private string GetAceInformation(UserAccessRuleExport fsare)
      {
         StringBuilder info = new StringBuilder(fsare.DomainUserIdentity);
         info.Append(" : ").Append(fsare.AccessMask.ToString());
         return info.ToString();
      }

      #endregion

      private void buttonSave_Click(object sender, EventArgs e)
      {
         shareDetails.SharesToRestore = lmsd;
      }

   }
}
