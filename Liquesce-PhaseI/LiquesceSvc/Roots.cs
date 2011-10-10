#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Roots.cs" company="Smurf-IV">
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
using System.Threading;
using System.Runtime.InteropServices;
using LiquesceFacade;
using NLog;

namespace LiquesceSvc
{
   // this class delivers the current physical root of the disk which should be used next
   // for file/folder creation.
   class Roots
   {
      private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      private readonly CacheHelper<string, FileSystemInfo> cachedRootPathsSystemInfo;

      private readonly Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
      private readonly ReaderWriterLockSlim foundDirectoriesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


      private static ConfigDetails configDetails;

      // constructor
      public Roots(ConfigDetails configDetailsTemp)
      {
         configDetails = configDetailsTemp;
         cachedRootPathsSystemInfo = new CacheHelper<string, FileSystemInfo>(configDetails.CacheLifetimeSeconds);
      }


      public FileSystemInfo GetPath(string filename, bool isCreate = false)
      {
         FileSystemInfo fsi = null;
         try
         {
            if (String.IsNullOrWhiteSpace(filename))
            {
               Log.Trace("Win 7 (x64) sometimes passes in a blank");
               filename = PathDirectorySeparatorChar;
            }

            if (cachedRootPathsSystemInfo.TryGetValue(filename, out fsi))
            {
               Log.Trace("Found in cache");
               return fsi;
            }
            string foundPath = FindAllocationRootPath(filename);
            if (filename == PathDirectorySeparatorChar)
            {
               Log.Trace("Assuming Home directory so add new to cache and return");
               fsi = new DirectoryInfo(foundPath);
               cachedRootPathsSystemInfo[filename] = fsi;
               return fsi;
            }

            string searchFilename = filename.Trim(Path.DirectorySeparatorChar);
            if (String.IsNullOrWhiteSpace(filename))
               throw new ArgumentNullException(filename, "Not allowed to pass this length 2");

            if (!CheckAndGetType(filename, Path.Combine(foundPath, searchFilename), out fsi))
            {
               // if (configDetails.SourceLocations != null)
               if (configDetails.SourceLocations.Select(sourceLocation => Path.Combine(sourceLocation, searchFilename)).Any(newTarget => CheckAndGetType(filename, newTarget, out fsi)))
               {
                  Log.Trace("Found in source list");
                  return fsi;
               }
            }
            else
            {
               Log.Trace("Not found in 1st mode source");
               return fsi;
            }
            if (configDetails.KnownSharePaths != null)
            {
               foreach (string sharePath in configDetails.KnownSharePaths)
               {
                  string newPath = Path.Combine(sharePath, searchFilename);
                  string relPath = newPath.Replace(Path.GetPathRoot(newPath), String.Empty);
                  if (!CheckAndGetType(newPath, Path.Combine(foundPath, relPath), out fsi))
                  {
                     // if (configDetails.SourceLocations != null)
                     if ( configDetails.SourceLocations.Select(sourceLocation => Path.Combine(sourceLocation, relPath)).
                           Any(newTarget => CheckAndGetType(newPath, newTarget, out fsi)))
                     {
                        Log.Trace("Found in source list");
                        return fsi;
                     }
                  }
                  else
                  {
                     return fsi;
                  }
               }
            }

            //-------------------------------------
            Log.Trace("file/folder not found");
            // So create a holder for the return and do not store
            fsi = new FileInfo(Path.Combine(foundPath, searchFilename));
            cachedRootPathsSystemInfo[filename] = fsi;
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            if (fsi != null)
               Log.Debug("GetPath from [{0}] found [{1}]", filename, fsi.FullName);
            else
            {
               Log.Debug("GetPath found nothing for [{0}]. isCreate=[{1}]", filename, isCreate);
            }
         }
         return fsi;
      }

      private bool CheckAndGetType(string filename, string newTarget, out FileSystemInfo fsi)
      {
         if (cachedRootPathsSystemInfo.TryGetValue(newTarget, out fsi))
         {
            Log.Trace("Found in check cache");
            return true;
         }
         Log.Trace("Try and GetPath from [{0}]", newTarget);
         //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
         // this has been running, So this ought to try and find if it exists that way.
         fsi = new FileInfo(newTarget);
         if (!fsi.Exists)
            fsi = new DirectoryInfo(newTarget);
         if (fsi.Exists)
         {
            cachedRootPathsSystemInfo[filename] = fsi;
            return true;
         }
         return false;
      }


      public static string[] GetAllPaths(string relativefolder)
      {
         return configDetails.SourceLocations.Select(t => t + relativefolder).Where(Directory.Exists).ToArray();
      }

      // *** NTh Change ***
      // Get the paths of all the copies of the file
      public static string[] GetAllFilePaths(string file_name)
      {
         return configDetails.SourceLocations.Select(t => t + file_name).Where(File.Exists).ToArray();
      }


      public static string GetRoot(string path)
      {
         if (!String.IsNullOrEmpty(path))
            foreach (string t in configDetails.SourceLocations.Where(path.Contains))
            {
               return t;
            }

         return String.Empty;
      }


      // return the path from a inputpath seen relative from the root
      public static string GetRelative(string path)
      {
         return path.Replace(GetRoot(path), String.Empty);
      }



      // this method returns a path (real physical path) of a place where the next folder/file root can be.
      private static string FindAllocationRootPath(string relativeFolder)
      {
         string foundRoot;
         switch (configDetails.AllocationMode)
         {
            case ConfigDetails.AllocationModes.folder:
               foundRoot = GetSourceThatMatchesThisFolderWithSpace(relativeFolder);
               if (string.IsNullOrEmpty(foundRoot))
                  goto case ConfigDetails.AllocationModes.priority;
               break;

            case ConfigDetails.AllocationModes.priority:
               foundRoot = GetHighestPrioritySourceWithSpace();
               if (string.IsNullOrEmpty(foundRoot))
                  goto case ConfigDetails.AllocationModes.balanced;
               break;

            case ConfigDetails.AllocationModes.balanced:
               foundRoot = GetSourceWithMostFreeSpace();
               break;

            default:
               foundRoot = GetSourceWithMostFreeSpace();
               break;
         }
         return foundRoot;
      }


      // returns the root for:
      //  1. the first disk where relativeFolder exists and there is enough free space
      //  2. priority mode
      private static string GetSourceThatMatchesThisFolderWithSpace(string relativeFolder)
      {
         // remove the last \ to delete the last directory
         relativeFolder = relativeFolder.TrimEnd(new char[] { Path.DirectorySeparatorChar });

         // for every source location
         foreach (string t in configDetails.SourceLocations)
         {
            // first get free space
            ulong lpFreeBytesAvailable, num2, num3;
            if (GetDiskFreeSpaceEx(t, out lpFreeBytesAvailable, out num2, out num3))
            {
               // see if enough space
               if (lpFreeBytesAvailable > configDetails.HoldOffBufferBytes)
               {
                  string testpath = t + relativeFolder;

                  // check if relativeFolder is on this disk
                  if (Directory.Exists(testpath))
                     return t;
               }
            }
         }
         return string.Empty;
      }


      // returns the next root with the highest priority
      private static string GetHighestPrioritySourceWithSpace()
      {
         ulong lpFreeBytesAvailable = 0, num2, num3;
         foreach (string t in from t in configDetails.SourceLocations
                              where GetDiskFreeSpaceEx(t, out lpFreeBytesAvailable, out num2, out num3)
                              where lpFreeBytesAvailable > configDetails.HoldOffBufferBytes
                              select t)
         {
            return t;
         }
         return string.Empty;
      }


      // returns the root with the most free space
      private static string GetSourceWithMostFreeSpace()
      {
         ulong highestFreeSpace = 0;
         string sourceWithMostFreeSpace = string.Empty;

         configDetails.SourceLocations.ForEach(str =>
         {
            ulong num, num2, num3;
            if (GetDiskFreeSpaceEx(str, out num, out num2, out num3))
            {
               if (highestFreeSpace < num)
               {
                  highestFreeSpace = num;
                  sourceWithMostFreeSpace = str;
               }
            }
         });

         // return the largest regardless
         return sourceWithMostFreeSpace;
      }

      public static bool RelativeFileExists(string relative)
      {
         return configDetails.SourceLocations.Any(t => File.Exists(t + relative));
      }


      public static bool RelativeFolderExists(string relative)
      {
         return configDetails.SourceLocations.Any(t => Directory.Exists(t + relative));
      }


      public static string FilterDirFromPath(string path, string filterdir)
      {
         return path.Replace("\\" + filterdir, String.Empty);
      }



      // for debugging to print all disks and it's availabel space
      private static void LogToString()
      {
         Log.Trace("Printing all disks:");
         ulong num = 0, num2, num3;
         foreach (string t in
            configDetails.SourceLocations.Where(t => GetDiskFreeSpaceEx(t, out num, out num2, out num3)))
         {
            Log.Trace("root[{0}], space[{1}]", t, num);
         }
      }



      // adds the root path to cachedRootPathsSystemInfo dicionary for a specific file
      public string TrimAndAddUnique(FileSystemInfo fsi)
      {
         string fullFilePath = fsi.FullName;
         string key = findOffsetPath(fullFilePath);
         if (!string.IsNullOrEmpty(key))
         {
            Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
            cachedRootPathsSystemInfo[key] = fsi;
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }

      private string findOffsetPath(string fullFilePath)
      {
         int index = configDetails.SourceLocations.FindIndex(fullFilePath.StartsWith);
         if (index >= 0)
         {
            return fullFilePath.Remove(0, configDetails.SourceLocations[index].Length);
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

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion

      public void DeleteDirectory(string filename)
      {
         using (foundDirectoriesSync.UpgradableReadLock())
         {
            // Only delete the directories that this knew about before the delete was called 
            // (As the user may be moving files into the sources from the mount !!)
            List<string> targetDeletes = foundDirectories[filename];
            if (targetDeletes != null)
               for (int index = 0; index < targetDeletes.Count; index++)
               {
                  // Use an index for speed (It all counts !)
                  string fullPath = targetDeletes[index];
                  Log.Trace("Deleting matched dir [{0}]", fullPath);
                  Directory.Delete(fullPath, false);
               }
            using (foundDirectoriesSync.WriteLock())
               foundDirectories.Remove(filename);
            RemoveFromLookup(filename);
         }
      }

      public void DeleteFile(string filename)
      {
         // *** NTh Change ***
         // Get all copies of the same file in other sources and delete them
         Log.Trace("DeleteOnClose File");
         foreach (string path in GetAllFilePaths(filename))
         {
            File.Delete(path);
         }
         RemoveFromLookup(filename);
      }
   }
}
