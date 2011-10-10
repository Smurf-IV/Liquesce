#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="XMoveFile.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV & fpDragon
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
using System.Runtime.InteropServices;
using NLog;

namespace LiquesceSvc
{
   static internal class XMoveFile
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public static void Move(Roots roots, string filename, string newname, bool replaceIfExisting, bool isDirectory)
      {
         Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);

         if (!isDirectory)
         {
            // *** NTh Change ***
            // Check to see if there are more copies of the same file and move them to the same location
            foreach (string pathSource in Roots.GetAllFilePaths(filename))
            {
               string pathTarget = pathSource.Substring(0, pathSource.LastIndexOf(filename)) + newname;
               // *** NTh Change ***
               // Optimization in the Move process, in my opinion, we should try to keep the file on the same physical disk
               // This function creates all the tree path from root to the child directory
               Directory.CreateDirectory(pathTarget.Substring(0, pathTarget.LastIndexOf("\\")));
               Log.Info("MoveFile pathSource: [{0}] pathTarget: [{1}]", pathSource, pathTarget);
               MoveFileEx(pathSource, pathTarget, replaceIfExisting);
               // While we are here, remove 
               roots.RemoveTargetFromLookup(pathSource);
            }
         }
         else
         {
            // getting all paths of the source location
            // rename every 
            foreach (string dirSource in Roots.GetAllPaths(filename))
            {
               string dirTarget = Roots.GetRoot(dirSource) + newname;
               XMoveDirectory.Move(roots, dirSource, dirTarget, replaceIfExisting);
            }
         }
      }

      private static void MoveFileEx(string pathSource, string pathTarget, bool replaceIfExisting)
      {
         //Check if the destination is on pool drive

         //System.IO.File.Move(pathSource, pathTarget);
         // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
         UInt32 dwFlags = (uint)(replaceIfExisting ? 1 : 0);
         // If the file is to be moved to a different volume, the function simulates the move by using the 
         // CopyFile and DeleteFile functions.
         dwFlags += 2; // MOVEFILE_COPY_ALLOWED 

         // The function does not return until the file is actually moved on the disk.
         // Setting this value guarantees that a move performed as a copy and delete operation 
         // is flushed to disk before the function returns. The flush occurs at the end of the copy operation.
         dwFlags += 8; // MOVEFILE_WRITE_THROUGH

         if (!MoveFileExW(pathSource, pathTarget, dwFlags))
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
      }

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      private static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, UInt32 dwFlags);
   }
}
