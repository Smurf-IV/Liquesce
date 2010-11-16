using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Xml.Serialization;
using DokanNet;
using LiquesceFacade;
using NLog;
using NLog.Config;

namespace ClientLiquesceSvc
{
   class ManagementLayer
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static ManagementLayer instance;
      private ClientConfigDetails currentConfigDetails;
      private readonly DateTime startTime;
      private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LiquesceSvc", Settings1.Default.ConfigFileName);
      private LiquesceSvcState state = LiquesceSvcState.Stopped;
      private static readonly Dictionary<Client, IStateChange> subscribers = new Dictionary<Client, IStateChange>();
      private static readonly ReaderWriterLockSlim subscribersLock = new ReaderWriterLockSlim();

      /// <summary>
      /// Returns "The single instance" of this singleton class.
      /// </summary>
      public static ManagementLayer Instance
      {
         get { return instance ?? (instance = new ManagementLayer()); }
      }

      /// <summary>
      /// Private constructor to prevent multiple instances
      /// </summary>
      private ManagementLayer()
      {
         try
         {
            Log.Debug("New ManagementLayer created.");
            startTime = DateTime.UtcNow;
         }
         catch (Exception ex)
         {
            Log.ErrorException("Constructor blew: ", ex);
         }
      }

      public void Subscribe(Guid guid)
      {
         try
         {
            IStateChange callback = OperationContext.Current.GetCallbackChannel<IStateChange>();
            using (subscribersLock.WriteLock())
               subscribers.Add(new Client { id = guid }, callback);
         }
         catch (Exception ex)
         {
            Log.ErrorException("Subscribe", ex);
         }
      }

      public void Unsubscribe(Guid guid)
      {
         try
         {
            IStateChange callback = OperationContext.Current.GetCallbackChannel<IStateChange>();
            using (subscribersLock.WriteLock())
            {
               var query = from c in subscribers.Keys
                           where c.id == guid
                           select c;
               subscribers.Remove(query.First());
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Unsubscribe", ex);
         }
      }

      public void Start()
      {
         ReadConfigDetails();
         if (currentConfigDetails == null)
            return;
         SetNLogLevel(currentConfigDetails.ServiceLogLevel);
         if (currentConfigDetails.SharesToRestore == null)
            return;
         foreach (ClientShareDetail clientShareDetails in currentConfigDetails.SharesToRestore)
         {
            Thread t = new Thread(new ParameterizedThreadStart(AddNewDrive));
            // Start the thread
            t.Start(clientShareDetails);
         }
      }

      private readonly Dictionary<string, List<string>> DrivetoDomainUsers = new Dictionary<string, List<string>>();
      private readonly ReaderWriterLockSlim DrivetoDomainUsersSync = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

      /// <summary>
      /// Invokes DokanNet.DokanMain function to mount a drive. 
      /// The function blocks until the file system is unmounted.
      /// Administrator privilege is needed to communicate with Dokan driver. 
      /// You need a manifest file for .NET application.
      /// </summary>
      private void AddNewDrive(object obj)
      {
         ClientShareDetail clientShareDetail = obj as ClientShareDetail;
         if (clientShareDetail == null)
         {
            Log.Fatal("Parameter passed into thread is null");
            return;
         }
         Log.Info("Find existing drive");
         List<string> existingUsers;
         bool needToCreateDrive = false;
         using (DrivetoDomainUsersSync.UpgradableReadLock())
         {
            if (!DrivetoDomainUsers.TryGetValue(clientShareDetail.DriveLetter, out existingUsers))
            {
               needToCreateDrive = true;
               existingUsers = new List<string>();
               using (DrivetoDomainUsersSync.WriteLock())
                  DrivetoDomainUsers[clientShareDetail.DriveLetter] = existingUsers;
            }
            Log.Info("Now see if the user exists");
            if (!existingUsers.Contains(clientShareDetail.DomainUserIdentity))
            {
               using (DrivetoDomainUsersSync.WriteLock())
                  existingUsers.Add(clientShareDetail.DomainUserIdentity);
            }
         }
         if (needToCreateDrive)
         {
            DokanOptions options = new DokanOptions
                                      {
                                         DriveLetter = clientShareDetail.DriveLetter[0],
                                         ThreadCount = currentConfigDetails.ThreadCount,
                                         DebugMode = currentConfigDetails.DebugMode,
                                         //      public bool UseStdErr;
                                         //    public bool UseAltStream;
                                         UseKeepAlive = true,
                                         NetworkDrive = true,
                                         VolumeLabel = clientShareDetail.VolumeLabel
                                      };

            ClientLiquesceOps dokanOperations = new ClientLiquesceOps(clientShareDetail);

            char mountedDriveLetter = clientShareDetail.DriveLetter[0];
            int retVal = Dokan.DokanMain(options, dokanOperations);
            Log.Warn("Dokan.DokanMain has exited");
            IsRunning = false;
            switch (retVal)
            {
               case Dokan.DOKAN_SUCCESS: // = 0;
                  FireStateChange(LiquesceSvcState.Stopped, "Dokan is not mounted");
                  break;
               case Dokan.DOKAN_ERROR: // = -1; // General Error
                  FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_ERROR] - General Error");
                  break;
               case Dokan.DOKAN_DRIVE_LETTER_ERROR: // = -2; // Bad Drive letter
                  FireStateChange(LiquesceSvcState.InError,
                                  "Dokan is not mounted [DOKAN_DRIVE_LETTER_ERROR] - Bad drive letter");
                  break;
               case Dokan.DOKAN_DRIVER_INSTALL_ERROR: // = -3; // Can't install driver
                  FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVER_INSTALL_ERROR]");
                  Environment.Exit(-1);
                  break;
               case Dokan.DOKAN_START_ERROR: // = -4; // Driver something wrong
                  FireStateChange(LiquesceSvcState.InError,
                                  "Dokan is not mounted [DOKAN_START_ERROR] - Driver Something is wrong");
                  Environment.Exit(-1);
                  break;
               case Dokan.DOKAN_MOUNT_ERROR: // = -5; // Can't assign drive letter
                  FireStateChange(LiquesceSvcState.InError,
                                  "Dokan is not mounted [DOKAN_MOUNT_ERROR] - Can't assign drive letter");
                  break;
               default:
                  FireStateChange(LiquesceSvcState.InError,
                                  String.Format("Dokan is not mounted [Uknown Error: {0}]", retVal));
                  Environment.Exit(-1);
                  break;
            }
         }
      }

      private void SetNLogLevel(string serviceLogLevel)
      {
         LoggingConfiguration currentConfig = LogManager.Configuration;
         //LogManager.DisableLogging();
         foreach (LoggingRule rule in currentConfig.LoggingRules)
         {
            rule.EnableLoggingForLevel(LogLevel.Fatal);
            rule.EnableLoggingForLevel(LogLevel.Error);
            rule.EnableLoggingForLevel(LogLevel.Info);
            // Turn on in order
            switch (serviceLogLevel)
            {
               case "Trace":
                  rule.EnableLoggingForLevel(LogLevel.Trace);
                  goto case "Debug"; // Drop through
               default:
               case "Debug":
                  rule.EnableLoggingForLevel(LogLevel.Debug);
                  goto case "Warn"; // Drop through
               case "Warn":
                  rule.EnableLoggingForLevel(LogLevel.Warn);
                  break;
            }
            // Turn off the rest
            switch (serviceLogLevel)
            {
               case "Warn":
                  rule.DisableLoggingForLevel(LogLevel.Debug);
                  goto default; // Drop through
               default:
                  //case "Debug":
                  rule.DisableLoggingForLevel(LogLevel.Trace);
                  break;
               case "Trace":
                  // Prevent turning off again !
                  break;
            }
         }
         //LogManager.EnableLogging();
         //LogManager.Configuration = null;
         LogManager.ReconfigExistingLoggers();
         //LogManager.Configuration = currentConfig;
         Log.Warn("Test @ [{0}]", serviceLogLevel);
         Log.Debug("Test @ [{0}]", serviceLogLevel);
         Log.Trace("Test @ [{0}]", serviceLogLevel);
      }

      private bool IsRunning { get; set; }

      public ClientConfigDetails CurrentConfigDetails
      {
         get { return currentConfigDetails; }
         set
         {
            currentConfigDetails = value;
            // I know.. Bad form calling a function in a setter !
            WriteOutConfigDetails();
         }
      }

      internal void FireStateChange(LiquesceSvcState newState, string message)
      {
         try
         {
            state = newState;
            Log.Info("Changing newState to [{0}]:[{1}]", newState, message);
            using (subscribersLock.ReadLock())
            {
               // Get all the clients in dictionary
               var query = (from c in subscribers
                            select c.Value).ToList();
               // Create the callback action
               Action<IStateChange> action = callback => callback.Update(newState, message);

               // For each connected client, invoke the callback
               query.ForEach(action);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Unable to fire state change", ex);
         }
      }

      public LiquesceSvcState State
      {
         get { return state; }
      }

      public void Stop()
      {
         using (DrivetoDomainUsersSync.ReadLock())
            foreach (string mountedDriveLetter in DrivetoDomainUsers.Keys)
            {
               int retVal = Dokan.DokanUnmount(mountedDriveLetter[0]);
               Log.Info("Dokan.DokanUnmount({0}) returned[{1}]", mountedDriveLetter, retVal);
            }
         FireStateChange(LiquesceSvcState.Unknown, "Stop has been requested");
      }


      private void ReadConfigDetails()
      {
         try
         {
            // Initialise a default to allow type get !
            currentConfigDetails = new ClientConfigDetails();
            XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
            Log.Info("Attempting to read Dokan Drive details from: [{0}]", configFile);
            using (TextReader textReader = new StreamReader(configFile))
            {
               currentConfigDetails = x.Deserialize(textReader) as ClientConfigDetails;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot read the configDetails: ", ex);
            currentConfigDetails = null;
         }
         finally
         {
            if (currentConfigDetails == null)
            {
               Log.Info("Creating new ClientConfigDetails");
               currentConfigDetails = new ClientConfigDetails();
               currentConfigDetails.SharesToRestore.Add(new ClientShareDetail());
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
         if (currentConfigDetails != null)
            try
            {
               XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
               using (TextWriter textWriter = new StreamWriter(configFile))
               {
                  x.Serialize(textWriter, currentConfigDetails);
               }
            }
            catch (Exception ex)
            {
               Log.ErrorException("Cannot save configDetails: ", ex);
            }
      }

   }
}
