using System;
using System.Threading;
using DokanNet;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   class ManagementLayer
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static ManagementLayer instance;
      private readonly object aLock = new object();
      private TimeSpan isAlivePeriodms;
      private readonly object runLock = new object();
      private ConfigDetails configDetails;

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

      public bool Start()
      {
         bool success;
         try
         {
            lock (aLock)
            {
               if (!IsRunning)
               {
                  DokanOptions options = new DokanOptions();
                  IDokanOperations dokanOperations = new LiquesceOps();
                  State = LiquesceSvcState.InError;
                  int retVal = DokanNet.DokanNet.DokanMain(options, dokanOperations);
                  switch (retVal)
                  {
                     case DokanNet.DokanNet.DOKAN_SUCCESS: // = 0;
                        Log.Info("DOKAN_SUCCESS");
                        State = LiquesceSvcState.Running;
                        IsRunning = true;
                        break;
                     case DokanNet.DokanNet.DOKAN_ERROR:// = -1; // General Error
                        Log.Info("DOKAN_ERROR");
                        break;
                     case DokanNet.DokanNet.DOKAN_DRIVE_LETTER_ERROR: // = -2; // Bad Drive letter
                        Log.Info("DOKAN_DRIVE_LETTER_ERROR");
                        break;
                     case DokanNet.DokanNet.DOKAN_DRIVER_INSTALL_ERROR: // = -3; // Can't install driver
                        Log.Info("DOKAN_DRIVER_INSTALL_ERROR");
                        break;
                     case DokanNet.DokanNet.DOKAN_START_ERROR: // = -4; // Driver something wrong
                        Log.Info("DOKAN_START_ERROR");
                        break;
                     case DokanNet.DokanNet.DOKAN_MOUNT_ERROR: // = -5; // Can't assign drive letter
                        Log.Info("DOKAN_MOUNT_ERROR");
                        break;
                     default:
                        Log.Info("Unknown Error retirn {0}", retVal);
                        break;
                  }
               }
            }
            success = IsRunning;
         }
         catch (Exception ex)
         {
            Log.ErrorException("Start has failed in an uncontrolled way: ", ex);
            success = false;
         }
         return success;
      }

      private bool IsRunning { get; set; }

      public ConfigDetails ConfigDetails
      {
         get { return configDetails; }
         set { configDetails = value; }
      }

      public LiquesceSvcState State { get; private set; }

      public void Stop()
      {
         lock (aLock)
         {
            if (IsRunning
               && (State != LiquesceSvcState.Unknown)
               )
            {
               State = LiquesceSvcState.Unknown;
               int retVal = DokanNet.DokanNet.DokanUnmount(configDetails.DriveLetter);
               Log.Info("Stop returned[{0}]", retVal);
            }
         }
      }


      public void ReadConfigDetails()
      {
         throw new NotImplementedException();
         // configDetails = ??
      }

   }
}
