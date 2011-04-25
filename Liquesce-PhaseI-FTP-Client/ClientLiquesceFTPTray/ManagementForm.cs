using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using NLog;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray
{
   public partial class ManagementForm : Form
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private ClientPropertiesDisplay cpd;
      private static readonly string userAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"ClientLiquesceFtp");
      private readonly string configFile = Path.Combine(userAppData, @"Client.Properties.config.xml");

      private ClientConfigDetails csd;

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

      public ManagementForm()
      {
         InitializeComponent();
         FileInfo fi = new FileInfo(configFile);
         if (!fi.Exists)
         {
            DirectoryInfo di = fi.Directory;
            if (!di.Exists)
               di.Create();
            // The file will now be created when the ReadConfig is called
         }
         ReadConfigDetails();
      }

      private void ManagementForm_Load(object sender, EventArgs e)
      {
         // Stolen from Utils
         try
         {
            System.Reflection.PropertyInfo pi = propertyGrid1.GetType().GetProperty("Controls");
            Control.ControlCollection cc = (Control.ControlCollection)pi.GetValue(propertyGrid1, null);

            foreach (Control c in cc)
            {
               Type ct = c.GetType();
               string sName = ct.Name;

               if (sName == "DocComment")
               {
                  pi = ct.GetProperty("Lines");
                  if (pi != null)
                  {
#pragma warning disable 168
                     int i = (int)pi.GetValue(c, null);
#pragma warning restore 168
                     pi.SetValue(c, 6, null);
                  }

                  if (ct.BaseType != null)
                  {
                     System.Reflection.FieldInfo fi = ct.BaseType.GetField("userSized",
                                                                           System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                     if (fi != null)
                        fi.SetValue(c, true);
                  }
                  break;
               }
            }
         }
         catch( Exception ex )
         {
            Log.WarnException("ManagementForm_Load", ex);
         }
      }

      #region Button Actions
      private void btnConnect_Click(object sender, EventArgs e)
      {
         try
         {
            UseWaitCursor = true;
            Enabled = false;
            // create a new ftpclient object with the host and port number to use
            using (FtpClient ftp = new FtpClient(cpd.TargetMachineName, cpd.Port, cpd.SecurityProtocol))
            {
               ftp.Open(cpd.UserName, cpd.Password);
               // ftp.IsConnected;
               ftp.Close();
            }
            btnSave.Enabled = true;
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


      private void btnSave_Click(object sender, EventArgs e)
      {
         try
         {
            UseWaitCursor = true;
            Enabled = false;

            Log.Info("Get the details from the page into the share object");
            csd.SharesToRestore[0].TargetMachineName = cpd.TargetMachineName;
            csd.SharesToRestore[0].Port = cpd.Port;
            csd.SharesToRestore[0].SecurityProtocol = cpd.SecurityProtocol;
            csd.SharesToRestore[0].UserName = cpd.UserName;
            csd.SharesToRestore[0].TargetShareName = cpd.TargetShareName;
            string oldDriveLetter = csd.SharesToRestore[0].DriveLetter;
            csd.SharesToRestore[0].DriveLetter = cpd.DriveLetter;
            csd.SharesToRestore[0].VolumeLabel = cpd.VolumeLabel;
            csd.SharesToRestore[0].BufferWireTransferSize = cpd.BufferWireTransferSize;

            Log.Info("Write the values to the Service config file");
            WriteOutConfigDetails();
            if (DialogResult.Yes == MessageBox.Show(this, "This is about to stop then start the \"Share Enabler Service\".\nDo you want to this to happen now ?",
               "Stop then Start the Service Now..", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
               try
               {
                  Log.Info("Now toggle the service");
                  if (Handlers.ClientMappings.ContainsKey(oldDriveLetter))
                  {
                     if (Handlers.ClientMappings[oldDriveLetter].Stop())
                        Handlers.ClientMappings.Remove(oldDriveLetter);
                  }
                  HandleMappingThread newMapping = new HandleMappingThread();
                  Handlers.ClientMappings[oldDriveLetter] = newMapping;
                  newMapping.Start(csd.SharesToRestore[0]);
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

      private void btnLogView_Click(object sender, EventArgs e)
      {
         try
         {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
               InitialDirectory = Path.Combine(userAppData, @"Logs"),
               Filter = "Log files (*.log)|*.log|Archive logs (*.*)|*.*",
               FileName = "*.log",
               FilterIndex = 2,
               Title = "Select name to view contents"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
               Process word = Process.Start("Wordpad.exe", '"' + openFileDialog.FileName + '"');
               if (word != null)
               {
                  word.WaitForInputIdle();
                  SendKeys.SendWait("^{END}");
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenFile has an exception: ", ex);
            MessageBox.Show(this, ex.Message, "Failed to open the client log view", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
      #endregion

      private void ManagementForm_Shown(object sender, EventArgs e)
      {
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
               catch( Exception ex )
               {
                  Log.WarnException("ReadConfigDetails", ex);
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
