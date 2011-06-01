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
      public const string NO_PATH_TO_FILTER = "?";
      public const string HIDDEN_MIRROR_FOLDER = "_mirror";
      public const string HIDDEN_BACKUP_FOLDER = "_backup";

      private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
      static private readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      static private readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();


      private static ConfigDetails configDetails;

      // constructor
      public Roots(ConfigDetails configDetailsTemp)
      {
         configDetails = configDetailsTemp;
      }


      public string GetPath(string filename, bool isCreate = false)
      {
         string foundPath;

         if ((configDetails.AllocationMode == ConfigDetails.AllocationModes.backup)
            && IsBackup(filename)
            )
         {
            string originalrelative = FilterDirFromPath(filename, HIDDEN_BACKUP_FOLDER);
            string originalpath = GetPath(originalrelative);
            // if folder backup found in the original directory then stop this!
            foundPath = GetNewRoot(Directory.Exists(originalpath) ? GetRoot(originalpath) : NO_PATH_TO_FILTER, 0, filename);
         }
         else
            foundPath = GetNewRoot(NO_PATH_TO_FILTER, 0, filename);


         try
         {
            if (!String.IsNullOrWhiteSpace(filename) // Win 7 (x64) passes in a blank
               && (filename != PathDirectorySeparatorChar)
               )
            {
               using (rootPathsSync.UpgradableReadLock())
               {
                  bool isAShare = false;
                  if (!filename.StartsWith(PathDirectorySeparatorChar))
                  {
                     isAShare = true;
                     filename = Path.DirectorySeparatorChar + filename;
                  }
                  if (filename.EndsWith(PathDirectorySeparatorChar))
                     isAShare = true;

                  filename = filename.TrimEnd(Path.DirectorySeparatorChar);

                  if (!rootPaths.TryGetValue(filename, out foundPath))
                  {
                     bool found = false;
                     if (String.IsNullOrWhiteSpace(filename))
                        throw new ArgumentNullException(filename, "Not allowed to pass this length 2");
                     if (filename[0] != Path.DirectorySeparatorChar)
                        filename = PathDirectorySeparatorChar + filename;

                     if (!isAShare && (configDetails.SourceLocations != null))
                     {
                        foreach (string newTarget in
                           configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
                        {
                           Log.Trace("Try and GetPath from [{0}]", newTarget);
                           //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
                           // this has been running, So this ought to try and find if it exists that way.
                           if (Directory.Exists(newTarget) || File.Exists(newTarget))
                           {
                              TrimAndAddUnique(newTarget);
                              found = rootPaths.TryGetValue(filename, out foundPath);
                              break;
                           }
                        }
                     }
                     else if (isAShare && (configDetails.KnownSharePaths != null))
                     {
                        found = configDetails.KnownSharePaths.Exists(delegate(string sharePath)
                        {
                           Log.Trace("Try and find from [{0}][{1}]", sharePath, filename);
                           return rootPaths.TryGetValue(sharePath + filename, out foundPath);
                        });
                     }

                     //-------------------------------------
                     // file/folder not found!!!
                     // let's see if we should create a new one...
                     if (!found)
                     {
                        //Log.Trace("was this a failed redirect thing from a network share ? [{0}]", filename);
                        if (isCreate)
                        {
                           // new file in folder mode
                           if (configDetails.AllocationMode == ConfigDetails.AllocationModes.folder)
                           {
                              Log.Trace("Perform search for path: {0}", filename);
                              foundPath = GetNewRoot(NO_PATH_TO_FILTER, configDetails.HoldOffBufferBytes, filename) + filename;
                              Log.Trace("Now make sure it can be found when it tries to reopen via the share");
                              TrimAndAddUnique(foundPath);
                           }

                           // new file in .backup mode and it is a backup!
                           else if ((configDetails.AllocationMode == ConfigDetails.AllocationModes.backup)
                              && IsBackup(filename)
                              )
                           {
                              Log.Trace("Seems that we got a backup relative path to create [{0}]", filename);

                              string originalrelative = FilterDirFromPath(filename, HIDDEN_BACKUP_FOLDER);
                              string originalpath = GetPath(originalrelative);
                              // if folder backup found in the original directory then stop this!
                              if (Directory.Exists(originalpath) || File.Exists(originalpath))
                              {
                                 foundPath = GetNewRoot(GetRoot(originalpath), 0, filename) + filename;
                                 Log.Trace("Seems that we got a backup path [{0}]", foundPath);
                              }
                              else
                              {
                                 // strict backup
                                 //foundPath = String.Empty;

                                 foundPath = GetNewRoot(NO_PATH_TO_FILTER, 0, filename) + filename;
                              }
                           }

                           // new file in other modes priority, balanced, .backup mode
                           else
                           {
                              foundPath = GetNewRoot(NO_PATH_TO_FILTER, 0, filename) + filename;
                           }
                           // Need to solve the issue with Synctoy performing moves into unknown / unused space
                           // MoveFile pathSource: [F:\_backup\Kylie Minogue\FSP01FA0CF932F74BF5AE5C217F4AE6626B.tmp] pathTarget: [G:\_backup\Kylie Minogue\(2010) Aphrodite\12 - Can't Beat The Feeling.mp3] 
                           // MoveFile threw:  System.IO.DirectoryNotFoundException: The system cannot find the path specified. (Exception from HRESULT: 0x80070003)
                           string newDir = Path.GetDirectoryName(foundPath);
                           if (!String.IsNullOrWhiteSpace(newDir)
                              && !Directory.Exists(newDir)
                              )
                           {
                              Log.Info("Creating base directory for the Move Target [{0}]", newDir);
                              Directory.CreateDirectory(newDir);
                           }
                        }
                        else
                           foundPath = String.Empty; // This is used when not creating new directory / file
                     }
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetPath threw: ", ex);
         }
         finally
         {
            if (!String.IsNullOrWhiteSpace(foundPath))
               Log.Debug("GetPath from [{0}] found [{1}]", filename, foundPath);
            else
               Log.Debug("GetPath found nothing for [{0}]. isCreate=[{1}]", filename, isCreate);
         }
         return foundPath;
      }


      public List<string> GetAllPaths(string relativefolder)
      {
         List<string> paths = new List<string>();

         for (int i = 0; i < configDetails.SourceLocations.Count; i++)
         {
            string current = configDetails.SourceLocations[i] + relativefolder;
            if (Directory.Exists(current))
               paths.Add(current);
         }

         return paths;
      }

      // *** NTh Change ***
      // Get the paths of all the copies of the file
      public List<string> GetAllFilePaths(string file_name)
      {
         List<string> paths = new List<string>();

         for (int i = 0; i < configDetails.SourceLocations.Count; i++)
         {
            string current = configDetails.SourceLocations[i] + file_name;
            if (File.Exists(current))
               paths.Add(current);
         }

         return paths;
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
      // filesize should be the size of the file which one wans to create on the disk
      public static string GetNewRoot(string FilterThisPath, UInt64 filesize, string relativeFolder)
      {
         switch (configDetails.AllocationMode)
         {
            case ConfigDetails.AllocationModes.folder:
               return GetFromFolder(relativeFolder, FilterThisPath, filesize);

            case ConfigDetails.AllocationModes.priority:
               return GetHighestPriority(FilterThisPath, filesize);

            case ConfigDetails.AllocationModes.balanced:
               return GetWithMostFreeSpace(FilterThisPath, filesize);

            //case ConfigDetails.AllocationModes.mirror:
            //    return GetWithMostFreeSpace(FilterThisPath, filesize);

            case ConfigDetails.AllocationModes.backup:
               return GetWithMostFreeSpace(FilterThisPath, filesize);

            default:
               return GetHighestPriority(NO_PATH_TO_FILTER, filesize);
         }
      }


      // returns the root for:
      //  1. the first disk where relativeFolder exists and there is enough free space
      //  2. priority mode
      private static string GetFromFolder(string relativeFolder, string FilterThisPath, UInt64 filesize)
      {
         // if no disk with enough free space and an existing relativeFolder
         // then fall back to priority mode
         string rootForPriority = String.Empty;

         // remove the last \ to delete the last directory
         relativeFolder = relativeFolder.TrimEnd(new char[] { Path.DirectorySeparatorChar });

         // for every source location
         foreach (string t in configDetails.SourceLocations.Where(t => !t.Contains(FilterThisPath)))
         {
            // first get free space
            ulong num, num2, num3;
            if (GetDiskFreeSpaceEx(t, out num, out num2, out num3))
            {
               // see if enough space
               if (num > filesize)
               {
                  string testpath = t + relativeFolder;

                  // check if relativeFolder is on this disk
                  if (Directory.Exists(testpath))
                     return t;

                  // mark as highest priority if first disk
                  if (String.IsNullOrEmpty(rootForPriority))
                     rootForPriority = t;
               }
            }
         }
         return (String.IsNullOrEmpty(rootForPriority)) ? GetWithMostFreeSpace(FilterThisPath, 0) : rootForPriority;
      }


      // returns the next root with the highest priority
      private static string GetHighestPriority(string FilterThisPath, UInt64 filesize)
      {
         ulong num = 0, num2, num3;
         foreach (string t in from t in configDetails.SourceLocations
                              where !t.Contains(FilterThisPath)
                              where GetDiskFreeSpaceEx(t, out num, out num2, out num3)
                              where num > configDetails.HoldOffBufferBytes && num > filesize
                              select t)
         {
            return t;
         }

         // if all drives are full (exepting the HoldOffBuffers) choose that one with the most free space
         return GetWithMostFreeSpace(FilterThisPath, filesize);
      }


      // returns the root with the most free space
      private static string GetWithMostFreeSpace(string FilterThisPath, UInt64 filesize)
      {
         ulong HighestFreeSpace = 0;
         string PathWithMostFreeSpace = String.Empty;

         configDetails.SourceLocations.ForEach(str =>
         {
            // if the path shouldn't be filtered or there is no filter
            if (!str.Contains(FilterThisPath))
            {
               ulong num, num2, num3;
               if (GetDiskFreeSpaceEx(str, out num, out num2, out num3))
               {
                  if (HighestFreeSpace < num)
                  {
                     HighestFreeSpace = num;
                     PathWithMostFreeSpace = str;
                  }
               }
            }
         });

         // if the file fits on the disk
         return (HighestFreeSpace > filesize) ? PathWithMostFreeSpace : String.Empty;
      }



      public static bool IsBackup(string path)
      {
         return path.Contains(HIDDEN_BACKUP_FOLDER);
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



      // adds the root path to rootPaths dicionary for a specific file
      public string TrimAndAddUnique(string fullFilePath)
      {
         int index = configDetails.SourceLocations.FindIndex(fullFilePath.StartsWith);
         if (index >= 0)
         {
            string key = fullFilePath.Remove(0, configDetails.SourceLocations[index].Length);
            Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
            using (rootPathsSync.WriteLock())
            {
               // TODO: Add the collisions / duplicate feedback from here
               rootPaths[key] = fullFilePath;
            }
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }


      // removes a root from root lookup
      public void RemoveTargetFromLookup(string realFilename)
      {
         using (rootPathsSync.UpgradableReadLock())
         {
            string key = string.Empty;
            foreach (KeyValuePair<string, string> kvp in rootPaths.Where(kvp => kvp.Value == realFilename))
            {
               key = kvp.Key;
               break;
            }
            if (!String.IsNullOrEmpty(key))
            {
               RemoveFromLookup(key);
            }
         }
      }

      // removes a path from root lookup
      public void RemoveFromLookup(string filename)
      {
         using (rootPathsSync.WriteLock())
            rootPaths.Remove(filename);
      }



      #region DLL Imports

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion
   }
}
