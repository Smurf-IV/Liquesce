using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        static private readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();
        static private readonly Logger Log = LogManager.GetCurrentClassLogger();

        private ConfigDetails configDetails;

        // constructor
        public Roots(ConfigDetails configDetails)
        {
            this.configDetails = configDetails;
        }

        public string getRoot(string path)
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
        public string getNewRoot()
        {
            //if (Log.IsTraceEnabled == true)
            //{
            //    LogToString();
            //}

            return getNewRoot(NO_PATH_TO_FILTER);
        }

        // this method returns a path (real physical path) of a place where the next folder/file root can be.
        // FilterThisPath can be used to not use a specific location (for mirror feature)
        public string getNewRoot(string FilterThisPath)
        {
            if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.priority)
            {
                return getHighestPriority(FilterThisPath);
            }
            else if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.balanced)
            {
                return getWithMostFreeSpace(FilterThisPath);
            }
            else if (configDetails.eAllocationMode == ConfigDetails.AllocationModes.mirror)
            {
                return getWithMostFreeSpace(FilterThisPath);
            }
            else
            {
                return getHighestPriority(FilterThisPath);
            }
        }


        // returns the next root with the highest priority
        private string getHighestPriority(string FilterThisPath)
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
        private string getWithMostFreeSpace(string FilterThisPath)
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
        private void LogToString()
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
