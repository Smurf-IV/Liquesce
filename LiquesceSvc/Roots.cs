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
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      private readonly CacheHelper<string, NativeFileOps> cachedRootPathsSystemInfo;

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
         cachedRootPathsSystemInfo = new CacheHelper<string, NativeFileOps>(cacheLifetimeSeconds, true, StringComparer.OrdinalIgnoreCase);
      }


      public NativeFileOps GetPath(string filename, ulong spaceRequired = 0)
      {
         NativeFileOps fsi = null;
         bool isNamedStream = false;
         string namedStream = string.Empty;
         try
         {
            // Even A directory requires 4K to be created !
            spaceRequired += 16384;
            Log.Trace("GetPath [{0}] spaceRequired [{1}]", filename, spaceRequired);

            if (cachedRootPathsSystemInfo.TryGetValue(filename, out fsi))
            {
               Log.Trace("Found in cache");
               return fsi;
            }
            string[] splits = filename.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
            int offset = Path.IsPathRooted( filename )?2:1;
            if ( (splits != null)
               && (splits.Length > offset)
               )
            {
               isNamedStream = true;
               filename = splits[offset-1];
               namedStream = splits[offset];
            }
            string foundPath = FindAllocationRootPath(filename, spaceRequired);
            if (string.IsNullOrEmpty(foundPath))
            {
               // TODO: Should something have gone boom before this ?
// ReSharper disable UnusedVariable.Compiler
               int ti = 4;
// ReSharper restore UnusedVariable.Compiler
            }
            if (filename == PathDirectorySeparatorChar)
            {
               Log.Trace("Assuming Home directory so add new to cache and return");
               fsi = new NativeFileOps(foundPath, AreAllReadOnly);
               return fsi;
            }

            string searchFilename = filename.Trim(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(filename))
               throw new ArgumentNullException(filename, "Not allowed to pass this length 2");

            if (!CheckAndGetType(Path.Combine(foundPath, searchFilename), out fsi))
            {
               if (mountDetail.SourceLocations.Select(sourceLocation => Path.Combine(sourceLocation.SourcePath, searchFilename)).Any(newTarget => CheckAndGetType(newTarget, out fsi)))
               {
                  Log.Trace("Found in source list");
                  return fsi;
               }
            }
            else
            {
               Log.Trace("found in 1st mode source");
               return fsi;
            }

            //-------------------------------------
            Log.Trace("file/folder not found");
            // So create a holder for the return and do not store
            fsi = new NativeFileOps(Path.Combine(foundPath, searchFilename), AreAllReadOnly);
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            if ( (fsi != null)
               && fsi.Exists
               )
            {
               Log.Debug("GetPath from [{0}] found [{1}]", filename, fsi.FullName);
               cachedRootPathsSystemInfo[filename] = fsi;
               if (isNamedStream)
               {
                  Log.Warn("isNamedStream [{0}] found [{1}]", filename, namedStream);
                  cachedRootPathsSystemInfo[string.Format("{0}:{1}",filename, namedStream)] = fsi;
               }
            }
            else
            {
               Log.Debug("GetPath found nothing for [{0}].", filename);
            }
         }
         return fsi;
      }


      private bool CheckAndGetType(string newTarget, out NativeFileOps fsi)
      {
         if (cachedRootPathsSystemInfo.TryGetValue(newTarget, out fsi))
         {
            Log.Trace("Found in check cache");
            return true;
         }
         Log.Trace("Try and GetPath from [{0}]", newTarget);
         // Now here's a kicker.. The User might have copied a file directly onto one of the drives while
         // this has been running, So this ought to try and find if it exists that way.
         fsi = new NativeFileOps(newTarget, IsRootReadOnly(newTarget));
         return fsi.Exists;
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

      public IEnumerable<string> GetAllPaths(string relativefolder)
      {
         return mountDetail.SourceLocations.Select(t => t.SourcePath + relativefolder).Where(Directory.Exists);
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
         return path.Replace(GetRoot(path), string.Empty);
      }

      public string FindByFileId(long fileId)
      {
         foreach (string found in GetAllPaths(PathDirectorySeparatorChar)
                           .Select(rootToCheck => NfsSupport.GetByFileId(rootToCheck, fileId))
                           .Where(found => !string.IsNullOrEmpty(found))
                  )
         {
            return GetRelative(found);
         }
         return string.Empty;
      }

      // this method returns a path (real physical path) of a place where the next folder/file root can be.
      private string FindAllocationRootPath(string relativeFolder, ulong spaceRequired)
      {
         string foundRoot;
         switch (mountDetail.AllocationMode)
         {
            case MountDetail.AllocationModes.Folder:
               foundRoot = GetSourceThatMatchesThisFolderWithSpace(relativeFolder, spaceRequired);
               if (string.IsNullOrEmpty(foundRoot))
                  goto case MountDetail.AllocationModes.Priority;
               break;

            case MountDetail.AllocationModes.Priority:
               foundRoot = GetHighestPrioritySourceWithSpace(spaceRequired);
               if (string.IsNullOrEmpty(foundRoot))
                  goto case MountDetail.AllocationModes.Balanced;
               break;

            case MountDetail.AllocationModes.Balanced:
               foundRoot = GetSourceWithMostFreeSpace(spaceRequired);
               break;

            default:
               foundRoot = GetSourceWithMostFreeSpace(spaceRequired);
               break;
         }
         return foundRoot;
      }


      // returns the root for:
      //  The first disk where relativeFolder exists and if there is enough free space
      private string GetSourceThatMatchesThisFolderWithSpace(string relativeFolder, ulong spaceRequired)
      {
         Log.Trace("Trying GetSourceThatMatchesThisFolderWithSpace([{0}],[{1}])", relativeFolder, spaceRequired);
         // remove the last \ to delete the last directory
         relativeFolder = relativeFolder.TrimEnd(new char[] { Path.DirectorySeparatorChar });

         // for every source location
         foreach (string sourcePath in mountDetail.SourceLocations.Select(s => s.SourcePath))
         {
            // first get free space
            ulong lpFreeBytesAvailable, num2, num3;
            if (GetDiskFreeSpaceExW(sourcePath, out lpFreeBytesAvailable, out num2, out num3))
            {
               Log.Trace("See if enough space on [{0}] lpFreeBytesAvailable[{1}] > spaceRequired[{2}]", sourcePath,
                         lpFreeBytesAvailable, spaceRequired);
               if (lpFreeBytesAvailable > spaceRequired)
               {
                  string testpath = sourcePath + relativeFolder;

                  // check if relativeFolder is on this disk
                  if (new NativeFileOps(testpath, AreAllReadOnly).Exists)
                     return sourcePath;
               }
            }
         }
         return string.Empty;
      }


      // returns the next root with the highest priority and the space
      private string GetHighestPrioritySourceWithSpace(ulong spaceRequired)
      {
         Log.Trace("Trying GetHighestPrioritySourceWithSpace([{0}])", spaceRequired);
         foreach (SourceLocation w in mountDetail.SourceLocations)
         {
            ulong lpFreeBytesAvailable, num2, num3;
            if (GetDiskFreeSpaceExW(w.SourcePath, out lpFreeBytesAvailable, out num2, out num3) 
               && (lpFreeBytesAvailable > spaceRequired)
               )
            {
               return w.SourcePath;
            }
         }
         return string.Empty;
      }


      // returns the root with the most free space
      private string GetSourceWithMostFreeSpace(ulong spaceRequired)
      {
         Log.Trace("Trying GetSourceWithMostFreeSpace([{0}])", spaceRequired);
         ulong highestFreeSpace = 0;
         string sourceWithMostFreeSpace = string.Empty;

         mountDetail.SourceLocations.ForEach(str =>
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
         });
         if (highestFreeSpace < spaceRequired)
            Log.Warn("Amount of free space[{0}] on [{1}] is less than required [{2}]", highestFreeSpace, sourceWithMostFreeSpace, spaceRequired);
         return sourceWithMostFreeSpace;
      }

      private string findOffsetPath(string fullFilePath)
      {
         foreach (SourceLocation location in mountDetail.SourceLocations.Where(location => fullFilePath.StartsWith(location.SourcePath)))
         {
            return fullFilePath.Remove(0, location.SourcePath.Length);
         }
         return string.Empty;
      }

      // removes a root from root lookup
      public void RemoveTargetFromLookup(string realFilename)
      {
         string key = findOffsetPath(realFilename);
         if (string.IsNullOrEmpty(key))
         {
            RemoveFromLookup(key);
         }
      }

      // removes a path from root lookup
      public void RemoveFromLookup(string filename)
      {
         cachedRootPathsSystemInfo.Remove(filename);
      }


      #region DLL Imports

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion
   }
}
