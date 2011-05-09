using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Xml.Serialization;
using LiquesceFTPFacade;
using LiquesceFTPSvc.FTP;
using NLog;
using NLog.Config;

namespace LiquesceFTPSvc
{
   class ManagementLayer
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static ManagementLayer instance;
      private ConfigDetails currentConfigDetails;
      private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LiquesceFTPSvc", Settings1.Default.ConfigFileName);
      private LiquesceFTPSvcState state = LiquesceFTPSvcState.Stopped;
      private static readonly Dictionary<Client, IStateChange> subscribers = new Dictionary<Client, IStateChange>();
      private static readonly ReaderWriterLockSlim subscribersLock = new ReaderWriterLockSlim();
      internal static FTPServer FtpServer;

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
         }
         catch (Exception ex)
         {
            Log.ErrorException("Constructor blew: ", ex);
         }
      }

      public static void Subscribe(Guid guid)
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

      public static void Unsubscribe(Guid guid)
      {
         try
         {
            IStateChange callback = OperationContext.Current.GetCallbackChannel<IStateChange>();
            using (subscribersLock.WriteLock())
            {
               IEnumerable<Client> query = from c in subscribers.Keys
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

      /// <summary>
      /// Invokes DokanNet.DokanMain function to mount a drive. 
      /// The function blocks until the file system is unmounted.
      /// Administrator privilege is needed to communicate with Dokan driver. 
      /// You need a manifest file for .NET application.
      /// </summary>
      public void Start(object obj)
      {
         try
         {
               if (currentConfigDetails == null)
                  ReadConfigDetails();
               FireStateChange(LiquesceFTPSvcState.InError, "Starting up");
               if (currentConfigDetails == null)
               {
                  Log.Fatal("Unable to read the config details to allow this service to run. Will now exit");
                  Environment.Exit(-1);
                  // ReSharper disable HeuristicUnreachableCode
                  return;
                  // ReSharper restore HeuristicUnreachableCode
               }
               SetNLogLevel(currentConfigDetails.ServiceLogLevel);

               FireStateChange(LiquesceFTPSvcState.Unknown, "LiquesceFTPSvc initialised");
               FtpServer = new FTPServer(currentConfigDetails);
               IsRunning = FtpServer.Start(); 

               //switch (retVal)
               //{
               //   case Dokan.DOKAN_SUCCESS: // = 0;
               //      FireStateChange(LiquesceSvcState.Stopped, "Dokan is not mounted");
               //      break;
               //   case Dokan.DOKAN_ERROR:// = -1; // General Error
               //      FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_ERROR] - General Error");
               //      break;
               //   case Dokan.DOKAN_DRIVE_LETTER_ERROR: // = -2; // Bad Drive letter
               //      FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVE_LETTER_ERROR] - Bad drive letter");
               //      break;
               //   case Dokan.DOKAN_DRIVER_INSTALL_ERROR: // = -3; // Can't install driver
               //      FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVER_INSTALL_ERROR]");
               //      Environment.Exit(-1);
               //      break;
               //   case Dokan.DOKAN_START_ERROR: // = -4; // Driver something wrong
               //      FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_START_ERROR] - Driver Something is wrong");
               //      Environment.Exit(-1);
               //      break;
               //   case Dokan.DOKAN_MOUNT_ERROR: // = -5; // Can't assign drive letter
               //      FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_MOUNT_ERROR] - Can't assign drive letter");
               //      break;
               //   default:
               //      FireStateChange(LiquesceSvcState.InError, String.Format("Dokan is not mounted [Uknown Error: {0}]", retVal));
               //      Environment.Exit(-1);
               //      break;
               //}
         }
         catch (Exception ex)
         {
            Log.ErrorException("Start has failed in an uncontrolled way: ", ex);
            Environment.Exit(-1);
         }
         finally
         {
            IsRunning = false;
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

      public ConfigDetails CurrentConfigDetails
      {
         get { return currentConfigDetails; }
         set
         {
            currentConfigDetails = value;
            // I know.. Bad form calling a function in a setter !
            WriteOutConfigDetails();
         }
      }

      internal void FireStateChange(LiquesceFTPSvcState newState, string message)
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

      public LiquesceFTPSvcState State
      {
         get { return state; }
      }

      public void Stop()
      {
         if (IsRunning)
         {
            FireStateChange(LiquesceFTPSvcState.Unknown, "Stop has been requested");
            if ((FtpServer != null)
               && IsRunning)
            {
               FtpServer.Stop();
            }
            FireStateChange(LiquesceFTPSvcState.Stopped, "Dokan is not mounted");
         }
      }

      public List<LanManShareDetails> GetPossibleShares()
      {
         // TODO: Phase 2 will have a foreach onthe drive letter
         throw new NotImplementedException();
         // return (LanManShareHandler.MatchDriveLanManShares(currentConfigDetails.DriveLetter));
      }


      private void ReadConfigDetails()
      {
         try
         {
            InitialiseToDefault();
            XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
            Log.Info("Attempting to read Merge Drive details from: [{0}]", configFile);
            using (TextReader textReader = new StreamReader(configFile))
            {
               currentConfigDetails = x.Deserialize(textReader) as ConfigDetails;
            }
            Log.Info("Now normalise the paths to allow the file finders to work correctly");
            if (currentConfigDetails != null)
            {
               List<string> fileSourceLocations = new List<string>(currentConfigDetails.SourceLocations);
               currentConfigDetails.SourceLocations.Clear();

               fileSourceLocations.ForEach(
                  location => currentConfigDetails.SourceLocations.Add(Path.GetFullPath(location).TrimEnd(Path.DirectorySeparatorChar)));
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
               InitialiseToDefault();
               if (!File.Exists(configFile))
                  WriteOutConfigDetails();
            }
         }

      }

      private void InitialiseToDefault()
      {
         try
         {
            if (currentConfigDetails == null)
            {
               currentConfigDetails = new ConfigDetails
                                         {
                                            SourceLocations = new List<string>(1),
                                            //HoldOffBufferBytes = 1*1024*1024*1024, // ==1GB
                                         };
               currentConfigDetails.SourceLocations.Add(@"C:\");
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot create the default configDetails: ", ex);
            currentConfigDetails = null;
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
