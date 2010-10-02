﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Xml.Serialization;
using DokanNet;
using LiquesceFaçade;
using NLog;
using NLog.Config;

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
            TimeSpan delayStart = DateTime.UtcNow - startTime;
            int repeatWait = 0;
            while (IsRunning
               && (repeatWait++ < 100)
               )
            {
               Log.Warn("Last Dokan is still running");
               Thread.Sleep(250);
            }
            if (!IsRunning)
            {
               if (currentConfigDetails == null)
                  ReadConfigDetails();
               FireStateChange(LiquesceSvcState.InError, "Starting up");
               if (currentConfigDetails == null)
               {
                  Log.Fatal("Unable to read the config details to allow this service to run. Will now exit");
                  Environment.Exit(-1);
                  // ReSharper disable HeuristicUnreachableCode
                  return;
                  // ReSharper restore HeuristicUnreachableCode
               }
               SetNLogLevel(currentConfigDetails.ServiceLogLevel);

               FireStateChange(LiquesceSvcState.Unknown, "Dokan initialised");
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

               DokanOptions options = new DokanOptions
                                         {
                                            DriveLetter = currentConfigDetails.DriveLetter[0],
                                            ThreadCount = currentConfigDetails.ThreadCount,
                                            DebugMode = currentConfigDetails.DebugMode,
                                            //      public bool UseStdErr;
                                            //    public bool UseAltStream;
                                            UseKeepAlive = true,  // When you set TRUE on DokanOptions->UseKeepAlive, dokan library automatically unmounts 15 seconds after user-mode file system hanged up
                                            NetworkDrive = false,  // Set this to true to see if it stops the recycler bin question until [workitem:7253] is sorted
                                            VolumeLabel = currentConfigDetails.VolumeLabel
                                         };

               LiquesceOps dokanOperations = new LiquesceOps(currentConfigDetails);
               ThreadPool.QueueUserWorkItem(dokanOperations.InitialiseShares, dokanOperations);

               mountedDriveLetter = currentConfigDetails.DriveLetter[0];


               try
               {
                  Log.Info("DokanVersion:[{0}], DokanDriverVersion[{1}]", Dokan.DokanVersion(), Dokan.DokanDriverVersion());
                  Dokan.DokanUnmount(mountedDriveLetter);
               }
               catch (Exception ex)
               {
                  Log.InfoException("Make sure it's unmounted threw:", ex);
               }
               int retVal = Dokan.DokanMain(options, dokanOperations);
               Log.Warn("Dokan.DokanMain has exited");
               IsRunning = false;
               switch (retVal)
               {
                  case Dokan.DOKAN_SUCCESS: // = 0;
                     FireStateChange(LiquesceSvcState.Stopped, "Dokan is not mounted");
                     break;
                  case Dokan.DOKAN_ERROR:// = -1; // General Error
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_ERROR] - General Error");
                     break;
                  case Dokan.DOKAN_DRIVE_LETTER_ERROR: // = -2; // Bad Drive letter
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVE_LETTER_ERROR] - Bad drive letter");
                     break;
                  case Dokan.DOKAN_DRIVER_INSTALL_ERROR: // = -3; // Can't install driver
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_DRIVER_INSTALL_ERROR]");
                     Environment.Exit(-1);
                     break;
                  case Dokan.DOKAN_START_ERROR: // = -4; // Driver something wrong
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_START_ERROR] - Driver Something is wrong");
                     Environment.Exit(-1);
                     break;
                  case Dokan.DOKAN_MOUNT_ERROR: // = -5; // Can't assign drive letter
                     FireStateChange(LiquesceSvcState.InError, "Dokan is not mounted [DOKAN_MOUNT_ERROR] - Can't assign drive letter");
                     break;
                  default:
                     FireStateChange(LiquesceSvcState.InError, String.Format("Dokan is not mounted [Uknown Error: {0}]", retVal));
                     Environment.Exit(-1);
                     break;
               }
            }
            else
            {
               FireStateChange(LiquesceSvcState.InError, "Seems like the last exit request into Dokan did not exit in time");
            }
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
         if (IsRunning)
         {
            FireStateChange(LiquesceSvcState.Unknown, "Stop has been requested");
            int retVal = Dokan.DokanUnmount(mountedDriveLetter);
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
               location => currentConfigDetails.SourceLocations.Add(Path.GetFullPath(location).TrimEnd(Path.DirectorySeparatorChar))); 
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