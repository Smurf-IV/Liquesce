#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Roots.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Smurf-IV
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LiquesceFacade;
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
      public static readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      private readonly CacheHelper<string, NativeFileOps> cachedRootPathsSystemInfo;

      private readonly ConfigDetails configDetails;

      // constructor
      public Roots(ConfigDetails configDetailsTemp)
      {
         configDetails = configDetailsTemp;
         // NTFS is case-preserving but case-insensitive in the Win32 namespace
         cachedRootPathsSystemInfo = new CacheHelper<string, NativeFileOps>(configDetails.CacheLifetimeSeconds, true, StringComparer.OrdinalIgnoreCase);
      }


      public NativeFileOps GetPath(string filename, ulong spaceRequired = 0)
      {
         NativeFileOps fsi = null;
         try
         {
            // Even A directory requires 4K to be created !
            spaceRequired += 16384;
            Log.Trace("GetPath [{0}] spaceRequired [{1}]", filename, spaceRequired);
            if (string.IsNullOrWhiteSpace(filename))
            {
               Log.Trace("Win 7 (x64) sometimes passes in a blank");
               filename = PathDirectorySeparatorChar;
            }

            if (cachedRootPathsSystemInfo.TryGetValue(filename, out fsi))
            {
               Log.Trace("Found in cache");
               return fsi;
            }
            string foundPath = FindAllocationRootPath(filename, spaceRequired);
            if (string.IsNullOrEmpty(foundPath))
            {
               int ti = 4;
            }
            if (filename == PathDirectorySeparatorChar)
            {
               Log.Trace("Assuming Home directory so add new to cache and return");
               fsi = new NativeFileOps(foundPath);
               return fsi;
            }

            string searchFilename = filename.Trim(Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(filename))
               throw new ArgumentNullException(filename, "Not allowed to pass this length 2");

            if (!CheckAndGetType(Path.Combine(foundPath, searchFilename), out fsi))
            {
               if (configDetails.SourceLocations.Select(sourceLocation => Path.Combine(sourceLocation, searchFilename)).Any(newTarget => CheckAndGetType(newTarget, out fsi)))
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
            fsi = new NativeFileOps(Path.Combine(foundPath, searchFilename));
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            if (fsi != null)
            {
               Log.Debug("GetPath from [{0}] found [{1}]", filename, fsi.FullName);
               cachedRootPathsSystemInfo[filename] = fsi;
            }
            else
            {
               Log.Debug("GetPath found nothing for [{0}].", filename);
            }
         }
         return fsi;
      }

      /// <summary>
      /// Takes the SMB offset name and attempt to match against a knwon location.
      /// This is indended to be used for the MoveFile workaround
      /// </summary>
      /// <param name="searchOffsetFilename"></param>
      /// <param name="spaceRequired"></param>
      /// <returns>will return string.Empty if not found</returns>
      public NativeFileOps GetPathRelatedtoShare(string searchOffsetFilename, ulong spaceRequired = 0)
      {
         NativeFileOps fsi = null;
         try
         {
            // Even A directory requires 4K to be created !
            spaceRequired += 16384;

            if (configDetails.KnownSharePaths != null)
            {
               string foundPath = FindAllocationRootPath(PathDirectorySeparatorChar, spaceRequired);

// ReSharper disable LoopCanBeConvertedToQuery
               // Do not allow Resharper to mangle this one, as it gets the variable scoping wrong !
               foreach (string sharePath in configDetails.KnownSharePaths)
// ReSharper restore LoopCanBeConvertedToQuery
               {
                  string newPath = Path.GetFullPath(Path.Combine(sharePath, searchOffsetFilename));
                  string relPath = newPath.Replace(Path.GetPathRoot(newPath), String.Empty);
                  if (!CheckAndGetType(Path.Combine(foundPath, relPath), out fsi))
                  {
                     // if (configDetails.SourceLocations != null)
                     if (configDetails.SourceLocations.Select(sourceLocation => Path.Combine(sourceLocation, relPath)).
                           Any(newTarget => CheckAndGetType(newTarget, out fsi)))
                     {
                        Log.Trace("Found in source list");
                        return fsi;
                     }
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
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
         //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
         // this has been running, So this ought to try and find if it exists that way.
         fsi = new NativeFileOps(newTarget);
         return fsi.Exists;
      }


      public string[] GetAllPaths(string relativefolder)
      {
         return configDetails.SourceLocations.Select(t => t + relativefolder).Where(Directory.Exists).ToArray();
      }

      // *** NTh Change ***
      // Get the paths of all the copies of the file
      public string[] GetAllFilePaths(string file_name)
      {
         return configDetails.SourceLocations.Select(t => t + file_name).Where(File.Exists).ToArray();
      }


      public string GetRoot(string path)
      {
         if (!string.IsNullOrEmpty(path))
            foreach (string t in configDetails.SourceLocations.Where(path.Contains))
            {
               return t;
            }

         return string.Empty;
      }


      // return the path from a inputpath seen relative from the root
      public string GetRelative(string path)
      {
         return path.Replace(GetRoot(path), string.Empty);
      }



      // this method returns a path (real physical path) of a place where the next folder/file root can be.
      private string FindAllocationRootPath(string relativeFolder, ulong spaceRequired)
      {
         string foundRoot;
         switch (configDetails.AllocationMode)
         {
            case ConfigDetails.AllocationModes.folder:
               foundRoot = GetSourceThatMatchesThisFolderWithSpace(relativeFolder, spaceRequired);
               if (string.IsNullOrEmpty(foundRoot))
                  goto case ConfigDetails.AllocationModes.priority;
               break;

            case ConfigDetails.AllocationModes.priority:
               foundRoot = GetHighestPrioritySourceWithSpace(spaceRequired);
               if (string.IsNullOrEmpty(foundRoot))
                  goto case ConfigDetails.AllocationModes.balanced;
               break;

            case ConfigDetails.AllocationModes.balanced:
               foundRoot = GetSourceWithMostFreeSpace(spaceRequired);
               break;

            default:
               foundRoot = GetSourceWithMostFreeSpace(spaceRequired);
               break;
         }
         return foundRoot;
      }


      // returns the root for:
      //  1. the first disk where relativeFolder exists and there is enough free space
      //  2. priority mode
      private string GetSourceThatMatchesThisFolderWithSpace(string relativeFolder, ulong spaceRequired)
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
               if (lpFreeBytesAvailable > spaceRequired)
               {
                  string testpath = t + relativeFolder;

                  // check if relativeFolder is on this disk
                  if (new NativeFileOps(testpath).Exists)
                     return t;
               }
            }
         }
         return string.Empty;
      }


      // returns the next root with the highest priority
      private string GetHighestPrioritySourceWithSpace(ulong spaceRequired)
      {
         ulong lpFreeBytesAvailable = 0, num2, num3;
         foreach (string t in from t in configDetails.SourceLocations
                              where GetDiskFreeSpaceEx(t, out lpFreeBytesAvailable, out num2, out num3)
                              where lpFreeBytesAvailable > spaceRequired
                              select t)
         {
            return t;
         }
         return string.Empty;
      }


      // returns the root with the most free space
      private string GetSourceWithMostFreeSpace(ulong spaceRequired)
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
         if (highestFreeSpace < spaceRequired)
            Log.Warn("Amount of free space[{0}] on [{1}] is less than required [{2}]", highestFreeSpace, sourceWithMostFreeSpace, spaceRequired);
         return sourceWithMostFreeSpace;
      }

      public bool RelativeFileExists(string relative)
      {
         return configDetails.SourceLocations.Any(t => new NativeFileOps(t + relative).Exists);
      }

      // adds the root path to cachedRootPathsSystemInfo dicionary for a specific file
      public string TrimAndAddUnique(NativeFileOps fsi)
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
         return index >= 0 ? fullFilePath.Remove(0, configDetails.SourceLocations[index].Length) : string.Empty;
      }

      public string ReturnMountFileName(string actualFilename)
      {
         return string.Concat(configDetails.DriveLetter, ":", findOffsetPath(actualFilename));
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

      public void DeleteDirectory(string dirName)
      {
         foreach (string path in GetAllPaths(dirName))
         {
            Log.Trace("Deleting matched dir [{0}]", path);
            NativeFileOps.DeleteDirectory(path);
         }
         RemoveFromLookup(dirName);
      }

      public void DeleteFile(string filename)
      {
         Log.Trace("DeleteFile - Get all copies of the same file in other sources and delete them");
         foreach (string path in GetAllFilePaths(filename))
         {
            Log.Trace("Deleting file [{0}]", path);
            NativeFileOps.DeleteFile(path);
         }
         RemoveFromLookup(filename);
      }

   }
}
