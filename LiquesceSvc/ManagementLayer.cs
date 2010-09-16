using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Xml.Serialization;
using DokanNet;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   class ManagementLayer
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static ManagementLayer instance;
      private ConfigDetails currentConfigDetails;
      private readonly DateTime startTime;
      private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LiquesceSvc", Settings1.Default.ConfigFileName);
      private char mountedDriveLetter;
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
            try
            {
               subscribersLock.EnterWriteLock();
               subscribers.Add(new Client { id = guid }, callback);
            }
            finally
            {
               subscribersLock.ExitWriteLock();
            } 
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
            try
            {
               subscribersLock.EnterWriteLock();
               var query = from c in subscribers.Keys
                           where c.id == guid
                           select c;
               subscribers.Remove(query.First());
            }
            finally
            {
               subscribersLock.ExitWriteLock();
            } 
         }
         catch(Exception ex)
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
      /// <returns></returns>
      public void Start(object obj)
      {
         try
         {
            if (!IsRunning)
            {
               if (currentConfigDetails == null)
                  ReadConfigDetails();
               TimeSpan delayStart = DateTime.UtcNow - startTime;
               FireStateChange(LiquesceSvcState.InError, "Starting up");
               if (currentConfigDetails == null)
               {
                  Log.Fatal("Unable to read the config details to allow this service to run. Will now exit");
                  Environment.Exit(-1);
                  // ReSharper disable HeuristicUnreachableCode
                  return;
                  // ReSharper restore HeuristicUnreachableCode
               }
               FireStateChange(LiquesceSvcState.Running, "Dokan initialised");
               IsRunning = true;

               // Sometimes the math gets all confused due to the casting !!
               int delayStartMilliseconds = (int)(currentConfigDetails.DelayStartMilliSec - delayStart.Milliseconds);
               if ((delayStartMilliseconds > 0)
                  && (delayStartMilliseconds < UInt16.MaxValue)
                  )
               {
                  Log.Info("Delay Start needs to be obeyed");
                  Thread.Sleep(delayStartMilliseconds);
               }
               if (State != LiquesceSvcState.Running)
                  return;  // We have been asked to exit via the stop
               DokanOptions options = new DokanOptions
                                         {
                                            DriveLetter = currentConfigDetails.DriveLetter[0],
                                            ThreadCount = currentConfigDetails.ThreadCount,
                                            DebugMode = currentConfigDetails.DebugMode,
                                            //      public bool UseStdErr;
                                            //    public bool UseAltStream;
                                            UseKeepAlive = true,  // When you set TRUE on DokanOptions->UseKeepAlive, dokan library automatically unmounts 15 seconds after user-mode file system hanged up
                                            NetworkDrive = false,
                                            VolumeLabel = currentConfigDetails.VolumeLabel
                                         };

               LiquesceOps dokanOperations = new LiquesceOps(currentConfigDetails);
               ThreadPool.QueueUserWorkItem(dokanOperations.InitialiseShares, dokanOperations);

               mountedDriveLetter = currentConfigDetails.DriveLetter[0];
               int retVal = Dokan.DokanMain(options, dokanOperations);
               IsRunning = false;
               switch (retVal)
               {
                  case Dokan.DOKAN_SUCCESS: // = 0;
                     FireStateChange(LiquesceSvcState.Stopped, "Dokan is not mounted");
                     break;
                  case Dokan.DOKAN_ERROR:// = -1; // General Error
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_ERROR]");
                     break;
                  case Dokan.DOKAN_DRIVE_LETTER_ERROR: // = -2; // Bad Drive letter
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVE_LETTER_ERROR]");
                     break;
                  case Dokan.DOKAN_DRIVER_INSTALL_ERROR: // = -3; // Can't install driver
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVER_INSTALL_ERROR]");
                     break;
                  case Dokan.DOKAN_START_ERROR: // = -4; // Driver something wrong
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_START_ERROR]");
                     break;
                  case Dokan.DOKAN_MOUNT_ERROR: // = -5; // Can't assign drive letter
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_MOUNT_ERROR]");
                     break;
                  default:
                     FireStateChange(LiquesceSvcState.InError, String.Format("Dokan is not mounted [Uknown Error: {0}]", retVal));
                     break;
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Start has failed in an uncontrolled way: ", ex);
         }
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

      internal void FireStateChange(LiquesceSvcState newState, string message)
      {
         try
         {
            state = newState;
            Log.Info("Changing newState to [{0}]:[{1}]", newState, message);
            try
            {
               subscribersLock.EnterReadLock();
               // Get all the clients in dictionary
               var query = (from c in subscribers
                            select c.Value).ToList();
               // Create the callback action
               Action<IStateChange> action = callback => callback.Update(newState, message);

               // For each connected client, invoke the callback
               query.ForEach(action);
            }
            finally
            {
               subscribersLock.ExitReadLock();
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
         if (IsRunning
            && (State != LiquesceSvcState.Unknown)
            )
         {
            FireStateChange(LiquesceSvcState.Unknown, "Stop has been requested");
            int retVal = Dokan.DokanUnmount(mountedDriveLetter);
            IsRunning = false;
            Log.Info("Stop returned[{0}]", retVal);
         }
      }


      private void ReadConfigDetails()
      {
         try
         {
            InitialiseToDefault();
            XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
            Log.Info("Attempting to read Dokan Drive details from: [{0}]", configFile);
            using (TextReader textReader = new StreamReader(configFile))
            {
               currentConfigDetails = x.Deserialize(textReader) as ConfigDetails;
            }
            Log.Info("Now normalise the paths to allow the file finders to work correctly");
            List<string> fileSourceLocations = new List<string>(currentConfigDetails.SourceLocations);
            currentConfigDetails.SourceLocations.Clear();
            fileSourceLocations.ForEach(
               location => currentConfigDetails.SourceLocations.Add(Path.GetPathRoot(location).TrimEnd(Path.DirectorySeparatorChar)));
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot read the configDetails: ", ex);
            currentConfigDetails = null;
         }
         finally
         {
            if (currentConfigDetails == null)
               InitialiseToDefault();
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
                                            DebugMode = true,
                                            DelayStartMilliSec = (uint)short.MaxValue,
                                            DriveLetter = "N",
                                            SourceLocations = new List<string>(1),
                                            ThreadCount = 1,
                                            //HoldOffBufferBytes = 1*1024*1024*1024, // ==1GB
                                            VolumeLabel = "InternallyCreated"
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
