using NLog;

namespace DokanNet
{
   public class DokanOptions
   {
      public char DriveLetter;
      public ushort ThreadCount;
      public bool DebugMode;
      public bool UseStdErr;
      public bool UseAltStream;
      public bool UseKeepAlive;
      public bool NetworkDrive;
      public string VolumeLabel;
   }


   public static class Dokan
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      #region File Operation Errors (From WinError.h)
      public const int ERROR_FILE_NOT_FOUND = -2;  // MessageText: The system cannot find the file specified.
      public const int ERROR_PATH_NOT_FOUND = -3;  // MessageText: The system cannot find the path specified.
      public const int ERROR_ACCESS_DENIED = -5;   // MessageText: Access is denied.
      public const int ERROR_SHARING_VIOLATION = -32;
      public const int ERROR_INVALID_NAME = -123;
      public const int ERROR_FILE_EXISTS = -80;
      public const int ERROR_DIR_NOT_EMPTY = -145; // MessageText: The directory is not empty.
      public const int ERROR_ALREADY_EXISTS = -183;// MessageText: Cannot create a file when that file already exists.
      #endregion

      #region Dokan Driver Errors
      public const int DOKAN_SUCCESS = 0;
      public const int DOKAN_ERROR = -1; // General Error
      public const int DOKAN_DRIVE_LETTER_ERROR = -2; // Bad Drive letter
      public const int DOKAN_DRIVER_INSTALL_ERROR = -3; // Can't install driver
      public const int DOKAN_START_ERROR = -4; // Driver something wrong
      public const int DOKAN_MOUNT_ERROR = -5; // Can't assign drive letter
      #endregion

      #region Dokan Driver Options
      private const uint DOKAN_OPTION_DEBUG = 1;
      private const uint DOKAN_OPTION_STDERR = 2;
      private const uint DOKAN_OPTION_ALT_STREAM = 4;
      private const uint DOKAN_OPTION_KEEP_ALIVE = 8;
      private const uint DOKAN_OPTION_NETWORK = 16;
      #endregion

#region File Constants from Win32
      public const uint GENERIC_READ = 0x80000000;
      public const uint GENERIC_WRITE = 0x40000000;
      public const uint GENERIC_EXECUTE = 0x20000000;

      public const uint FILE_READ_DATA = 0x00000001;
      public const uint FILE_WRITE_DATA = 0x00000002;
      public const uint FILE_APPEND_DATA = 0x00000004;
      public const uint FILE_READ_EA = 0x00000008;
      public const uint FILE_WRITE_EA = 0x00000010;
      public const uint FILE_EXECUTE = 0x00000020;
      public const uint FILE_READ_ATTRIBUTES = 0x00000080;
      public const uint FILE_WRITE_ATTRIBUTES = 0x00000100;
      public const uint DELETE = 0x00010000;
      public const uint READ_CONTROL = 0x00020000;
      public const uint WRITE_DAC = 0x00040000;
      public const uint WRITE_OWNER = 0x00080000;
      public const uint SYNCHRONIZE = 0x00100000;

      public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;

      public const uint STANDARD_RIGHTS_READ = READ_CONTROL;
      public const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
      public const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;

      public const uint FILE_SHARE_READ = 0x00000001;
      public const uint FILE_SHARE_WRITE = 0x00000002;
      public const uint FILE_SHARE_DELETE = 0x00000004;

      public const uint CREATE_NEW = 1;
      public const uint CREATE_ALWAYS = 2;
      public const uint OPEN_EXISTING = 3;
      public const uint OPEN_ALWAYS = 4;
      public const uint TRUNCATE_EXISTING = 5;

      public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
      public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
      public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
      public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
      public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
      public const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
      public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
      public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
      public const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;

      //
      // File creation flags must start at the high end since they
      // are combined with the attributes
      //

      public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
      public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
      public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
      public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
      public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
      public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
      public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
      public const uint FILE_FLAG_POSIX_SEMANTICS = 0x01000000;
      public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
      public const uint FILE_FLAG_OPEN_NO_RECALL = 0x00100000;
      public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;

#endregion


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
