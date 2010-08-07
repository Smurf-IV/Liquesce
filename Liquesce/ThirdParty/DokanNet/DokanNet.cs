using NLog;

namespace DokanNet
{
   public class DokanOptions
   {
      public readonly char DriveLetter;
      public readonly ushort ThreadCount;
      public readonly bool DebugMode;
      public readonly bool UseStdErr;
      public readonly bool UseAltStream;
      public readonly bool UseKeepAlive;
      public readonly bool NetworkDrive;
      public string VolumeLabel;

      public DokanOptions(char driveLetter, ushort threadCount, bool debugMode, bool useStdErr, bool useAltStream, bool useKeepAlive, bool networkDrive)
      {
         DriveLetter = driveLetter;
         NetworkDrive = networkDrive;
         UseKeepAlive = useKeepAlive;
         UseAltStream = useAltStream;
         UseStdErr = useStdErr;
         DebugMode = debugMode;
         ThreadCount = threadCount;
      }
   }


   public class DokanNet
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      public const int ERROR_FILE_NOT_FOUND = 2;
      public const int ERROR_PATH_NOT_FOUND = 3;
      public const int ERROR_ACCESS_DENIED = 5;
      public const int ERROR_SHARING_VIOLATION = 32;
      public const int ERROR_INVALID_NAME = 123;
      public const int ERROR_FILE_EXISTS = 80;
      public const int ERROR_ALREADY_EXISTS = 183;

      public const int DOKAN_SUCCESS = 0;
      public const int DOKAN_ERROR = -1; // General Error
      public const int DOKAN_DRIVE_LETTER_ERROR = -2; // Bad Drive letter
      public const int DOKAN_DRIVER_INSTALL_ERROR = -3; // Can't install driver
      public const int DOKAN_START_ERROR = -4; // Driver something wrong
      public const int DOKAN_MOUNT_ERROR = -5; // Can't assign drive letter

      private const uint DOKAN_OPTION_DEBUG = 1;
      private const uint DOKAN_OPTION_STDERR = 2;
      private const uint DOKAN_OPTION_ALT_STREAM = 4;
      private const uint DOKAN_OPTION_KEEP_ALIVE = 8;
      private const uint DOKAN_OPTION_NETWORK = 16;

      public static int DokanMain(DokanOptions options, IDokanOperations operations)
      {
         Log.Info("Start DokanMain");
         if (options.VolumeLabel == null)
         {
            options.VolumeLabel = "DOKAN";
         }

         Proxy proxy = new Proxy(options, operations);

         var dokanOptions = new DOKAN_OPTIONS
                                         {
                                            DriveLetter = options.DriveLetter,
                                            ThreadCount = options.ThreadCount
                                         };

         dokanOptions.Options |= options.DebugMode ? DOKAN_OPTION_DEBUG : 0;
         dokanOptions.Options |= options.UseStdErr ? DOKAN_OPTION_STDERR : 0;
         dokanOptions.Options |= options.UseAltStream ? DOKAN_OPTION_ALT_STREAM : 0;
         dokanOptions.Options |= options.UseKeepAlive ? DOKAN_OPTION_KEEP_ALIVE : 0;
         dokanOptions.Options |= options.NetworkDrive ? DOKAN_OPTION_NETWORK : 0;

         var dokanOperations = new DOKAN_OPERATIONS
                                               {
                                                  CreateFile = proxy.CreateFileProxy,
                                                  OpenDirectory = proxy.OpenDirectoryProxy,
                                                  CreateDirectory = proxy.CreateDirectoryProxy,
                                                  Cleanup = proxy.CleanupProxy,
                                                  CloseFile = proxy.CloseFileProxy,
                                                  ReadFile = proxy.ReadFileProxy,
                                                  WriteFile = proxy.WriteFileProxy,
                                                  FlushFileBuffers = proxy.FlushFileBuffersProxy,
                                                  GetFileInformation = proxy.GetFileInformationProxy,
                                                  FindFiles = proxy.FindFilesProxy,
                                                  SetFileAttributes = proxy.SetFileAttributesProxy,
                                                  SetFileTime = proxy.SetFileTimeProxy,
                                                  DeleteFile = proxy.DeleteFileProxy,
                                                  DeleteDirectory = proxy.DeleteDirectoryProxy,
                                                  MoveFile = proxy.MoveFileProxy,
                                                  SetEndOfFile = proxy.SetEndOfFileProxy,
                                                  SetAllocationSize = proxy.SetAllocationSizeProxy,
                                                  LockFile = proxy.LockFileProxy,
                                                  UnlockFile = proxy.UnlockFileProxy,
                                                  GetDiskFreeSpace = proxy.GetDiskFreeSpaceProxy,
                                                  GetVolumeInformation = proxy.GetVolumeInformationProxy,
                                                  Unmount = proxy.UnmountProxy
                                               };

         return DokanDll.DokanMain(ref dokanOptions, ref dokanOperations);
      }


      public static int DokanUnmount(char driveLetter)
      {
         return DokanDll.DokanUnmount(driveLetter);
      }


      public static uint DokanVersion()
      {
         return DokanDll.DokanVersion();
      }

      public static uint DokanDriverVersion()
      {
         return DokanDll.DokanDriveVersion();
      }

      public static bool DokanResetTimeout(uint timeout, DokanFileInfo fileinfo)
      {
         var rawFileInfo = new DOKAN_FILE_INFO { DokanContext = fileinfo.DokanContext };
         return DokanDll.DokanResetTimeout(timeout, ref rawFileInfo);
      }
   }
}
