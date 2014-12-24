#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="XMoveFile.cs" company="Smurf-IV">
//
//  Copyright (C) 2010-2014 Simon Coghlan (Aka Smurf-IV) & fpDragon
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

#endregion Copyright (C)

using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.InteropServices;
using CBFS;
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
      /// <param name="useInplaceRenaming"></param>
      public static void Move(Roots roots, string oldName, string newName, bool replaceIfExisting, bool useInplaceRenaming)
      {
         Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, oldName, newName);

         NativeFileOps pathSource = roots.GetFromPathFileName(oldName);
         if (!pathSource.IsDirectory)
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
            //   MoveFile [\Test1\ds\test.txt\New Text Document (2).txt] to [\test.txt]
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
            // While we are here, remove
            roots.RemoveFromLookup(oldName); // File has been removed

            // Need to check if this is going to the recycler
            if (Roots.RecyclerDirectoryNames.Any(newName.Contains))
            {
               SendToRecycler(roots, pathSource.FullName, newName);
            }
            else if (useInplaceRenaming)
            {
               string relativeNewParent = NativeFileOps.GetParentPathName(newName);
               string sourceRoot = roots.GetRoot(pathSource.DirectoryPathOnly);
               // relativeNewParent will start with the //'s
               string newTarget = sourceRoot + relativeNewParent;
               // This will handle Drive 1 containg the dir, drive 2 containing the file at a lower point
               // i.e. copying down the dir chain to a non-existing dir whilst inplace is in force.
               NativeFileOps.CreateDirectory(newTarget);

               string fileTarget = Path.Combine(newTarget, NativeFileOps.GetFileName(newName));
               MoveFileExInPlace(pathSource.FullName, fileTarget, replaceIfExisting);
            }
            else
            {
               // Now check to see if this has enough space to make a "Copy" before the atomic delete of MoveFileEX
               NativeFileOps pathTarget = roots.FindCreateNewAllocationRootPath(newName, pathSource.Length);
               // The new target might be on a different drive, so re-create the folder path to the new location
               NativeFileOps.CreateDirectory(pathTarget.DirectoryPathOnly);
               MoveFileEx(pathSource.FullName, pathTarget.FullName, replaceIfExisting);
            }

         }
         else
         {
            // Repeat the file tests above, but use directories instead.
            Log.Trace("GetAllPaths [{0}]", oldName);
            string[] allPossibleRootTargets = roots.GetAllRootPathsWhereExists(oldName).ToArray();
            Log.Trace("Now do them backwards so that overwrites are done correctly");
            Array.Reverse(allPossibleRootTargets);

            // While we are here, remove
            roots.RemoveAllTargetDirsFromLookup(oldName); // File has been removed

            // newName will already be calculated as the relative offset (Should have starting '\')
            foreach (string dirRootSource in allPossibleRootTargets)
            {
               string sourceName = dirRootSource + oldName;
               // Need to check if this is going to the recycler
               if (Roots.RecyclerDirectoryNames.Any(newName.Contains))
               {
                  SendToRecycler(roots, sourceName, newName);
                  continue;
               }

               DirectoryInfo source = new DirectoryInfo(sourceName);
               string newTarget = dirRootSource + newName;
               DirectoryInfo target = new DirectoryInfo(newTarget);

               // This will handle Drive 1 containg the dir, drive 2 containing the file at a lower point
               // i.e. copying down the dir chain to a non-existing dir whilst inplace is in force.
               NativeFileOps.CreateDirectory(NativeFileOps.GetDirectoryName(newTarget));

               if (useInplaceRenaming
                  && !target.Exists    // Cannot do inplace if the DirExists
                  )
               {
                  //Directory.Move(source.FullName, target.FullName);
                  //source.MoveTo(target.FullName);
                  MoveFileExInPlace(source.FullName, target.FullName, false);
               }
               else
               {
                  Move(roots, source, newName, replaceIfExisting);
               }
            }
         }

         roots.RemoveFromLookup(newName); // Not a null file anymore
      }

      private static void Move(Roots roots, DirectoryInfo pathSource, string pathTarget_FullName, bool replaceIfExisting)
      {
         string pathSource_FullName = pathSource.FullName;
         Log.Trace("Move(pathSource[{0}], pathTarget[{1}], replaceIfExisting[{2}], useInplaceRenaming[{3}])", pathSource_FullName, pathTarget_FullName, replaceIfExisting);

         // Don't forget this directory
         NativeFileOps newTarget = roots.FindCreateNewAllocationRootPath(pathTarget_FullName);
         newTarget.CreateDirectory();

         // for every file in the current folder
         foreach (FileInfo filein in pathSource.EnumerateFiles())
         {
            // with each file, allow the target to distribute in case space is a problem
            string fileSource = Path.Combine(pathSource_FullName, filein.Name);
            newTarget = roots.FindCreateNewAllocationRootPath(Path.Combine(pathTarget_FullName, filein.Name), (ulong)filein.Length);
            // The new target might be on a different drive, so re-create the folder path to the new location
            NativeFileOps.CreateDirectory(newTarget.DirectoryPathOnly);

            MoveFileEx(fileSource, newTarget.FullName, replaceIfExisting);
         }

         // for every subfolder recurse
         foreach (DirectoryInfo dr in pathSource.EnumerateDirectories())
         {
            string dirSource = Path.Combine(pathTarget_FullName, dr.Name);
            Move(roots, dr, dirSource, replaceIfExisting);
         }

         Log.Trace("Delete this Dir[{0}]", pathSource_FullName);
         try
         {
            NativeFileOps.DeleteDirectory(pathSource_FullName);
         }
         catch (Exception ex)
         {
            Log.WarnException("Failed to delete " + pathSource_FullName, ex);
         }         
      }

      private static void MoveFileExInPlace(string pathSource, string pathTarget, bool replaceIfExisting)
      {
         // http://msdn.microsoft.com/en-us/library/aa365240%28VS.85%29.aspx
         MoveFileFlags dwFlags = (replaceIfExisting ? MoveFileFlags.MOVEFILE_REPLACE_EXISTING : 0);

         Log.Trace("MoveFileExW(pathSource[{0}], pathTarget[{1}], dwFlags[{2}])", pathSource, pathTarget, dwFlags);
         if (!MoveFileExW(pathSource, pathTarget, dwFlags))
         {
            throw new Win32Exception();
         }
      }

      private static void MoveFileEx(string pathSource, string pathTarget, bool replaceIfExisting)
      {
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
         {
            throw new Win32Exception();
         }
      }

      private static void SendToRecycler(Roots roots, string pathSource, string pathTarget)
      {
         // Work out the location of the Recycler
         string trimmedTarget = (from recyclerDirectoryName in Roots.RecyclerDirectoryNames 
                                 let indexOf = pathTarget.IndexOf(recyclerDirectoryName, StringComparison.Ordinal) 
                                 where indexOf >= 0 
                                 select pathTarget.Remove(0, indexOf + recyclerDirectoryName.Length)
                                 ).FirstOrDefault();

         string sourceRoot = roots.GetRoot(pathSource);
         string recyclerTarget = (from recycler in Roots.RecyclerDirectoryNames 
                                  select new DirectoryInfo(sourceRoot + recycler) 
                                  into info1 
                                  where info1.Exists 
                                  select info1.FullName + trimmedTarget
                                  ).FirstOrDefault();
         
         if (string.IsNullOrEmpty(recyclerTarget))
         {
            // Set to use the @"Recycle Bin"
            DirectoryInfo info = new DirectoryInfo(sourceRoot + Roots.RecyclerDirectoryNames[1]);
            info.Create();
            info.Attributes = info.Attributes | FileAttributes.Hidden | FileAttributes.System;
            recyclerTarget = info.FullName + trimmedTarget;
            //throw new Win32Exception(CBFSWinUtil.ERROR_INVALID_ADDRESS, string.Format("Unable to send {0} to the Recycle.bin", pathTarget));
         }
         // Create the Directory - In case it is in mixed mode
         NativeFileOps.CreateDirectory(NativeFileOps.GetDirectoryName(recyclerTarget));
         // Move and overwrite - Just in case
         MoveFileEx(pathSource, recyclerTarget, true);
      }

      #region Win32

      /// <summary>
      /// http://pinvoke.net/default.aspx/Enums/MoveFileFlags.html
      /// </summary>
      [Flags]
      private enum MoveFileFlags
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

      #endregion Win32
   }
}

// ReSharper restore UnusedMember.Local