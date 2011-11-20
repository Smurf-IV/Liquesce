#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="XMoveDirectory.cs" company="Smurf-IV">
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Transactions;

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
      private readonly Dictionary<string, string> dirMovesNotTransacted = new Dictionary<string, string>();

      public void Move(Roots roots, string source, string pathTarget_FullName, bool replaceIfExisting)
      {
         try
         {
            using (TransactionScope scope = new TransactionScope())
            {
               Move(roots, new DirectoryInfo(source), pathTarget_FullName, replaceIfExisting);
               scope.Complete();
            }
         }
         catch
         {
            // try to move the non transactions back
            foreach (KeyValuePair<string, string> newToOld in dirMovesNotTransacted)
            {
               try
               {
                  XMoveFile.MoveFileEx(newToOld.Key, newToOld.Value, true);
               }
               catch {}
            }
            throw;
         }
      }

      private void Move(Roots roots, DirectoryInfo pathSource, string pathTarget_FullName, bool replaceIfExisting)
      {
         string pathSource_FullName = pathSource.FullName;
         Log.Trace("Move(pathSource[{0}], pathTarget[{1}], replaceIfExisting[{2}])", pathSource_FullName, pathTarget_FullName, replaceIfExisting);
         // Create in place ready for the files or other SubDir's
         CreateDirTrans(pathSource, pathTarget_FullName);
         // for every file in the current folder
         foreach (FileInfo filein in pathSource.GetFiles())
         {
            // with each file, allow the target to distribute in case space is a problem
            string fileSource = Path.Combine( pathSource_FullName, filein.Name);
            string fileTarget = Path.Combine( pathTarget_FullName, filein.Name);

            MoveFileTrans(fileSource, fileTarget, replaceIfExisting);
         }

         // for every subfolder recurse
         foreach (DirectoryInfo dr in pathSource.GetDirectories())
         {
            Move(roots, dr, Path.Combine(pathTarget_FullName, dr.Name), replaceIfExisting);
         }

         Log.Trace("Delete this Dir[{0}]", pathSource_FullName);
         DeleteDirTrans(pathSource);
         // While we are here, remove 
         roots.RemoveTargetFromLookup(roots.GetRelative(pathSource_FullName));
      }

      private void CreateDirTrans(DirectoryInfo pathSource, string fullPathName)
      {
         if (!dirAllreadyCreatedInThisTrans.Contains(fullPathName))
         {
            if (!Directory.Exists(fullPathName))
            {
               dirAllreadyCreatedInThisTrans.Add(fullPathName);
               if (!KtmTransactionHandle.IsAvailable)
               {
                  Directory.CreateDirectory(fullPathName, pathSource.GetAccessControl());
                  dirMovesNotTransacted[fullPathName] = pathSource.FullName;
               }
               else
               {
                  TransactedCreateDirOnly(pathSource, fullPathName);
               }
            }
         }
      }

      private void TransactedCreateDirOnly(DirectoryInfo pathSource, string fullPathName)
      {
         int errorCode = 0;
         using (KtmTransactionHandle ktmScope = KtmTransactionHandle.CreateKtmTransactionHandle())
            if (!CreateDirectoryTransactedW(pathSource.FullName, fullPathName, IntPtr.Zero, ktmScope))
               errorCode = Marshal.GetLastWin32Error();
         switch (errorCode)
         {
            case 0: // All is good :-)
               break;
            case 6832: // Not Allowed to use transaction on this file !
               Directory.CreateDirectory(fullPathName, pathSource.GetAccessControl());
               break;
            case ERROR_PATH_NOT_FOUND: // This means that a parent has not been created
               // Recusion will bail if new DirectoryInfo gets a null !
               TransactedCreateDirOnly(pathSource, new DirectoryInfo(fullPathName).Parent.FullName);
               // Now recall to get this child recreated
               TransactedCreateDirOnly(pathSource, fullPathName);
               break;
            default:
               Log.Error("CreateDirectoryTransactedW threw [{0}]", errorCode);
               throw new System.ComponentModel.Win32Exception(errorCode);
         }
      }

      private void DeleteDirTrans(DirectoryInfo pathSource)
      {
         if (!KtmTransactionHandle.IsAvailable)
            pathSource.Delete();
         else
         {
            int errorCode = 0;
            using (KtmTransactionHandle ktmScope = KtmTransactionHandle.CreateKtmTransactionHandle())
               if (!RemoveDirectoryTransactedW(pathSource.FullName, ktmScope))
                  errorCode = Marshal.GetLastWin32Error();
            switch (errorCode)
            {
               case 0: // All is good :-)
                  break;
               case 6832:  // Not Allowed to use transaction on this file !
                  pathSource.Delete();
                  break;
               default:
                  Log.Error("RemoveDirectoryTransactedW threw [{0}]", errorCode);
                  throw new System.ComponentModel.Win32Exception(errorCode);
            }
         }
      }

      private void MoveFileTrans(string fileSource, string fileTarget, bool replaceIfExisting )
      {
         if (!KtmTransactionHandle.IsAvailable)
         {
            XMoveFile.MoveFileEx(fileSource, fileTarget, replaceIfExisting);
         }
         else
         {
            XMoveFile.MoveFileFlags dwFlags = (replaceIfExisting ? XMoveFile.MoveFileFlags.MOVEFILE_REPLACE_EXISTING : 0);
            dwFlags |= XMoveFile.MoveFileFlags.MOVEFILE_COPY_ALLOWED;
            // No need for MOVEFILE_WRITE_THROUGH, as this just slows the transaction down.
            // dwFlags |= XMoveFile.MoveFileFlags.MOVEFILE_WRITE_THROUGH;

            int errorCode = 0;
            using (KtmTransactionHandle ktmScope = KtmTransactionHandle.CreateKtmTransactionHandle())
               if (!MoveFileTransactedW(fileSource, fileTarget, IntPtr.Zero, IntPtr.Zero, dwFlags, ktmScope))
                  errorCode = Marshal.GetLastWin32Error();
            switch (errorCode)
            {
               case 0: // All is good :-)
                  break;
               case 6832:  // Not Allowed to use transaction on this file !
                  XMoveFile.MoveFileEx(fileSource, fileTarget, replaceIfExisting);
                  break;
               default:
                  Log.Error("MoveFileTransactedW threw [{0}]", errorCode);
                  throw new System.ComponentModel.Win32Exception(errorCode);
            }
         }
      }

#region DllImports
      // ReSharper disable InconsistentNaming
#pragma warning disable 169
      // From WinError.h -> http://msdn.microsoft.com/en-us/library/ms819773.aspx
      public const int ERROR_PATH_NOT_FOUND = 3;  // MessageText: The system cannot find the path specified.
#pragma warning restore 169
      // ReSharper restore InconsistentNaming

      [DllImport("kernel32.dll", EntryPoint = "MoveFileTransacted", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool MoveFileTransactedW(
          [In] string lpExistingFileName,
          [In] string lpNewFileName,
          [In] IntPtr lpProgressRoutine,
          [In] IntPtr lpData,
          [In] XMoveFile.MoveFileFlags dwFlags,
          [In] SafeHandle hTransaction
      );

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CreateDirectoryTransactedW(
         [In] string lpTemplateDirectory, 
         [In] string lpNewDirectory,
         [In] IntPtr lpSecurityAttributes, 
         SafeHandle hTransaction
         );

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool RemoveDirectoryTransactedW([In] string lpPathName, [In] SafeHandle hTransaction);

#endregion
   }
}