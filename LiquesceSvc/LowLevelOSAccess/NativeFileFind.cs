//
// Stolen and modified from http://pinvoke.net/default.aspx/FindFirstFile
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using NLog;

namespace LiquesceSvc
{
   internal class NativeFileFind
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// stolen from http://stackoverflow.com/questions/755574/how-to-quickly-check-if-folder-is-empty-net
      /// And then some changes made.
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      public static bool IsDirEmpty(string path)
      {
         bool maybeEmpty = true;
         Log.Trace("IsDirEmpty IN pathAndPattern[{0}]", path);
         try
         {
            char endChar = path[path.Length - 1];
            if (endChar != Path.DirectorySeparatorChar)
               path += Path.DirectorySeparatorChar;
            path += '*';

            WIN32_FIND_DATA findData;
            using (SafeFindHandle findHandle = FindFirstFile(path, out findData))
            {
               do
               {
                  maybeEmpty = ((findData.cFileName == ".") 
                     || (findData.cFileName == "..")
                     );
               } while (maybeEmpty 
                  && FindNextFile(findHandle, out findData)
                  );
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("IsDirEmpty threw: ", ex);
            maybeEmpty = false;
         }
         return maybeEmpty;
      }

      public static bool FindFirstOnly(string pathAndPattern, ref WIN32_FIND_DATA findData)
      {
         bool success = false;
         Log.Trace("FindFirstOnly IN pathAndPattern[{0}]", pathAndPattern);
         try
         {
            using (SafeFindHandle findHandle = FindFirstFile(pathAndPattern, out findData))
            {
               success = !findHandle.IsInvalid;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("FindFirstOnly threw: ", ex);
            success = false;
         }
         return success;
      }

      public static void AddFiles(string path, Dictionary<string, WIN32_FIND_DATA> files, string pattern)
      {
         Log.Trace("AddFiles IN path[{0}] pattern[{1}]", path, pattern);
         try
         {
            WIN32_FIND_DATA findData;

            // please note that the following line won't work if you try this on a network folder, like \\Machine\C$
            // simply remove the \\?\ part in this case or use \\?\UNC\ prefix
            // FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
            using (SafeFindHandle findHandle = FindFirstFile(NativeFileOps.CombineNoChecks(path, pattern), out findData))
            {
               if (!findHandle.IsInvalid)
               {
                  do
                  {
                     AddToUniqueLookup(findData, files);
                  }
                  while (FindNextFile(findHandle, out findData));
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
            throw;
         }
      }

      private static void AddToUniqueLookup(WIN32_FIND_DATA info2, Dictionary<string, WIN32_FIND_DATA> files)
      {
         if (!string.IsNullOrWhiteSpace(info2.cFileName))
         {
            // Prevent expensive time spent allowing indexing == FileAttributes.NotContentIndexed
            info2.dwFileAttributes |= (uint) NativeFileOps.EFileAttributes.NotContentIndexed;
            // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
            if (Log.IsTraceEnabled)
               info2.dwFileAttributes |= (uint) NativeFileOps.EFileAttributes.Offline;
            files[info2.cFileName] = info2;
         }
      }


      /// <summary>
      /// Stolen and modified from http://pinvoke.net/default.aspx/FindFirstFile 
      /// </summary>
      private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
      {
         // Methods
         [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
         internal SafeFindHandle()
            : base(true)
         {
         }

         public SafeFindHandle(IntPtr preExistingHandle, bool ownsHandle)
            : base(ownsHandle)
         {
            base.SetHandle(preExistingHandle);
         }

         protected override bool ReleaseHandle()
         {
            if (!(IsInvalid || IsClosed))
            {
               return FindClose(this);
            }
            return (IsInvalid || IsClosed);
         }

         protected override void Dispose(bool disposing)
         {
            if (!(IsInvalid || IsClosed))
            {
               FindClose(this);
            }
            base.Dispose(disposing);
         }
      }

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      static extern SafeFindHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

      [DllImport("kernel32.dll", SetLastError = true)]
      static extern bool FindClose(SafeHandle hFindFile);

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      static extern bool FindNextFile(SafeHandle hFindFile, out WIN32_FIND_DATA lpFindFileData);

   }
}
