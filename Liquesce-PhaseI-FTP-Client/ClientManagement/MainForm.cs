using System;
using System.IO;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Xml.Serialization;
using NLog;

namespace ClientManagement
{
   public partial class MainForm : Form
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private ClientPropertiesDisplay cpd;
      private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"LiquesceSvc\Client.Properties.config.xml");

      ClientConfigDetails csd;

      private ClientConfigDetails ClientConfigDetails
      {
         get { return csd; }
         set
         {
            csd = value;
            if (csd.SharesToRestore.Count == 0)
               csd.SharesToRestore.Add(new ClientShareDetail());

            cpd = new ClientPropertiesDisplay(csd.SharesToRestore[0]);
            propertyGrid1.SelectedObject = cpd;
            propertyGrid1.Refresh();
         }
      }

      public MainForm()
      {
         InitializeComponent();
      }

      private void MainForm_Load(object sender, EventArgs e)
      {
         Utils.ResizeDescriptionArea(ref propertyGrid1, 6); // okay for most
      }

      #region Button Actions
      private void btnConnect_Click(object sender, EventArgs e)
      {
         try
         {
            UseWaitCursor = true;
            Enabled = false;
            Ping pn = new Ping();
            PingReply pr = pn.Send(cpd.TargetMachineName);
            // ReSharper disable PossibleNullReferenceException
            // I Want this to throw if it is null !!
            if (pr.Status != IPStatus.Success)
            {
               throw new PingException(pr.Status.ToString());
            }
            // ReSharper restore PossibleNullReferenceException
         }
         catch (Exception ex)
         {
            Log.ErrorException("btnConnect_Click", ex);
            MessageBox.Show(this, ex.Message, "Failed to contact Target", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            UseWaitCursor = false;
            Enabled = true;
         }
      }

      private void btnRefresh_Click(object sender, EventArgs e)
      {
         try
         {
            UseWaitCursor = true;
            Enabled = false;
         }
         finally
         {
            UseWaitCursor = false;
            Enabled = true;
         }
      }

      private void btnViewLog_Click(object sender, EventArgs e)
      {
         new LogDisplay(@"Liquesce\Logs").ShowDialog(this);
      }

      private void btnSend_Click(object sender, EventArgs e)
      {
         try
         {
            UseWaitCursor = true;
            Enabled = false;

            Log.Info("Get the details from the page into the share object");
            csd.SharesToRestore[0].TargetMachineName = cpd.TargetMachineName;
            csd.SharesToRestore[0].DomainUserIdentity = cpd.DomainUserIdentity;
            csd.SharesToRestore[0].TargetShareName = cpd.TargetShareName;
            csd.SharesToRestore[0].DriveLetter = cpd.DriveLetter;
            csd.SharesToRestore[0].VolumeLabel = cpd.VolumeLabel;
            csd.SharesToRestore[0].BufferWireTransferSize = cpd.BufferWireTransferSize;
            csd.SharesToRestore[0].ShowAsNetworkDrive = cpd.ShowAsNetworkDrive;

            Log.Info("Write the values to the Service config file");
            WriteOutConfigDetails();
            if (System.Windows.Forms.DialogResult.Yes == MessageBox.Show(this, "This is about to stop then start the \"Share Enabler Service\".\nDo you want to this to happen now ?",
               "Stop then Start the Service Now..", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
               try
               {
                  Log.Info("Now toggle the service");
                  serviceController1.Stop();
                  serviceController1.WaitForStatus(ServiceControllerStatus.Stopped);
                  serviceController1.Start();
               }
               catch (Exception ex)
               {
                  Log.ErrorException("btnSend_Click", ex);
                  MessageBox.Show(this, ex.Message, "Failed, Check the logs", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
            }
         }
         finally
         {
            UseWaitCursor = false;
            Enabled = true;
         }
      }
      #endregion

      private void MainForm_Shown(object sender, EventArgs e)
      {
         ServiceControllerStatus serviceStatus = ServiceControllerStatus.Stopped;
         try
         {
            serviceStatus = serviceController1.Status;
         }
         catch (Exception ex)
         {
            Log.ErrorException("Service is probably not installed", ex);
         }
         if (serviceStatus != ServiceControllerStatus.Running)
         {
           btnRefresh.Text = "Enabler stopped";
         }
         try
         {
            ReadConfigDetails();

         }
         catch (Exception ex)
         {
            Log.ErrorException("Unable to ReadConfigDetails", ex);
         }
      }

      private void ReadConfigDetails()
      {
         try
         {
            // Initialise a default to allow type get !
            csd = new ClientConfigDetails();
            XmlSerializer x = new XmlSerializer(csd.GetType());
            Log.Info("Attempting to read ClientConfigDetails from: [{0}]", configFile);
            using (TextReader textReader = new StreamReader(configFile))
            {
               ClientConfigDetails = x.Deserialize(textReader) as ClientConfigDetails;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot read the configDetails: ", ex);
            csd = null;
         }
         finally
         {
            if (csd == null)
            {
               Log.Info("Creating new ClientConfigDetails");
               ClientConfigDetails = new ClientConfigDetails();
               try
               {
                  if (File.Exists(configFile))
                     File.Move(configFile, configFile + Guid.NewGuid());
               }
               catch
               {
               }
               WriteOutConfigDetails();
            }
         }

      }


      private void WriteOutConfigDetails()
      {
         if (csd != null)
            try
            {
               XmlSerializer x = new XmlSerializer(csd.GetType());
               using (TextWriter textWriter = new StreamWriter(configFile))
               {
                  x.Serialize(textWriter, csd);
               }
            }
            catch (Exception ex)
            {
               Log.ErrorException("Cannot save configDetails: ", ex);
            }
      }

   }
}
