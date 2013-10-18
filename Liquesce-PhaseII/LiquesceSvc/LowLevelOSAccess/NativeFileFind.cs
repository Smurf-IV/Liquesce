//
// Stolen and modified from http://pinvoke.net/default.aspx/FindFirstFile
// 2013 Added the Alternative stream finding as well.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using NLog;

namespace LiquesceSvc
{
   internal class NativeFileFind
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      public const char StreamSeparator = ':';

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

      // TODO: Need a away to make the IEnumberable, so that large dir counts do not get bogged down.
      // yield return WIN32_FIND_DATA
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
            //if (Log.IsTraceEnabled)
            //   info2.dwFileAttributes |= (uint) NativeFileOps.EFileAttributes.Offline;
            files[info2.cFileName] = info2;
         }
      }

      #region Alternative Stream stuff

      // "Characters whose integer representations are in the range from 1 through 31, 
      // except for alternate streams where these characters are allowed"
      // http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
      private static readonly char[] InvalidStreamNameChars = Path.GetInvalidFileNameChars().Where(c => c < 1 || c > 31).ToArray();

// ReSharper disable once UnusedParameter.Local
      private static void ValidateStreamName(string streamName)
      {
         if (!string.IsNullOrEmpty(streamName) 
            && (-1 != streamName.IndexOfAny(InvalidStreamNameChars))
            )
         {
            // ERROR_INVALID_NAME = 123
            throw new Win32Exception(123, "The stream name contains invalid characters");
         }
      }

      public static List<AlternateNativeInfo> ListAlternateDataStreams(SafeFileHandle hFile)
      {
         List<AlternateNativeInfo> result = new List<AlternateNativeInfo>();

         using (AlternateNameWrapper alternateName = new AlternateNameWrapper())
         {
            if (!hFile.IsInvalid)
            {
               WIN32_STREAM_ID streamId = new WIN32_STREAM_ID();
               UInt32 dwStreamHeaderSize = (uint) Marshal.SizeOf(streamId);
               bool finished = false;
               IntPtr context = IntPtr.Zero;
               UInt32 bytesRead;

               try
               {
                  while (!finished)
                  {
                     // Read the next stream header:
                     if (!BackupRead(hFile, ref streamId, dwStreamHeaderSize, out bytesRead, false, false, ref context)
                        || (dwStreamHeaderSize != bytesRead)
                        )
                     {
                        finished = true;
                     }
                     else
                     {
                        // Read the stream name:
                        string foundStreamName = string.Empty;
                        if (0 != streamId.dwStreamNameSize)
                        {
                           alternateName.EnsureCapacity(streamId.dwStreamNameSize);
                           if (!BackupRead(hFile, alternateName.MemoryBlock, streamId.dwStreamNameSize, out bytesRead, false, false, ref context))
                           {
                              foundStreamName = null;
                              finished = true;
                           }
                           else
                           {
                              // Unicode chars are 2 bytes:
                              foundStreamName = alternateName.ReadAlternateStreamName(bytesRead >> 1);
                           }
                        }

                        // Add the stream info to the result:
                        if (!string.IsNullOrEmpty(foundStreamName))
                        {
                           result.Add(new AlternateNativeInfo
                           {
                              StreamType = (FileStreamType)streamId.dwStreamType,
                              StreamAttributes = (FileStreamAttributes)streamId.dwStreamAttributes,
                              StreamSize = streamId.Size,
                              StreamName = foundStreamName
                           });
                        }

                        // Skip the contents of the stream:
                        uint bytesSeekedLow;
                        uint bytesSeekedHigh;
                        if (!finished 
                           && !BackupSeek(hFile, (uint)(streamId.Size & 0xFFFFFFFF), (uint)(streamId.Size >> 32), out bytesSeekedLow, out bytesSeekedHigh, ref context)
                           )
                        {
                           finished = true;
                        }
                     }
                  }
               }
               finally
               {
                  // Abort the backup:
                  BackupRead(hFile, alternateName.MemoryBlock, 0, out bytesRead, true, false, ref context);
               }
            }
         }

         return result;
      }

      #endregion

      /// <summary>
      /// Stolen and modified from http://pinvoke.net/default.aspx/FindFirstFile 
      /// </summary>
      internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
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
      internal static extern SafeFindHandle FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

      [DllImport("kernel32.dll", SetLastError = true)]
      static extern bool FindClose(SafeHandle hFindFile);

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      static extern bool FindNextFile(SafeHandle hFindFile, out WIN32_FIND_DATA lpFindFileData);

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool BackupRead( SafeFileHandle hFile, ref WIN32_STREAM_ID pBuffer,
         [In] UInt32 numberOfBytesToRead, out UInt32 numberOfBytesRead,
         [In] [MarshalAs(UnmanagedType.Bool)] bool abort,
         [In] [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
         ref IntPtr context);

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool BackupRead(SafeFileHandle hFile, SafeHGlobalHandle pBuffer,
         [In] UInt32 numberOfBytesToRead, out UInt32 numberOfBytesRead,
         [In] [MarshalAs(UnmanagedType.Bool)] bool abort,
         [In] [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
         ref IntPtr context);

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      static extern bool BackupSeek(SafeFileHandle hFile, 
         [In] UInt32 bytesToSeekLow, [In] UInt32 bytesToSeekHigh,
         out UInt32 bytesSeekedLow,
         out UInt32 bytesSeekedHigh,
         [In] ref IntPtr context);
   }
}
