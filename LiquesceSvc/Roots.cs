using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using LiquesceFaçade;
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

        private static readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // This would normally be static, but then there should only ever be one of these classes present from the Dokan Lib callback.
        private static readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();


        private static ConfigDetails configDetails;

        // constructor
        public Roots(ConfigDetails configDetailsTemp)
        {
            configDetails = configDetailsTemp;
        }


        public static string GetPath(string filename, bool isCreate = false, bool isMirror = false)
        {
            string foundPath = getNewRoot();
            try
            {
                if (!String.IsNullOrWhiteSpace(filename) // Win 7 (x64) passes in a blank
                   && (filename != PathDirectorySeparatorChar)
                   )
                {
                    try
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
                        rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
                        if (!rootPaths.TryGetValue(filename, out foundPath))
                        {
                            bool found = false;
                            if (String.IsNullOrWhiteSpace(filename))
                                throw new ArgumentNullException(filename, "Not allowed to pass this length 2");
                            if (filename[0] != Path.DirectorySeparatorChar)
                                filename = PathDirectorySeparatorChar + filename;

                            if (!isAShare
                                      && (configDetails.SourceLocations != null)
                                      )
                            {
                                foreach (string newTarget in
                                   configDetails.SourceLocations.Select(sourceLocation => sourceLocation + filename))
                                {
                                    Log.Trace("Try and GetPath from [{0}]", newTarget);
                                    //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
                                    // this has been running, So this ought to try and find if it exists that way.
                                    if (Directory.Exists(newTarget)
                                       || File.Exists(newTarget)
                                       )
                                    {
                                        TrimAndAddUnique(newTarget);
                                        found = rootPaths.TryGetValue(filename, out foundPath);
                                        break;
                                    }
                                }
                            }
                            else if (isAShare
                               && (configDetails.KnownSharePaths != null)
                               )
                            {
                                found = configDetails.KnownSharePaths.Exists(delegate(string sharePath)
                                {
                                    Log.Trace("Try and find from [{0}][{1}]", sharePath, filename);
                                    return rootPaths.TryGetValue(sharePath + filename, out foundPath);
                                });

                            }
                            if (!found)
                            {
                                Log.Trace("was this a failed redirect thing from a network share ? [{0}]", filename);
                                if (isCreate)
                                {
                                    int lastDir = filename.LastIndexOf(Path.DirectorySeparatorChar);
                                    if (lastDir > -1)
                                    {
                                        Log.Trace("Perform search for path: {0}", filename);
                                        string newPart = filename.Substring(lastDir);
                                        foundPath = GetPath(filename.Substring(0, lastDir), false) + newPart;
                                        Log.Trace("Now make sure it can be found when it tries to repopen via the share");
                                        TrimAndAddUnique(foundPath);
                                    }
                                    else
                                        foundPath = getNewRoot() + filename; // This is used when creating new directory / file
                                }
                                else
                                    foundPath = getNewRoot() + filename; // This is used when creating new directory / file
                            }
                        }
                    }
                    finally
                    {
                        rootPathsSync.ExitUpgradeableReadLock();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException("GetPath threw: ", ex);
            }
            finally
            {
                Log.Debug("GetPath from [{0}] found [{1}]", filename, foundPath);
            }
            return foundPath;
        }



        public static string getRoot(string path)
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
        public static string getNewRoot()
        {
            //if (Log.IsTraceEnabled == true)
            //{
            //    LogToString();
            //}

            return getNewRoot(NO_PATH_TO_FILTER);
        }



        // this method returns a path (real physical path) of a place where the next folder/file root can be.
        // FilterThisPath can be used to not use a specific location (for mirror feature)
        public static string getNewRoot(string FilterThisPath)
        {
            switch (configDetails.eAllocationMode) 
            {
                case ConfigDetails.AllocationModes.priority:
                    return getHighestPriority(FilterThisPath);

                case ConfigDetails.AllocationModes.balanced:
                    return getWithMostFreeSpace(FilterThisPath);

                case ConfigDetails.AllocationModes.mirror:
                    return getWithMostFreeSpace(FilterThisPath);

                default:
                    return getHighestPriority(FilterThisPath);
            }
        }



        // returns the next root with the highest priority
        private static string getHighestPriority(string FilterThisPath)
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
                        if (num > configDetails.HoldOffBufferBytes)
                        {
                            return configDetails.SourceLocations[i];
                        }
                    }
                }
            }

            return getWithMostFreeSpace(null);
        }



        // returns the root with the most free space
        private static string getWithMostFreeSpace(string FilterThisPath)
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

            return PathWithMostFreeSpace;
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



        public static DokanFileInfo copyDokanFileInfo(DokanFileInfo indfi)
        {
            DokanFileInfo newdfi = new DokanFileInfo(0); // no need for internal context
            newdfi.Context = indfi.Context;
            newdfi.IsDirectory = indfi.IsDirectory;
            newdfi.DeleteOnClose = indfi.DeleteOnClose;
            newdfi.PagingIo = indfi.PagingIo;
            newdfi.SynchronousIo = indfi.SynchronousIo;
            newdfi.Nocache = indfi.Nocache;
            newdfi.WriteToEndOfFile = indfi.WriteToEndOfFile;
            newdfi.InfoId = indfi.InfoId;
            newdfi.ProcessId = indfi.ProcessId;

            return newdfi;
        }



        // adds the root path to rootPaths dicionary for a specific file
        public static string TrimAndAddUnique(string fullFilePath)
        {
            int index = configDetails.SourceLocations.FindIndex(fullFilePath.StartsWith);
            if (index >= 0)
            {
                string key = fullFilePath.Remove(0, configDetails.SourceLocations[index].Length);
                try
                {
                    Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
                    rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
                    // TODO: Add the collisions / duplicate feedback from here
                    rootPaths[key] = fullFilePath;
                }
                finally
                {
                    rootPathsSync.ExitWriteLock();
                }
                return key;
            }
            throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
        }



        // removes a root from root lookup
        public static void RemoveTargetFromLookup(string realFilename)
        {
            try
            {
                rootPathsSync.TryEnterUpgradeableReadLock(configDetails.LockTimeout);
                string key = string.Empty;
                foreach (KeyValuePair<string, string> kvp in rootPaths.Where(kvp => kvp.Value == realFilename))
                {
                    key = kvp.Key;
                    break;
                }
                if (!String.IsNullOrEmpty(key))
                {
                    rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
                    rootPaths.Remove(key);
                }
            }
            finally
            {
                rootPathsSync.ExitUpgradeableReadLock();
            }
        }



        // removes a path from root lookup
        public static void RemoveFromLookup(string filename)
        {
            try
            {
                rootPathsSync.TryEnterWriteLock(configDetails.LockTimeout);
                rootPaths.Remove(filename);
            }
            finally
            {
                rootPathsSync.ExitWriteLock();
            }
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
