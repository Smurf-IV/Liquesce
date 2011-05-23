using System;
using NLog;

namespace DokanNet
{
   public class DokanOptions
   {
      public ushort Version;
      public ushort ThreadCount;
      public bool DebugMode;
      public bool UseStdErr;
      public bool UseAltStream;
      public bool UseKeepAlive;
      public bool NetworkDrive;
      public bool RemovableDrive;
      public string VolumeLabel;
      public string MountPoint;
   }


   public static class Dokan
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      // ReSharper disable InconsistentNaming
#pragma warning disable 169
      #region File Operation Errors
      // From WinError.h -> http://msdn.microsoft.com/en-us/library/ms819773.aspx
      public const int ERROR_FILE_NOT_FOUND = -2;  // MessageText: The system cannot find the file specified.
      public const int ERROR_PATH_NOT_FOUND = -3;  // MessageText: The system cannot find the path specified.
      public const int ERROR_ACCESS_DENIED = -5;   // MessageText: Access is denied.
      public const int ERROR_SHARING_VIOLATION = -32;
      public const int ERROR_FILE_EXISTS = -80;
      public const int ERROR_DISK_FULL = -112;     // There is not enough space on the disk.
      public const int ERROR_INVALID_NAME = -123;
      public const int ERROR_DIR_NOT_EMPTY = -145; // MessageText: The directory is not empty.
      public const int ERROR_ALREADY_EXISTS = -183;// MessageText: Cannot create a file when that file already exists.
      public const int ERROR_EXCEPTION_IN_SERVICE = -1064;//  An exception occurred in the service when handling thecontrol request.

      #endregion

      #region Dokan Driver Errors
      public const int DOKAN_SUCCESS = 0;
      public const int DOKAN_ERROR = -1; // General Error
      public const int DOKAN_DRIVE_LETTER_ERROR = -2; // Bad Drive letter
      public const int DOKAN_DRIVER_INSTALL_ERROR = -3; // Can't install driver
      public const int DOKAN_START_ERROR = -4; // Driver something wrong
      public const int DOKAN_MOUNT_ERROR = -5; // Can't assign drive letter
      #endregion

      private const ushort DOKAN_VERSION = 600; // ver 0.6.0

      #region Dokan Driver Options
      private const uint DOKAN_OPTION_DEBUG = 1;
      private const uint DOKAN_OPTION_STDERR = 2;
      private const uint DOKAN_OPTION_ALT_STREAM = 4;
      private const uint DOKAN_OPTION_KEEP_ALIVE = 8;
      private const uint DOKAN_OPTION_NETWORK = 16;
      private const uint DOKAN_OPTION_REMOVABLE = 32;
      #endregion
#pragma warning restore 169
      // ReSharper restore InconsistentNaming



      public static int DokanMain(DokanOptions options, IDokanOperations operations)
      {
         Log.Info("Start DokanMain");
         if (String.IsNullOrEmpty(options.VolumeLabel))
         {
            options.VolumeLabel = "DOKAN";
         }

         Proxy proxy = new Proxy(options, operations);

         var dokanOptions = new DOKAN_OPTIONS
                                         {
                                            Version = options.Version != 0 ? options.Version : DOKAN_VERSION,
                                            MountPoint = options.MountPoint,
                                            ThreadCount = options.ThreadCount
                                         };

         dokanOptions.Options |= options.RemovableDrive ? DOKAN_OPTION_REMOVABLE : 0;
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
                                                  //FindFilesWithPattern = proxy.FindFilesWithPatternProxy,
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
                                                  //,GetFileSecurity = proxy.GetFileSecurity
                                                  //,SetFileSecurity = proxy.SetFileSecurity
                                               };

         return DokanDll.DokanMain(ref dokanOptions, ref dokanOperations);
      }


      public static int DokanUnmount(char driveLetter)
      {
         return DokanDll.DokanUnmount(driveLetter);
      }

      public static int DokanRemoveMountPoint(string mountPoint)
      {
         return Dokan.DokanRemoveMountPoint(mountPoint);
      }

      public static uint DokanVersion()
      {
         return DokanDll.DokanVersion();
      }

      public static uint DokanDriverVersion()
      {
         return DokanDll.DokanDriverVersion();
      }

      //public static bool DokanResetTimeout(uint timeout, DokanFileInfo fileinfo)
      //{
      //   var rawFileInfo = new DOKAN_FILE_INFO { DokanContext = fileinfo.DokanContext };
      //   return DokanDll.DokanResetTimeout(timeout, ref rawFileInfo);
      //}
   }
}
