using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LiquesceFaçade;

namespace LiquesceSvc
{
    class Roots
    {

        private List<string> rootslist;
        private ConfigDetails configDetails;

        public Roots(ConfigDetails configDetails)
        {
            this.rootslist = configDetails.SourceLocations;
            this.configDetails = configDetails;
        }

        public string get()
        {
            if (configDetails.AllocationMode == "priority")
            {
                return getHighestPriority();
            }
            else if (configDetails.AllocationMode == "balanced")
            {
                return getWithMostFreeSpace();
            }
            else
            {
                return getHighestPriority();
            }
        }

        private string getHighestPriority()
        {
            for (int i = 0; i < rootslist.Count; i++)
            {
                ulong num;
                ulong num2;
                ulong num3;
                if (GetDiskFreeSpaceEx(rootslist[i], out num, out num2, out num3))
                {
                    if (num > configDetails.HoldOffBufferBytes)
                    {
                        return rootslist[i];
                    }
                }
            }

            return getWithMostFreeSpace();
        }

        private string getWithMostFreeSpace()
        {
            ulong HighestFreeSpace = 0;
            string PathWithMostFreeSpace = "";

            rootslist.ForEach(str =>
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
            });

            return PathWithMostFreeSpace;
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
