#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ManagementLayer.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion
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

      // ReSharper disable MemberCanBeMadeStatic.Local
      // This will need to be changed to be a map of drive to ops
      public LiquesceOps dokanOperations
      // ReSharper restore MemberCanBeMadeStatic.Local
      {
         get;
         private set;
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
               string mountPoint = currentConfigDetails.DriveLetter;
               mountedDriveLetter = currentConfigDetails.DriveLetter[0];
               try
               {
                  Log.Info("DokanVersion:[{0}], DokanDriverVersion[{1}]", Dokan.DokanVersion(), Dokan.DokanDriverVersion());
                  Dokan.DokanUnmount(mountedDriveLetter);
                  ShellChangeNotify.Unmount(mountedDriveLetter);
               }
               catch (Exception ex)
               {
                  Log.InfoException("Make sure it's unmounted threw:", ex);
               }

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

               // TODO: Search all usages of the DriveLetter and make sure they become MountPoint compatible
               //if (mountPoint.Length == 1)
               //   mountPoint += ":\\"; // Make this into a MountPoint for V 0.6.0
               DokanOptions options = new DokanOptions
               {
                  MountPoint = mountPoint,
                  ThreadCount = currentConfigDetails.ThreadCount,
                  DebugMode = currentConfigDetails.DebugMode,
                  //      public bool UseStdErr;
                  // UseAltStream = true, // This needs all sorts of extra API's
                  UseKeepAlive = true,  // When you set TRUE on DokanOptions->UseKeepAlive, dokan library automatically unmounts 15 seconds after user-mode file system hanged up
                  NetworkDrive = false,  // Set this to true to see if it stops the recycler bin question until [workitem:7253] is sorted
                                         // If the network is true then also need to have the correct version of the dokannp.dll that works on the installed OS
                  VolumeLabel = currentConfigDetails.VolumeLabel
               };


               dokanOperations = new LiquesceOps(currentConfigDetails);
               ThreadPool.QueueUserWorkItem(dokanOperations.InitialiseShares, dokanOperations);

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
         if (IsRunning)
         {
            FireStateChange(LiquesceSvcState.Unknown, "Stop has been requested");
            int retVal = Dokan.DokanUnmount(mountedDriveLetter);
            ShellChangeNotify.Unmount(mountedDriveLetter);
            Log.Info("Stop returned[{0}]", retVal);
         }
      }

      public List<LanManShareDetails> GetPossibleShares()
      {
         // TODO: Phase 2 will have a foreach onthe drive letter
         return (LanManShareHandler.MatchDriveLanManShares(currentConfigDetails.DriveLetter));
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
