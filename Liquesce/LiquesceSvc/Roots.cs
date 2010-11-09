using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using LiquesceFacade;
using DokanNet;
using NLog;

namespace LiquesceSvc
{
    // this class delivers the current physical root of the disk which should be used next
    // for file/folder creation.
    class Roots
    {
        public const string NO_PATH_TO_FILTER = "?";
        public const string HIDDEN_MIRROR_FOLDER = ".mirror";
        public const string HIDDEN_BACKUP_FOLDER = ".backup";

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

            if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.backup && Roots.IsBackup(filename))
            {
                string originalrelative = Roots.FilterDirFromPath(filename, Roots.HIDDEN_BACKUP_FOLDER);
                string originalpath = GetPath(originalrelative);
                // if folder backup found in the original directory then stop this!
                if (Directory.Exists(originalpath))
                {
                    foundPath = GetNewRoot(GetRoot(originalpath));
                }
                else
                {
                    foundPath = GetNewRoot();
                }
            }
            else
                foundPath = GetNewRoot();


            try
            {
                if (!String.IsNullOrWhiteSpace(filename) // Win 7 (x64) passes in a blank
                   && (filename != this.PathDirectorySeparatorChar)
                   )
                {
                    using ( rootPathsSync.UpgradableReadLock() )
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

                            if ( !isAShare && (configDetails.SourceLocations != null) )
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
                                    int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);

                                    // new file in folder mode
                                    if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.folder &&
                                        lastDir > -1)
                                    {
                                        Log.Trace("Perform search for path: {0}", filename);
                                        string newPart = filename.Substring(lastDir);
                                        foundPath = GetPath(filename.Substring(0, lastDir), false) + newPart;
                                        Log.Trace("Now make sure it can be found when it tries to repopen via the share");
                                        TrimAndAddUnique(foundPath);
                                    }

                                    // new file in .backup mode and it is a backup!
                                    else if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.backup && 
                                        Roots.IsBackup(filename))
                                    {
                                        Log.Trace("Seems that we got a backup relative path to create [{0}]", filename);

                                        string originalrelative = Roots.FilterDirFromPath(filename, Roots.HIDDEN_BACKUP_FOLDER);
                                        string originalpath = GetPath(originalrelative);
                                        // if folder backup found in the original directory then stop this!
                                        if (Directory.Exists(originalpath) || File.Exists(originalpath))
                                        {
                                            foundPath = GetNewRoot(GetRoot(originalpath)) + filename;
                                            Log.Trace("Seems that we got a backup path [{0}]", foundPath);
                                        }
                                        else
                                        {
                                            // strict backup
                                            //foundPath = "";

                                            foundPath = GetNewRoot() + filename;
                                        }
                                    }

                                    // new file in other modes priority, balanced, .backup mode
                                    else
                                    {
                                        foundPath = GetNewRoot() + filename;
                                    }
                                }
                                else
                                    foundPath = ""; // This is used when not creating new directory / file
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



        public static string GetRoot(string path)
        {
            for (int i = 0; i < configDetails.SourceLocations.Count; i++)
            {
                if (path.Contains(configDetails.SourceLocations[i]))
                {
                    return configDetails.SourceLocations[i];
                }
            }

            return "";
        }



        // this method returns a path (real physical path) of a place where the next folder/file root can be.
        public static string GetNewRoot(string relativeFolder = "")
        {
            //if (Log.IsTraceEnabled == true)
            //{
            //    LogToString();
            //}

            return GetNewRoot(NO_PATH_TO_FILTER, relativeFolder);
        }



        // return the path from a inputpath seen relative from the root
        public static string GetRelative(string path)
        {
            return path.Replace(GetRoot(path),"");
        }



        // this method returns a path (real physical path) of a place where the next folder/file root can be.
        // FilterThisPath can be used to not use a specific location (for mirror feature)
        public static string GetNewRoot(string FilterThisPath, string relativeFolder = "")
        {
            switch (configDetails.eAllocationMode)
            {
                case ConfigDetails.AllocationModes.folder:
                    return GetFromFolder(relativeFolder, FilterThisPath);

                case ConfigDetails.AllocationModes.priority:
                    return GetHighestPriority(FilterThisPath, 0);

                case ConfigDetails.AllocationModes.balanced:
                    return GetWithMostFreeSpace(FilterThisPath, 0);

                case ConfigDetails.AllocationModes.mirror:
                    return GetWithMostFreeSpace(FilterThisPath, 0);

                case ConfigDetails.AllocationModes.backup:
                    return GetWithMostFreeSpace(FilterThisPath, 0);

                default:
                    return GetHighestPriority(FilterThisPath, 0);
            }
        }



        // this method returns a path (real physical path) of a place where the next folder/file root can be.
        // filesize should be the size of the file which one wans to create on the disk
        public static string GetNewRoot(UInt64 filesize, string relativeFolder = "")
        {
            switch (configDetails.eAllocationMode)
            {
                case ConfigDetails.AllocationModes.folder:
                    return GetFromFolder(relativeFolder, NO_PATH_TO_FILTER);

                case ConfigDetails.AllocationModes.priority:
                    return GetHighestPriority(NO_PATH_TO_FILTER, filesize);

                case ConfigDetails.AllocationModes.balanced:
                    return GetWithMostFreeSpace(NO_PATH_TO_FILTER, filesize);

                case ConfigDetails.AllocationModes.mirror:
                    return GetWithMostFreeSpace(NO_PATH_TO_FILTER, filesize);

                case ConfigDetails.AllocationModes.backup:
                    return GetWithMostFreeSpace(NO_PATH_TO_FILTER, filesize);

                default:
                    return GetHighestPriority(NO_PATH_TO_FILTER, filesize);
            }
        }


        // returns the root for:
        //  1. the first disk where relativeFolder exists and there is enough free space
        //  2. priority mode
        private static string GetFromFolder(string relativeFolder, string FilterThisPath)
        {
            // if no disk with enough free space and an existing relativeFolder
            // then fall back to priority mode
            string rootForPriority = "";

            // for every source location
            for (int i = 0; i < configDetails.SourceLocations.Count; i++)
            {
                if (!configDetails.SourceLocations[i].Contains(FilterThisPath))
                {
                    // first get free space
                    ulong num;
                    ulong num2;
                    ulong num3;
                    if (GetDiskFreeSpaceEx(configDetails.SourceLocations[i], out num, out num2, out num3))
                    {
                        // see if enough space
                        if (num > configDetails.HoldOffBufferBytes)
                        {
                            string testpath = configDetails.SourceLocations[i] + relativeFolder;

                            // check if relativeFolder is on this disk
                            if (Directory.Exists(testpath))
                                return configDetails.SourceLocations[i];

                            // mark as highest priority if first disk
                            if (rootForPriority.Equals(""))
                                rootForPriority = configDetails.SourceLocations[i];
                        }
                    }
                }
            }

            return rootForPriority;
        }


        // returns the next root with the highest priority
        private static string GetHighestPriority(string FilterThisPath, UInt64 filesize)
        {
            for (int i = 0; i < configDetails.SourceLocations.Count; i++)
            {
                // if the path shouldn't be filtered or there is no filter
                if (! configDetails.SourceLocations[i].Contains(FilterThisPath))
                {
                    ulong num;
                    ulong num2;
                    ulong num3;
                    if (GetDiskFreeSpaceEx(configDetails.SourceLocations[i], out num, out num2, out num3))
                    {
                        if (num > configDetails.HoldOffBufferBytes && num > filesize)
                        {
                            return configDetails.SourceLocations[i];
                        }
                    }
                }
            }

            // if all drives are full (exepting the HoldOffBuffers) choose that one with the most free space
            return GetWithMostFreeSpace(FilterThisPath, filesize);
        }



        // returns the root with the most free space
        private static string GetWithMostFreeSpace(string FilterThisPath, UInt64 filesize)
        {
            ulong HighestFreeSpace = 0;
            string PathWithMostFreeSpace = "";

            configDetails.SourceLocations.ForEach(str =>
            {
                // if the path shouldn't be filtered or there is no filter
                if (! str.Contains(FilterThisPath))
                {
                    ulong num;
                    ulong num2;
                    ulong num3;
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
            if (HighestFreeSpace > filesize)
                return PathWithMostFreeSpace;
            else
                return "";
        }



        public static bool IsBackup(string path)
        {
            if (path.Contains(HIDDEN_BACKUP_FOLDER))
                return true;
            else
                return false;
        }


        public static string FilterDirFromPath(string path, string filterdir)
        {
            return path.Replace("\\" + filterdir, "");
        }



        // for debugging to print all disks and it's availabel space
        private static void LogToString()
        {
            Log.Trace("Printing all disks:");
            for (int i = 0; i < configDetails.SourceLocations.Count; i++)
            {
                ulong num;
                ulong num2;
                ulong num3;
                if (GetDiskFreeSpaceEx(configDetails.SourceLocations[i], out num, out num2, out num3))
                {
                    Log.Trace("root[{0}], space[{1}]", configDetails.SourceLocations[i], num);
               }
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
                    using (rootPathsSync.WriteLock() )
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
                    using( rootPathsSync.WriteLock() )
                    rootPaths.Remove(key);
                }
            }
        }



        // removes a path from root lookup
        public void RemoveFromLookup(string filename)
        {
            using (rootPathsSync.WriteLock() )
                rootPaths.Remove(filename);
        }



        #region DLL Imports
        /// <summary>
        /// The CreateFile function creates or opens a file, file stream, directory, physical disk, volume, console buffer, tape drive,
        /// communications resource, mailslot, or named pipe. The function returns a handle that can be used to access an object.
        /// </summary>
        /// <param name="lpFileName"></param>
        /// <param name="dwDesiredAccess"> access to the object, which can be read, write, or both</param>
        /// <param name="dwShareMode">The sharing mode of an object, which can be read, write, both, or none</param>
        /// <param name="SecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether or not the returned handle can
        /// be inherited by child processes. Can be null</param>
        /// <param name="dwCreationDisposition">An action to take on files that exist and do not exist</param>
        /// <param name="dwFlagsAndAttributes">The file attributes and flags. </param>
        /// <param name="hTemplateFile">A handle to a template file with the GENERIC_READ access right. The template file supplies file attributes
        /// and extended attributes for the file that is being created. This parameter can be null</param>
        /// <returns>If the function succeeds, the return value is an open handle to a specified file. If a specified file exists before the function
        /// all and dwCreationDisposition is CREATE_ALWAYS or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function
        /// succeeds. If a file does not exist before the call, GetLastError returns 0 (zero).
        /// If the function fails, the return value is INVALID_HANDLE_VALUE. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        #endregion
    }
}
