#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Roots.cs" company="Smurf-IV">
//
//  Copyright (C) 2010-2014 Simon Coghlan (Aka Smurf-IV)
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

#endregion Copyright (C)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using LiquesceFacade;
using LiquesceSvc.LowLevelOSAccess;
using NLog;

namespace LiquesceSvc
{
   /// <summary>
   /// this class delivers the current physical root of the disk which should be used next
   /// for file/folder creation.
   /// It also handles a few other fileName operations like detection, and deletion.
   /// </summary>
   internal class Roots
   {
      public static readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
      private static readonly string RootPathDirectorySeparatorChar = PathDirectorySeparatorChar + PathDirectorySeparatorChar;
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      private readonly CachedRootPathsSystemInfo cachedRootPathsSystemInfo;

      private readonly MountDetail mountDetail;

      private readonly bool AreAllReadOnly;
      private readonly bool AreAnyAllReadOnly;

      // constructor
      public Roots(MountDetail mountDetail, uint cacheLifetimeSeconds)
      {
         this.mountDetail = mountDetail;
         AreAnyAllReadOnly = mountDetail.SourceLocations.Any(sl => sl.UseIsReadOnly);
         if (AreAnyAllReadOnly)
         {
            AreAllReadOnly = mountDetail.SourceLocations.TrueForAll(sl => sl.UseIsReadOnly);
         }
         // NTFS is case-preserving but case-insensitive in the Win32 namespace
         cachedRootPathsSystemInfo = new CachedRootPathsSystemInfo(cacheLifetimeSeconds); 
      }

      public NativeFileOps GetFromPathFileName(string pathFileName)
      {
         Log.Trace("GetFromPathFileName [{0}]", pathFileName);
         NativeFileOps fsi = null;
         bool isNamedStream = false;
         string namedStream = string.Empty;
         try
         {
            if (cachedRootPathsSystemInfo.TryGetValue(pathFileName, out fsi))
            {
               Log.Trace("Found pathFileName in cache");
               return fsi;
            }
            if ((pathFileName.Length == 1)
               && NativeFileOps.IsDirectorySeparator(pathFileName[0])
               )
            {
               Log.Trace("Assuming Home directory so add new to cache and return");
               string firstSourceLocation = mountDetail.SourceLocations.First().SourcePath;
               return (fsi = new NativeFileOps(firstSourceLocation, IsRootReadOnly(firstSourceLocation)));
            }

            string searchFilename = RemoveStreamPart(pathFileName, out isNamedStream, out namedStream);

            if (isNamedStream)
            {
               if (string.IsNullOrEmpty(searchFilename))
               {
                  // TODO: Should something have gone boom before this ?
                  if (System.Diagnostics.Debugger.IsAttached)
                     System.Diagnostics.Debugger.Break();
               }
               if (cachedRootPathsSystemInfo.TryGetValue(searchFilename, out fsi))
               {
                  Log.Trace("Found in cache from native not stream");
                  return (fsi = new NativeFileOps(string.Format("{0}:{1}", fsi.FullName, namedStream), IsRootReadOnly(fsi.DirectoryPathOnly)));
               }
            }
            Log.Trace("Not found in cache so search for filename");

            // TODO: Not found, so check to see if this is a recycler bin offset

            foreach (string sourceLocation in GetAllRootPathsWhereExists(NativeFileOps.GetParentPathName(pathFileName)))
            {
               fsi = new NativeFileOps(Path.Combine(sourceLocation, searchFilename), IsRootReadOnly(sourceLocation));
               if (fsi.Exists)
               {
                  return fsi;
               }
            }

            //-------------------------------------
            Log.Trace("file/folder not found");
            // So create a holder for the return and do not store
            fsi = FindCreateNewAllocationRootPath(searchFilename);
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFromPathFileName threw: ", ex);
         }
         finally
         {
            if ((fsi != null)
               && fsi.Exists
               )
            {
               Log.Debug("GetFromPathFileName from [{0}] found [{1}]", pathFileName, fsi.FullName);
               if (string.IsNullOrEmpty(pathFileName))
               {
                  // TODO: Should something have gone boom before this ?
                  if (System.Diagnostics.Debugger.IsAttached)
                     System.Diagnostics.Debugger.Break();
               }

               cachedRootPathsSystemInfo[pathFileName] = fsi;
               if (isNamedStream)
               {
                  Log.Warn("isNamedStream [{0}] found [{1}]", pathFileName, namedStream);
                  cachedRootPathsSystemInfo[string.Format("{0}:{1}", pathFileName, namedStream)] = fsi;
               }
            }
            else
            {
               Log.Debug("GetFromPathFileName found nothing for [{0}].", pathFileName);
            }
         }
         return fsi;
      }

      private static string RemoveStreamPart(string pathFileName, out bool isNamedStream, out string namedStream)
      {
         string searchFilename = pathFileName.Trim(Path.DirectorySeparatorChar);
         string[] splits = searchFilename.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
         int offset = Path.IsPathRooted(pathFileName) ? 2 : 1;
         if (splits.Length > offset)
         {
            isNamedStream = true;
            searchFilename = splits[offset - 1];
            namedStream = splits[offset];
         }
         else
         {
            isNamedStream = false;
            namedStream = string.Empty;
         }
         return searchFilename;
      }

      private bool IsRootReadOnly(string newTarget)
      {
         if (AreAllReadOnly)
         {
            return true;
         }
         if (!AreAnyAllReadOnly)
         {
            return false;
         }
         string root = GetRoot(newTarget);
         return mountDetail.SourceLocations.Any(location => (location.SourcePath == root) && (location.UseIsReadOnly));
      }

      public IEnumerable<string> GetFullPathsThatContainThis(string relativefolder)
      {
         return from location in mountDetail.SourceLocations
                select new DirectoryInfo(location.SourcePath + relativefolder)
                   into dir
                   where dir.Exists
                   select dir.FullName;
      }

      public IEnumerable<string> GetAllRootPathsWhereExists(string relativefolder)
      {
         return mountDetail.SourceLocations.Where(location => Directory.Exists(location.SourcePath + relativefolder)).Select(location => location.SourcePath);
      }

      public string GetRoot(string path)
      {
         if (!string.IsNullOrEmpty(path))
         {
            foreach (SourceLocation location in mountDetail.SourceLocations.Where(location => path.Contains(location.SourcePath)))
            {
               return location.SourcePath;
            }
         }
         return string.Empty;
      }

      // return the path from a inputpath seen relative from the root
      public string GetRelative(string path)
      {
         return RootPathDirectorySeparatorChar + path.Replace(GetRoot(path), string.Empty);
      }

      public string FindByFileId(long fileId)
      {
         foreach (string found in mountDetail.SourceLocations
            .Select(location => NfsSupport.GetByFileId(location.SourcePath, fileId))
            .Where(found => !string.IsNullOrEmpty(found)))
         {
            return GetRelative(found);
         }
         return string.Empty;
      }

      internal static readonly string[] RecyclerDirectoryNames = { @"$RECYCLE.BIN", @"Recycle Bin", @"RECYCLER", @"Recycled" };

      // this method returns a path (real physical path) of a place where the next folder/file root can be created.
      public NativeFileOps FindCreateNewAllocationRootPath(string pathFileName, UInt64 length = 0)
      {
         ulong spaceRequired = Math.Max(mountDetail.HoldOffBufferBytes, length);

         const int dirSize = 4 * 1024; // Even A directory requires 4KB to be created !
         if (spaceRequired < dirSize)
         {
            spaceRequired = dirSize;
         }
         Log.Trace("FindCreateNewAllocationRootPath [{0}] spaceRequired [{1}]", pathFileName, spaceRequired);
         bool isNamedStream;
         string namedStream;
         string searchFilename = RemoveStreamPart(pathFileName, out isNamedStream, out namedStream);

         string foundRoot = null;
         string relativeParent = NativeFileOps.GetParentPathName(pathFileName);

         NativeFileOps fsi;
         if (cachedRootPathsSystemInfo.TryGetValue(relativeParent, out fsi))
         {
            Log.Trace("Found relativeParent in cache");
            foundRoot = GetRoot(fsi.FullName);
            if (!CheckSourceForSpace(spaceRequired, foundRoot))
            {
               foundRoot = string.Empty;
            }
         }
         if (string.IsNullOrEmpty(foundRoot))
         {
            switch (mountDetail.AllocationMode)
            {
               case MountDetail.AllocationModes.Folder:
                  foundRoot = GetWriteableSourceThatMatchesThisFolderWithSpace(relativeParent, spaceRequired);
                  if (string.IsNullOrEmpty(foundRoot))
                     goto case MountDetail.AllocationModes.Priority;
                  break;

               case MountDetail.AllocationModes.Priority:
                  foundRoot = GetWriteableHighestPrioritySourceWithSpace(spaceRequired);
                  if (string.IsNullOrEmpty(foundRoot))
                     goto case MountDetail.AllocationModes.Balanced;
                  break;

               case MountDetail.AllocationModes.Balanced:
                  foundRoot = GetWriteableSourceWithMostFreeSpace(spaceRequired);
                  break;

               default:
                  foundRoot = GetWriteableSourceWithMostFreeSpace(spaceRequired);
                  break;
            }
         }
         string newPathName = Path.Combine(foundRoot, searchFilename);
         // TODO: Should something have gone boom before this ?
         if (System.Diagnostics.Debugger.IsAttached)
         {
            if ((newPathName == searchFilename)
                || (newPathName == pathFileName)
               )
            {
               System.Diagnostics.Debugger.Break();
            }
         }

         NativeFileOps newAllocationRootPath = isNamedStream ? new NativeFileOps(string.Format("{0}:{1}", newPathName, namedStream), IsRootReadOnly(foundRoot))
                                                            : new NativeFileOps(newPathName, IsRootReadOnly(foundRoot));
         // If a recycler is required then request usage of an existing one from a root drive.
         if (RecyclerDirectoryNames.Any(pathFileName.Contains))
         {
            newAllocationRootPath = new NativeFileOps(NativeFileOps.GetRootOrMountFor(newAllocationRootPath.FullName) + pathFileName,
               newAllocationRootPath.ForceUseAsReadOnly);
         }

         return newAllocationRootPath;
      }

      // returns the root for:
      //  The first disk where relativeFolder exists and if there is enough free space
      private string GetWriteableSourceThatMatchesThisFolderWithSpace(string relativeFolder, ulong spaceRequired)
      {
         Log.Trace("Trying GetSourceThatMatchesThisFolderWithSpace([{0}],[{1}])", relativeFolder, spaceRequired);
         // remove the last \ to delete the last directory
         relativeFolder = relativeFolder.TrimEnd(new[] { Path.DirectorySeparatorChar });

         // for every source location
         foreach (string sourcePath in
            from sourcePath in mountDetail.SourceLocations
                  .Where(s => !s.UseIsReadOnly)
                  .Select(s => s.SourcePath)
            where CheckSourceForSpace(spaceRequired, sourcePath)
            let testpath = sourcePath + relativeFolder
            where new NativeFileOps(testpath, AreAllReadOnly).Exists
            select sourcePath
                     )
         {
            return sourcePath;
         }
         return string.Empty;
      }

      // returns the next root with the highest priority and the space
      private string GetWriteableHighestPrioritySourceWithSpace(ulong spaceRequired)
      {
         Log.Trace("Trying GetHighestPrioritySourceWithSpace([{0}])", spaceRequired);
         foreach (SourceLocation w in mountDetail.SourceLocations.Where(s => !s.UseIsReadOnly))
         {
            if (CheckSourceForSpace(spaceRequired, w.SourcePath))
               return w.SourcePath;
         }
         return string.Empty;
      }

      private static bool CheckSourceForSpace(ulong spaceRequired, string sourcePath)
      {
         ulong lpFreeBytesAvailable, num2, num3;
         // Regardless of the API owner Process ID, make sure "we" can get the answer
         new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
         return (GetDiskFreeSpaceExW(sourcePath, out lpFreeBytesAvailable, out num2, out num3)
                 && (lpFreeBytesAvailable > spaceRequired)
                );
      }

      // returns the root with the most free space
      private string GetWriteableSourceWithMostFreeSpace(ulong spaceRequired)
      {
         Log.Trace("Trying GetSourceWithMostFreeSpace([{0}])", spaceRequired);
         ulong highestFreeSpace = 0;
         string sourceWithMostFreeSpace = string.Empty;

         // Regardless of the API owner Process ID, make sure "we" can get the answer
         new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
         foreach (SourceLocation str in mountDetail.SourceLocations.Where(s => !s.UseIsReadOnly))
         {
            ulong num, num2, num3;
            if (GetDiskFreeSpaceExW(str.SourcePath, out num, out num2, out num3))
            {
               if (highestFreeSpace < num)
               {
                  highestFreeSpace = num;
                  sourceWithMostFreeSpace = str.SourcePath;
               }
            }
         }
         if (highestFreeSpace < spaceRequired)
            Log.Warn("Amount of free space[{0}] on [{1}] is less than required [{2}]", highestFreeSpace, sourceWithMostFreeSpace, spaceRequired);
         return sourceWithMostFreeSpace;
      }

      private string FindOffsetPath(string fullFilePath)
      {
         foreach (SourceLocation location in mountDetail.SourceLocations.Where(location => fullFilePath.StartsWith(location.SourcePath)))
         {
            return fullFilePath.Remove(0, location.SourcePath.Length);
         }
         return string.Empty;
      }

      // removes a path from root lookup
      public void RemoveFromLookup(string filename)
      {
         cachedRootPathsSystemInfo.Remove(filename);
      }

      public void RemoveAllTargetDirsFromLookup(string removeDirSource)
      {
         cachedRootPathsSystemInfo.RemoveAllTargetDirsFromLookup(removeDirSource);
      }

      #region DLL Imports

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion DLL Imports

   }
}