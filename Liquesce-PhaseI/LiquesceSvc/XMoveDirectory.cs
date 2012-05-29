#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="XMoveDirectory.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Smurf-IV & fpDragon
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
using System.Transactions;
using LiquesceSvc.FileSystemTransact;
using NLog;

namespace LiquesceSvc
{
   /// <summary>
   /// Using a transaction to attempt to rename / move the source directory across it's various areas
   /// </summary>
   internal class XMoveDirectory
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly List<string> dirAllreadyCreatedInThisTrans = new List<string>();

      public void Move(Roots roots, string source, string pathTarget_FullName, bool replaceIfExisting)
      {
         Log.Trace("XMoveDirectory.Move(roots[{0}], dirSource[{1}], currentNewTarget[{2}], replaceIfExisting[{3}])", roots, source, pathTarget_FullName, replaceIfExisting);
         try
         {
            TransFileManager fileManager = new TransFileManager();
            using (TransactionScope scope1 = new TransactionScope())
            {
               Move(roots, new DirectoryInfo(source), pathTarget_FullName, replaceIfExisting, fileManager);
               scope1.Complete();
            }
         }
         catch ( Exception ex )
         {
            Log.ErrorException( "Transactions should have moved back:", ex );
            throw;
         }
      }

      private void Move(Roots roots, DirectoryInfo pathSource, string pathTarget_FullName, bool replaceIfExisting, TransFileManager fileManager)
      {
         string pathSource_FullName = pathSource.FullName;
         Log.Trace("Move(pathSource[{0}], pathTarget[{1}], replaceIfExisting[{2}])", pathSource_FullName, pathTarget_FullName, replaceIfExisting);
         // Create in place ready for the files or other SubDir's
         CreateDirTrans(pathTarget_FullName, fileManager);
         // for every file in the current folder
         foreach (FileInfo filein in pathSource.GetFiles())
         {
            // with each file, allow the target to distribute in case space is a problem
            string fileSource = Path.Combine(pathSource_FullName, filein.Name);
            string fileTarget = Path.Combine(pathTarget_FullName, filein.Name);

            fileManager.Snapshot(fileSource);
            XMoveFile.MoveFileEx(fileSource, fileTarget, replaceIfExisting);
         }

         // for every subfolder recurse
         foreach (DirectoryInfo dr in pathSource.GetDirectories())
         {
            Move(roots, dr, Path.Combine(pathTarget_FullName, dr.Name), replaceIfExisting, fileManager);
         }

         Log.Trace("Delete this Dir[{0}]", pathSource_FullName);
         TransFileManager.DeleteDirectory(pathSource_FullName);
         // While we are here, remove 
         roots.RemoveTargetFromLookup(roots.GetRelative(pathSource_FullName));
      }

      private void CreateDirTrans(string fullPathName, TransFileManager fileManager)
      {
         if (!dirAllreadyCreatedInThisTrans.Contains(fullPathName))
         {
            if (!Directory.Exists(fullPathName))
            {
               dirAllreadyCreatedInThisTrans.Add(fullPathName);
               fileManager.CreateDirectory(fullPathName);
            }
         }
      }


   }
}