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
using DokanNet;
using NLog;

// ReSharper disable UnusedMember.Local
namespace LiquesceSvc
{
   static internal class XMoveFile
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// Moves / Renames a file / Directory. This function has to work out which
      /// </summary>
      /// <param name="roots"></param>
      /// <param name="oldName">FullPath to Old</param>
      /// <param name="newName">FullPath to new</param>
      /// <param name="replaceIfExisting"></param>
      /// <param name="info"></param>
      public static void Move(Roots roots, string oldName, string newName, bool replaceIfExisting, bool isDirectory, uint processID)
      {
         Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, oldName, newName);

         FileSystemInfo pathSource = roots.GetPath(oldName);
         ulong pathSource_Length = (ulong) (isDirectory?0:(pathSource as FileInfo).Length);
         FileSystemInfo pathTarget;
         // Got to handle defect / Issue "http://code.google.com/p/dokan/issues/detail?id=238" 
         if (ProcessIdentity.CouldBeSMB(processID)
            && (600 == Dokan.DokanVersion())
            )
         {
            Log.Warn("ProcessID indicates that this could be an SMB redirect. Workaround dokan issue 238!");
            int lastIndex = oldName.LastIndexOf(Roots.PathDirectorySeparatorChar);
            string offsetPath = (lastIndex > -1) ? newName.Remove(0, lastIndex + 1) : newName;
            pathTarget = roots.GetPathRelatedtoShare(offsetPath, 0);
            newName = roots.GetRelative(pathTarget.FullName);

         }
         // Now check to see if this has enough space to make a "Copy" before the atomic delete of MoveFileEX
         pathTarget = roots.GetPath(newName, pathSource_Length);
         string pathTarget_FullName = pathTarget.FullName;
         // Now check to see if this file exists, or exists in another location (Due to share redirect)
         if (pathTarget.Exists 
            && !replaceIfExisting
            )
         {
            throw new System.ComponentModel.Win32Exception(Dokan.ERROR_ALREADY_EXISTS* -1); // Need to remove the Dokan -ve, It will be put back in the catch
         }

         if (!isDirectory)
         {
            //1 Same directory Rename Scenario:
            //   MoveFileProxy replaceIfExisting [0] file: [\Test1\ds\New Text Document (2).txt] newfile: [\Test1\ds\test.txt] 
            //
            //2 Move into a subdir off "ds" Scenario:
            //   MoveFileProxy replaceIfExisting [0] file: [\Test1\ds\New Text Document (3).txt] newfile: [\Test1\ds\ds\New Text Document (3).txt] 
            //   File will be in [\Test1\ds\ds\New Text Document (3).txt]
            //
            //3 Rename a File when a directory name already exists
            //   MoveFileProxy replaceIfExisting [0] file: [\Test1\ds\New Text Document (2).txt] newfile: [\Test1\ds\test.txt]  
            //   But the Direcotory [\Test1\ds\test.txt] exists ! Resulting in the correct Explorer message
            //
            //4 Rename a file to be the same name as the parent directory
            //   MoveFile [\Test1\ds\test.txt\New Text Document (2).txt] to [\test.txt] IN DokanProcessId[2240]
            //   Should result in [\Test1\ds\test.txt\test.txt]
            //
            //5 Move a file to be Closer to the Mount point (higher) in the directory structure
            //   MoveFileProxy replaceIfExisting [0] file: [\Test1\ds\ds\New Text Document (3).txt] newfile: [\Test1\New Text Document (3).txt]
            //
            //6 Rename a file to a Name that already exists at this level
            //   MoveFileProxy replaceIfExisting [0] file: [\Test1\ds\testSame (2).txt] newfile: [\Test1\ds\testSame.txt] 
            //   [\Test1\ds\testSame.txt] already exists; so should display a MessageBox in Explorer
            //
            //7 Now perform all the above via a share access that does not contain the dir name 
            //   i.e. ShareName is [TestShare] pointing at [\Test1]
            //   Scenario 1 produces MoveFile [\Test1\ds\New Text Document (2).txt] to [\ds\test.txt]
            //
            //8 Perform a delete on the Server Mount to see if the file is moved to the recycle bin
            //   MoveFile Should be used to go to the Recycler dependent on the Drive Format Type and ACL Status
            //

            // The new target might be on a different drive, so re-create the folder path to the new location
            int lastPathIndex = pathTarget_FullName.LastIndexOf(Roots.PathDirectorySeparatorChar);
            string newPath = (lastPathIndex > -1) ? pathTarget_FullName.Remove(lastPathIndex) : pathTarget_FullName;
            if ( !string.IsNullOrEmpty( newPath ) )
               Directory.CreateDirectory(newPath);
            MoveFileEx(pathSource.FullName, pathTarget_FullName, replaceIfExisting);
         }
         else
         {
            // Cannot use MoveFileEx, as this app needs to remove the old cached values.
            // So call a function to move each file, and subdir recusively via XMoveDirectory
            // Repeat the file tests above, but use directories instead.
            string[] allPossibleTargets = roots.GetAllPaths(oldName);
            // Now do them backwards so that overwrites are done correctly
            Array.Reverse(allPossibleTargets);
            XMoveDirectory dirMover = new XMoveDirectory();
            // newName will already be calculated as the relative offset (Should have starting '\')
            foreach (string dirSource in allPossibleTargets)
            {
               string currentNewTarget = Path.GetFullPath(roots.GetRoot(dirSource) + newName);
               dirMover.Move(roots, dirSource, currentNewTarget, replaceIfExisting);
            }
         }
         // While we are here, remove 
         roots.RemoveTargetFromLookup(oldName); // File has been removed
         roots.RemoveTargetFromLookup(newName); // Not a null file anymore
      }

      internal static void MoveFileEx(string pathSource, string pathTarget, bool replaceIfExisting)
      {
         //Check if the destination is on pool drive

         //System.IO.File.Move(pathSource, pathTarget);
         // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
         MoveFileFlags dwFlags = (replaceIfExisting ? MoveFileFlags.MOVEFILE_REPLACE_EXISTING : 0);
         // If the file is to be moved to a different volume, the function simulates the move by using the 
         // CopyFile and DeleteFile functions.
         dwFlags |= MoveFileFlags.MOVEFILE_COPY_ALLOWED;

         // The function does not return until the file is actually moved on the disk.
         // Setting this value guarantees that a move performed as a copy and delete operation 
         // is flushed to disk before the function returns. The flush occurs at the end of the copy operation.
         dwFlags |= MoveFileFlags.MOVEFILE_WRITE_THROUGH;

         Log.Trace("MoveFileExW(pathSource[{0}], pathTarget[{1}], dwFlags[{2}])", pathSource, pathTarget, dwFlags);
         if (!MoveFileExW(pathSource, pathTarget, dwFlags))
            throw new System.ComponentModel.Win32Exception();
      }

      /// <summary>
      /// http://pinvoke.net/default.aspx/Enums/MoveFileFlags.html
      /// </summary>
      [Flags]
      public enum MoveFileFlags
      {
         MOVEFILE_REPLACE_EXISTING = 0x00000001,
         MOVEFILE_COPY_ALLOWED = 0x00000002,
         MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
         MOVEFILE_WRITE_THROUGH = 0x00000008,
         MOVEFILE_CREATE_HARDLINK = 0x00000010,
         MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
      }
      /// <summary>
      /// http://www.pinvoke.net/default.aspx/kernel32.movefileex
      /// </summary>
      /// <param name="lpExistingFileName"></param>
      /// <param name="lpNewFileName"></param>
      /// <param name="dwFlags"></param>
      /// <returns></returns>
      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, CallingConvention = CallingConvention.Winapi)]
      private static extern bool MoveFileExW(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);
   }
}
// ReSharper restore UnusedMember.Local
