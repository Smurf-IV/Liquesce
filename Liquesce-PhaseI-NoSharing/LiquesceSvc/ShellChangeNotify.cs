#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2011-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Runtime.InteropServices;
using NLog;

namespace LiquesceSvc
{
   /// <summary>
   /// This takes all(most) the operations of the Dokan Interface and implement the apropriate function to notift the OS of changes to the mounted drive
   /// </summary>
   /// <see cref="http://msdn.microsoft.com/en-us/library/bb762118.aspx"/>
   internal class ShellChangeNotify /*: IDokanOperations*/
   {
      [Flags]
      internal enum UpdateType
      {
         Write = 1,
         Delete = Write << 1,
         Attributes = Delete << 1,
         Times = Attributes << 1,
         Security = Times << 1,
         DeleteDir = Security << 1,
         DeleteFile = DeleteDir << 1,
         Size = DeleteFile << 1
      }

      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly Roots roots;
      private readonly Dictionary<UInt64, UpdateType> fileHandleContextUpdate = new Dictionary<UInt64, UpdateType>();

      public ShellChangeNotify(Roots roots)
      {
         this.roots = roots;
      }

      #region Implementation of IDokanOperations

      /// <summary>
      /// It is assumed that the file creaiton has been successfull by this point
      /// </summary>
      /// <param name="actualFilename"></param>
      /// <param name="fileHandleContext"></param>
      /// <param name="isNew"></param>
      public void CreateFile(string actualFilename, UInt64 fileHandleContext,  bool isNew)
      {
         if (isNew)
         {
            Log.Trace("Fire ShellChangeNotify for CreateFile on [{0}]", actualFilename);
            using (ConvertStringToIntPtr str = new ConvertStringToIntPtr(roots.ReturnMountFileName(actualFilename)))
               SHChangeNotify(HChangeNotifyEventID.SHCNE_CREATE,
                           HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
         }
      }

      public static void OpenDirectory(string actualFilename, UInt64 fileHandleContext)
      {}

      public void CreateDirectory(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for CreateDirectory on [{0}]", actualFilename);
         using (ConvertStringToIntPtr str = new ConvertStringToIntPtr(roots.ReturnMountFileName(actualFilename)))
            SHChangeNotify(HChangeNotifyEventID.SHCNE_MKDIR,
                        HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
      }

      public UpdateType Cleanup(string actualFilename, UInt64 fileHandleContext)
      {
         UpdateType type;
         if (fileHandleContextUpdate.TryGetValue(fileHandleContext, out type))
         {
            fileHandleContextUpdate.Remove(fileHandleContext);
            CombinedNotify(actualFilename, type);
         }
         return type;
      }

      public void CombinedNotify(string targetFullFilename, UpdateType type)
      {
         Log.Trace("Fire CombinedNotify for Cleanup on [{0}] with [{1}]", targetFullFilename, type);
         HChangeNotifyEventID updateWith = HChangeNotifyEventID.SHCNE_FREESPACE;
         if ((type & UpdateType.Write) == UpdateType.Write)
            updateWith |= HChangeNotifyEventID.SHCNE_FREESPACE;
         if ((type & UpdateType.DeleteDir) == UpdateType.DeleteDir)
            updateWith |= HChangeNotifyEventID.SHCNE_RMDIR;
         if ((type & UpdateType.DeleteFile) == UpdateType.DeleteFile)
            updateWith |= HChangeNotifyEventID.SHCNE_DELETE;
         if ((type & UpdateType.Times) == UpdateType.Times)
            updateWith |= HChangeNotifyEventID.SHCNE_ATTRIBUTES;
         if ((type & UpdateType.Security) == UpdateType.Security)
            updateWith |= HChangeNotifyEventID.SHCNE_ATTRIBUTES;
         if ((type & UpdateType.Size) == UpdateType.Size)
            updateWith |= HChangeNotifyEventID.SHCNE_FREESPACE;

         using (ConvertStringToIntPtr str = new ConvertStringToIntPtr(targetFullFilename))
            SHChangeNotify(updateWith,
                           HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT,
                           str.Ptr, IntPtr.Zero);
      }

      public static void CloseFile(string actualFilename, UInt64 fileHandleContext)
      {}

      public static void ReadFileNative(string actualFilename, UInt64 fileHandleContext)
      {}

      public void WriteFileNative(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for WriteFileNative on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Write;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public static void FlushFileBuffers(string actualFilename, UInt64 fileHandleContext)
      {}

      public static void GetFileInformationNative(string actualFilename, UInt64 fileHandleContext)
      {}

      //public void FindFiles(string actualFilename, UInt64 fileHandleContext)
      //{}

      //public void FindFilesWithPattern(string actualFilename, UInt64 fileHandleContext)
      //{}

      public void SetFileAttributes(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for SetFileAttributes on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Attributes;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public void SetFileTimeNative(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for SetFileTimeNative on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Times;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public void DeleteFile(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for DeleteFile on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.DeleteFile;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public void DeleteDirectory(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for DeleteDirectory on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.DeleteDir;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public void MoveFile(string oldFilename, string newFilename, bool isFolder, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for MoveFile on [{0}] to [{1}], as a folder[{2}]", oldFilename, newFilename, isFolder);

         using (ConvertStringToIntPtr strOld = new ConvertStringToIntPtr(roots.ReturnMountFileName(oldFilename)))
         {
            using (ConvertStringToIntPtr strNew = new ConvertStringToIntPtr(roots.ReturnMountFileName(newFilename)))
            {
               SHChangeNotify(
                  (isFolder ? HChangeNotifyEventID.SHCNE_RENAMEFOLDER : HChangeNotifyEventID.SHCNE_RENAMEITEM),
                  HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT,
                  strOld.Ptr, strNew.Ptr
                  );
            }
         }
      }

      public void SetEndOfFile(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for SetEndOfFile on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Size;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public void SetAllocationSize(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for SetAllocationSize on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Size;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      public static void LockFile(string actualFilename, UInt64 fileHandleContext)
      {}

      public static void UnlockFile(string actualFilename, UInt64 fileHandleContext)
      {}

      //public void GetDiskFreeSpace( UInt64 fileHandleContext)
      //{}

      public static void Mount(char mountedDriveLetter)
      {
         string mountPoint = mountedDriveLetter + ":\\";
         using (ConvertStringToIntPtr str = new ConvertStringToIntPtr(mountPoint))
         {
            SHChangeNotify(HChangeNotifyEventID.SHCNE_DRIVEADD,
                            HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
            SHChangeNotify(HChangeNotifyEventID.SHCNE_MKDIR,
                            HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
         }
      }

      public static void Unmount(char mountedDriveLetter)
      {
         string mountPoint = mountedDriveLetter + ":\\";
         using (ConvertStringToIntPtr str = new ConvertStringToIntPtr(mountPoint))
         {
            SHChangeNotify(HChangeNotifyEventID.SHCNE_RMDIR,
                            HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
            SHChangeNotify(HChangeNotifyEventID.SHCNE_DRIVEREMOVED,
                            HChangeNotifyFlags.SHCNF_PATHW | HChangeNotifyFlags.SHCNF_FLUSHNOWAIT, str.Ptr, IntPtr.Zero);
         }
      }

      public static void GetFileSecurityNative(string actualFilename, UInt64 fileHandleContext)
      {}

      public void SetFileSecurityNative(string actualFilename, UInt64 fileHandleContext)
      {
         Log.Trace("Fire ShellChangeNotify for SetFileSecurityNative on [{0}]", actualFilename);
         UpdateType modType;
         fileHandleContextUpdate.TryGetValue(fileHandleContext, out modType);
         modType |= UpdateType.Security;
         fileHandleContextUpdate[fileHandleContext] = modType;
      }

      #endregion

      /// <summary>
      /// 
      /// </summary>
      /// <param name="wEventId"></param>
      /// <param name="uFlags"></param>
      /// <param name="dwItem1"></param>
      /// <param name="dwItem2"></param>
      /// <see cref="http://www.pinvoke.net/default.aspx/shell32/SHChangeNotify.html"/>
      [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      static extern void SHChangeNotify(HChangeNotifyEventID wEventId, HChangeNotifyFlags uFlags, IntPtr dwItem1, IntPtr dwItem2);

      #region enum HChangeNotifyEventID
      /// <summary>
      /// Describes the event that has occurred.
      /// Typically, only one event is specified at a time.
      /// If more than one event is specified, the values contained
      /// in the <i>dwItem1</i> and <i>dwItem2</i>
      /// parameters must be the same, respectively, for all specified events.
      /// This parameter can be one or more of the following values.
      /// </summary>
      /// <remarks>
      /// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index
      /// in the system image list that has changed.
      /// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
      /// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index
      /// in the system image list that has changed.
      /// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
      /// </remarks>
      /// <see cref="http://www.pinvoke.net/default.aspx/shell32/HChangeNotifyEventID.html"/>
      [Flags]
      enum HChangeNotifyEventID
      {
         /// <summary>
         /// All events have occurred.
         /// </summary>
         SHCNE_ALLEVENTS = 0x7FFFFFFF,

         /// <summary>
         /// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
         /// must be specified in the <i>uFlags</i> parameter.
         /// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>.
         /// </summary>
         SHCNE_ASSOCCHANGED = 0x08000000,

         /// <summary>
         /// The attributes of an item or folder have changed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the item or folder that has changed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_ATTRIBUTES = 0x00000800,

         /// <summary>
         /// A nonfolder item has been created.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the item that was created.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_CREATE = 0x00000002,

         /// <summary>
         /// A nonfolder item has been deleted.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the item that was deleted.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_DELETE = 0x00000004,

         /// <summary>
         /// A drive has been added.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive that was added.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_DRIVEADD = 0x00000100,

         /// <summary>
         /// A drive has been added and the Shell should create a new window for the drive.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive that was added.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_DRIVEADDGUI = 0x00010000,

         /// <summary>
         /// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive that was removed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_DRIVEREMOVED = 0x00000080,

         /// <summary>
         /// Not currently used.
         /// </summary>
         SHCNE_EXTENDED_EVENT = 0x04000000,

         /// <summary>
         /// The amount of free space on a drive has changed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive on which the free space changed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_FREESPACE = 0x00040000,

         /// <summary>
         /// Storage media has been inserted into a drive.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive that contains the new media.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_MEDIAINSERTED = 0x00000020,

         /// <summary>
         /// Storage media has been removed from a drive.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the root of the drive from which the media was removed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_MEDIAREMOVED = 0x00000040,

         /// <summary>
         /// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/>
         /// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the folder that was created.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_MKDIR = 0x00000008,

         /// <summary>
         /// A folder on the local computer is being shared via the network.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the folder that is being shared.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_NETSHARE = 0x00000200,

         /// <summary>
         /// A folder on the local computer is no longer being shared via the network.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the folder that is no longer being shared.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_NETUNSHARE = 0x00000400,

         /// <summary>
         /// The name of a folder has changed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder.
         /// <i>dwItem2</i> contains the new PIDL or name of the folder.
         /// </summary>
         SHCNE_RENAMEFOLDER = 0x00020000,

         /// <summary>
         /// The name of a nonfolder item has changed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the previous PIDL or name of the item.
         /// <i>dwItem2</i> contains the new PIDL or name of the item.
         /// </summary>
         SHCNE_RENAMEITEM = 0x00000001,

         /// <summary>
         /// A folder has been removed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the folder that was removed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_RMDIR = 0x00000010,

         /// <summary>
         /// The computer has disconnected from a server.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the server from which the computer was disconnected.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// </summary>
         SHCNE_SERVERDISCONNECT = 0x00004000,

         /// <summary>
         /// The contents of an existing folder have changed,
         /// but the folder still exists and has not been renamed.
         /// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or
         /// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>.
         /// <i>dwItem1</i> contains the folder that has changed.
         /// <i>dwItem2</i> is not used and should be <see langword="null"/>.
         /// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or
         /// SHCNE_RENAMEFOLDER, respectively, instead.
         /// </summary>
         SHCNE_UPDATEDIR = 0x00001000,

         /// <summary>
         /// An image in the system image list has changed.
         /// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>.
         /// </summary>
         SHCNE_UPDATEIMAGE = 0x00008000,

      }
      #endregion // enum HChangeNotifyEventID

      #region public enum HChangeNotifyFlags
      /// <summary>
      /// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters.
      /// The uFlags parameter must be one of the following values.
      /// </summary>
      /// <see cref="http://www.pinvoke.net/default.aspx/shell32/HChangeNotifyFlags.html"/>
      [Flags]
      private enum HChangeNotifyFlags
      {
         /// <summary>
         /// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values.
         /// </summary>
         SHCNF_DWORD = 0x0003,
         /// <summary>
         /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that
         /// represent the item(s) affected by the change.
         /// Each ITEMIDLIST must be relative to the desktop folder.
         /// </summary>
         SHCNF_IDLIST = 0x0000,
         /// <summary>
         /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
         /// maximum length MAX_PATH that contain the full path names
         /// of the items affected by the change.
         /// </summary>
         SHCNF_PATHA = 0x0001,
         /// <summary>
         /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of
         /// maximum length MAX_PATH that contain the full path names
         /// of the items affected by the change.
         /// </summary>
         SHCNF_PATHW = 0x0005,
         /// <summary>
         /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
         /// represent the friendly names of the printer(s) affected by the change.
         /// </summary>
         SHCNF_PRINTERA = 0x0002,
         /// <summary>
         /// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that
         /// represent the friendly names of the printer(s) affected by the change.
         /// </summary>
         SHCNF_PRINTERW = 0x0006,
         /// <summary>
         /// The function should not return until the notification
         /// has been delivered to all affected components.
         /// As this flag modifies other data-type flags, it cannot by used by itself.
         /// </summary>
         SHCNF_FLUSH = 0x1000,
         /// <summary>
         /// The function should begin delivering notifications to all affected components
         /// but should return as soon as the notification process has begun.
         /// As this flag modifies other data-type flags, it cannot by used by itself.
         /// </summary>
         SHCNF_FLUSHNOWAIT = 0x2000
      }
      #endregion // enum HChangeNotifyFlags

      class ConvertStringToIntPtr : IDisposable
      {
         private IntPtr ptr = IntPtr.Zero;

         public ConvertStringToIntPtr(string str)
         {
            ptr = Marshal.StringToHGlobalAuto(str);
         }

         public IntPtr Ptr
         {
            get { return ptr; }
         }

         public void Dispose()
         {
            if (ptr != IntPtr.Zero)
            {
               Marshal.FreeHGlobal(ptr);
               ptr = IntPtr.Zero;
            }
         }
      }
   }
}
