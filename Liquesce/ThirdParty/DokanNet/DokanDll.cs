using System;
using System.Runtime.InteropServices;

namespace DokanNet
{
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
   struct DOKAN_OPTIONS
   {
      public ushort Version;
      public ushort ThreadCount; // number of threads to be used
      public uint Options;
      public ulong Dummy1;
      [MarshalAs(UnmanagedType.LPWStr)]
      public string MountPoint;
   }

   // this struct must be the same layout as DOKAN_OPERATIONS
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   struct DOKAN_OPERATIONS
   {
      public Proxy.CreateFileDelegate CreateFile;
      public Proxy.OpenDirectoryDelegate OpenDirectory;
      public Proxy.CreateDirectoryDelegate CreateDirectory;
      public Proxy.CleanupDelegate Cleanup;
      public Proxy.CloseFileDelegate CloseFile;
      public Proxy.ReadFileDelegate ReadFile;
      public Proxy.WriteFileDelegate WriteFile;
      public Proxy.FlushFileBuffersDelegate FlushFileBuffers;
      public Proxy.GetFileInformationDelegate GetFileInformation;
      public Proxy.FindFilesDelegate FindFiles;
      public Proxy.FindFilesWithPatternDelegate FindFilesWithPattern; // this is not used in DokanNet
      public Proxy.SetFileAttributesDelegate SetFileAttributes;
      public Proxy.SetFileTimeDelegate SetFileTime;
      public Proxy.DeleteFileDelegate DeleteFile;
      public Proxy.DeleteDirectoryDelegate DeleteDirectory;
      public Proxy.MoveFileDelegate MoveFile;
      public Proxy.SetEndOfFileDelegate SetEndOfFile;
      public Proxy.SetAllocationSizeDelegate SetAllocationSize;
      public Proxy.LockFileDelegate LockFile;
      public Proxy.UnlockFileDelegate UnlockFile;
      public Proxy.GetDiskFreeSpaceDelegate GetDiskFreeSpace;
      public Proxy.GetVolumeInformationDelegate GetVolumeInformation;
      public Proxy.UnmountDelegate Unmount;
      public Proxy.GetFileSecurityDelegate GetFileSecurity;
      public Proxy.SetFileSecurityDelegate SetFileSecurity;
   }

   static class DokanDll
   {
      [DllImport("dokan.dll")]
      public static extern int DokanMain(ref DOKAN_OPTIONS options, ref DOKAN_OPERATIONS operations);

      [DllImport("dokan.dll")]
      public static extern int DokanUnmount(char driveLetter);

      [DllImport("dokan.dll")]
      public static extern uint DokanVersion();

      [DllImport("dokan.dll")]
      public static extern uint DokanDriverVersion();

      [DllImport("dokan.dll")]
      public static extern int DokanRemoveMountPoint([MarshalAs(UnmanagedType.LPWStr)] string mountPoint);

      [DllImport("dokan.dll")]
      public static extern bool DokanResetTimeout(uint timeout, ref DOKAN_FILE_INFO rawFileInfo);

      [DllImport("dokan.dll", CharSet = CharSet.Unicode)]
      public static extern bool DokanIsNameInExpression(string Expression, // matching pattern
         string Name, // file name
         bool IgnoreCase);


   }
}