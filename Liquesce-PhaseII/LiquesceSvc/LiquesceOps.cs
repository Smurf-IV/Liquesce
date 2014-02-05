#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using LiquesceFacade;
using NLog;
using PID = LiquesceSvc.ProcessIdentity;

namespace LiquesceSvc
{
   internal partial class LiquesceOps
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      // currently open files...
      // last key
      static private Int64 openFilesLastKey;
      // lock
      private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      // dictionary of all open files
      private readonly Dictionary<Int64, NativeFileOps> openFiles = new Dictionary<Int64, NativeFileOps>();

      private readonly Roots roots;
      private readonly MountDetail mountDetail;

      public LiquesceOps(MountDetail mountDetail, uint cacheLifetimeSeconds)
      {
         this.mountDetail = mountDetail;
         roots = new Roots(mountDetail, cacheLifetimeSeconds);
      }

      #region FindFiles etc. Implementation

      // TODO: Need a away to make the IEnumberable, so that large dir counts do not get bogged down.
      // yield return WIN32_FIND_DATA
      private void FindFiles(string startPath, int processId, out WIN32_FIND_DATA[] files, string pattern = "*")
      {
         files = null;
         try
         {
            Log.Debug("FindFiles IN startPath[{0}], pattern[{1}]", startPath, pattern);
            // NTFS is case-preserving but case-insensitive in the Win32 namespace
            Dictionary<string, WIN32_FIND_DATA> uniqueFiles =
               new Dictionary<string, WIN32_FIND_DATA>(StringComparer.OrdinalIgnoreCase);
            //uniqueFiles.Add(".",
            // FindFiles should not return an empty array. It should return parent ("..") and self ("."). That wasn't obvious!
            PID.Invoke(processId, delegate
            {
               // Do this in reverse, so that the preferred references overwrite the older files
               for (int i = mountDetail.SourceLocations.Count - 1; i >= 0; i--)
               {
                  Dictionary<string, WIN32_FIND_DATA> localUniqueFiles = new Dictionary<string, WIN32_FIND_DATA>(StringComparer.OrdinalIgnoreCase);

                  NativeFileFind.AddFiles(mountDetail.SourceLocations[i].SourcePath + startPath, localUniqueFiles, pattern);
                  bool useIsReadOnly = mountDetail.SourceLocations[i].UseIsReadOnly;
                  foreach (KeyValuePair<string, WIN32_FIND_DATA> uniqueFile in localUniqueFiles)
                  {
                     WIN32_FIND_DATA findData = uniqueFile.Value;
                     if (useIsReadOnly)
                     {
                        findData.dwFileAttributes |= (uint) NativeFileOps.EFileAttributes.Readonly;
                     }
                     uniqueFiles[uniqueFile.Key] = findData;
                  }
               }
            });
            // If these are not found then the loop speed of a "failed remove" and "not finding" is the same !
            uniqueFiles.Remove(@"System Volume Information"); // NTFS
            files = new WIN32_FIND_DATA[uniqueFiles.Values.Count];
            uniqueFiles.Values.CopyTo(files, 0);
         }
         finally
         {
            Log.Debug("FindFiles OUT [found {0}]", (files != null ? files.Length : 0));
            if (Log.IsTraceEnabled)
            {
               if (files != null)
               {
                  StringBuilder sb = new StringBuilder();
                  sb.AppendLine();
                  foreach (WIN32_FIND_DATA fileInformation in files)
                  {
                     sb.AppendLine(fileInformation.cFileName);
                  }
                  Log.Trace(sb.ToString());
               }
            }
         }
      }

      public void GetDiskFreeSpace(out ulong freeBytesAvailable, out ulong totalBytes, out ulong totalFreeBytes)
      {
         Log.Trace("GetDiskFreeSpace IN ");
         freeBytesAvailable = totalBytes = totalFreeBytes = 0;

         HashSet<string> uniqueSources = new HashSet<string>();
         foreach (SourceLocation location in mountDetail.SourceLocations.Where(location => !location.UseIsReadOnly))
         {
            uniqueSources.Add(NativeFileOps.GetRootOrMountFor(location.SourcePath));
         }

         foreach (string source in uniqueSources)
         {
            ulong num;
            ulong num2;
            ulong num3;
            if (GetDiskFreeSpaceExW(source, out num, out num2, out num3))
            {
               freeBytesAvailable += num;
               totalBytes += num2;
               totalFreeBytes += num3;
            }
            Log.Debug("DirectoryName=[{0}], FreeBytesAvailable=[{1}], TotalNumberOfBytes=[{2}], TotalNumberOfFreeBytes=[{3}]",
                  source, num, num2, num3);
         }
         Log.Trace("GetDiskFreeSpace OUT");
      }

      #endregion

      public void InitialiseShares(object state)
      {
         Log.Debug("InitialiseShares IN");
         try
         {
            Thread.Sleep(250); // Give the driver some time to mount
            // Now check (in 2 phases) the existence of the drive
            string path = mountDetail.DriveLetter + ":" + Roots.PathDirectorySeparatorChar;
            while (!Directory.Exists(path))
            {
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares");
               Thread.Sleep(1000);
            }
            // 2nd phase as the above is supposed to be cheap but can return false +ves
            do
            {
               string[] drives = Environment.GetLogicalDrives();
               if (Array.Exists(drives, dr => dr.Remove(1) == mountDetail.DriveLetter))
                  break;
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares (Phase 2)");
               Thread.Sleep(100);
            } while (ManagementLayer.Instance.State == LiquesceSvcState.Running);

            foreach (LanManShareDetails shareDetails in mountDetail.SharesToRestore)
            {
               try
               {
                  Log.Info("Restore share for : [{0}] [{1} : {2}]", shareDetails.Path, shareDetails.Name, shareDetails.Description);
                  LanManShareHandler.SetLanManShare(shareDetails);
               }
               catch (Exception ex)
               {
                  Log.ErrorException("Unable to restore share for : " + shareDetails.Path, ex);
               }
            }
            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.Running, "Shares restored - good to go");
         }
         catch (Exception ex)
         {
            Log.ErrorException("Init shares threw: ", ex);
            ManagementLayer.Instance.FireStateChange(LiquesceSvcState.InError, "Init shares reports: " + ex.Message);
         }
         finally
         {
            Log.Debug("InitialiseShares OUT");
         }
      }
      #region DLL Imports

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetDiskFreeSpaceExW(string lpDirectoryName, out ulong lpFreeBytesAvailable,
         out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion

   }

}