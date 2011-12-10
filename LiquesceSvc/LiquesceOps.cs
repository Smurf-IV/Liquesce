#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LiquesceOps.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.Security.AccessControl;
using System.Text;
using System.Threading;

using DokanNet;
using LiquesceFacade;
using NLog;
using PID = LiquesceSvc.ProcessIdentity;

namespace LiquesceSvc
{
   internal class LiquesceOps : IDokanOperations
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      // currently open files...
      // last key
      static private UInt64 openFilesLastKey;
      // lock
      private readonly ReaderWriterLockSlim openFilesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      // dictionary of all open files
      private readonly Dictionary<UInt64, NativeFileOps> openFiles = new Dictionary<UInt64, NativeFileOps>();

      private readonly Roots roots;
      private readonly ConfigDetails configDetails;
      private readonly ShellChangeNotify notifyOf;

      public LiquesceOps(ConfigDetails configDetails)
      {
         this.configDetails = configDetails;
         roots = new Roots(configDetails); // Already been trimmed in ReadConfigDetails()
         notifyOf = new ShellChangeNotify(roots);
      }

      #region IDokanOperations Implementation

      /// <summary>
      /// The information given in the Dokan info is a bit misleading about the return codes
      /// This is what the Win OS suystem is expecting http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx
      /// So.. Everything succeeds but the Return code is ERROR_ALREADY_EXISTS
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawFlagsAndAttributes"></param>
      /// <param name="info"></param>
      /// <param name="rawAccessMode"></param>
      /// <param name="rawShare"></param>
      /// <param name="rawCreationDisposition"></param>
      /// <returns></returns>
      public int CreateFile(string filename, uint rawAccessMode, uint rawShare, uint rawCreationDisposition, uint rawFlagsAndAttributes, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            // TODO: Dump rawAccessMode out in hex to max it easier to decode :-)
            Log.Debug( "CreateFile IN filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}|{5}], ProcessId[{6}]",
              filename, rawAccessMode, (FileShare)rawShare, (FileMode)rawCreationDisposition, (rawFlagsAndAttributes&0xFFFE0000), (FileAttributes)(rawFlagsAndAttributes&0x0001FFFF), info.ProcessId);
            bool createNew = (rawCreationDisposition == Proxy.CREATE_NEW) || (rawCreationDisposition == Proxy.CREATE_ALWAYS);
            NativeFileOps foundFileInfo = roots.GetPath(filename,  (!createNew?0:configDetails.HoldOffBufferBytes));

            // Increment now in case there is an exception later
            using (openFilesSync.WriteLock())
               ++openFilesLastKey; // never be Zero !
            string fullName = foundFileInfo.FullName;
            PID.Invoke(info.ProcessId, delegate
            {
               if (createNew)
               {
                  // force it to be "Looked up" next time
                  roots.RemoveFromLookup(filename);
                  NativeFileOps.CreateDirectory( foundFileInfo.DirectoryPathOnly );
               }

               // TODO: The DokanFileInfo structure has the following extra things that need to be mapped tothe file open
               //public bool PagingIo;
               //public bool SynchronousIo;
               //public bool Nocache;
               // See http://msdn.microsoft.com/en-us/library/aa363858%28VS.85%29.aspx#caching_behavior
               if (info.PagingIo)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_RANDOM_ACCESS;
               if (info.Nocache)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_WRITE_THROUGH; // | Proxy.FILE_FLAG_NO_BUFFERING;
               // FILE_FLAG_NO_BUFFERING flag requires that all I/O operations on the file handle be in multiples of the sector size, 
               // AND that the I/O buffers also be aligned on addresses which are multiples of the sector size
               if (info.SynchronousIo)
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_SEQUENTIAL_SCAN;
               if (foundFileInfo.IsDirectory)
               {
                  info.IsDirectory = true;
                  rawFlagsAndAttributes |= Proxy.FILE_FLAG_BACKUP_SEMANTICS;
               }

               Log.Debug("Modified rawFlagsAndAttributes[{0}]", rawFlagsAndAttributes);

               NativeFileOps fs = NativeFileOps.CreateFile(fullName, rawAccessMode, rawShare,
                                                   rawCreationDisposition, rawFlagsAndAttributes);
               
               // If a specified file exists before the function call and dwCreationDisposition is CREATE_ALWAYS 
               // or OPEN_ALWAYS, a call to GetLastError returns ERROR_ALREADY_EXISTS, even when the function succeeds.
               //if (createNew)
               //   dokanReturn = Marshal.GetLastWin32Error() * -1;
               // It's not gone boom, so it must be okay..
               using (openFilesSync.WriteLock())
               {
                  info.refFileHandleContext = openFilesLastKey; // Incremented above
                  openFiles.Add(openFilesLastKey, fs);
               }
            });
            notifyOf.CreateFile(fullName, info.refFileHandleContext, createNew);
         }
         catch (Exception ex)
         {
            Log.Error("CreateFile filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}|{5}], ProcessId[{6}]",
              filename, rawAccessMode, (FileShare)rawShare, (FileMode)rawCreationDisposition, (rawFlagsAndAttributes&0xFFFE0000), (FileAttributes)(rawFlagsAndAttributes&0x0001FFFF), info.ProcessId);
            Log.ErrorException("CreateFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("CreateFile OUT dokanReturn=[{0}] context[{1}]", dokanReturn, openFilesLastKey);
            if (dokanReturn != Dokan.DOKAN_SUCCESS)
            {
               // force it to be "Looked up" next time
               roots.RemoveFromLookup(filename);
            }
         }
         return dokanReturn;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("OpenDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            NativeFileOps foundDirInfo = roots.GetPath(filename);
            PID.Invoke(info.ProcessId, delegate
            {
               if (foundDirInfo.Exists
                  && foundDirInfo.IsDirectory
                  )
               {
                  info.IsDirectory = true;
                  using (openFilesSync.WriteLock())
                     info.refFileHandleContext = ++openFilesLastKey; // never be Zero !
                  ShellChangeNotify.OpenDirectory(foundDirInfo.FullName, info.refFileHandleContext);
                  dokanReturn = Dokan.DOKAN_SUCCESS;
               }
               else
                  dokanReturn = Dokan.ERROR_PATH_NOT_FOUND;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("OpenDirectory threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("OpenDirectory OUT. dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }


      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;

         try
         {
            Log.Debug("CreateDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            PID.Invoke(info.ProcessId, delegate
            {
               NativeFileOps foundDirInfo = roots.GetPath(filename);
               foundDirInfo.CreateDirectory();
               Log.Debug("By the time it gets here the dir should exist, or have existed by another method / thread");
               info.IsDirectory = true;
               roots.TrimAndAddUnique(foundDirInfo);
               notifyOf.CreateDirectory(foundDirInfo.FullName, info.refFileHandleContext);
               dokanReturn = Dokan.DOKAN_SUCCESS;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateDirectory threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("CreateDirectory OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      /*
      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
      If the file system application stored file handle in the refFileHandleContext variable when the function CreateFile is invoked, 
      this should be closed in the Cleanup function, not in CloseFile function. If the user application calls CloseHandle
      and subsequently open the same file, the CloseFile function of the file system application may not be invoked 
      before the CreateFile API is called. This may cause sharing violation error. 
      Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      complete the I/O operations. The file system application should also properly work in this case.
      */
      /// <summary>
      /// When info->DeleteOnClose is true, you must delete the file in Cleanup.
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int Cleanup(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("Cleanup IN DokanProcessId[{0}] with filename [{1}] handle[{2}] isDir[{3}]", info.ProcessId, filename, info.refFileHandleContext, info.IsDirectory);
            NativeFileOps foundInfo = roots.GetPath(filename);
            CloseAndRemove(info);
            PID.Invoke(info.ProcessId, delegate
            {
               if (info.DeleteOnClose)
               {
                  if (info.IsDirectory)
                  {
                     Log.Trace("DeleteOnClose Directory");
                     roots.DeleteDirectory(filename);
                     notifyOf.DeleteDirectory(foundInfo.FullName, info.refFileHandleContext);
                  }
                  else
                  {
                     roots.DeleteFile(filename);
                     notifyOf.DeleteFile(foundInfo.FullName, info.refFileHandleContext);
                  }
               }
            });
            notifyOf.Cleanup(foundInfo.FullName, info.refFileHandleContext);
            info.refFileHandleContext = 0;
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cleanup threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("Cleanup OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("CloseFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            CloseAndRemove(info);
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CloseFile threw: ", ex);
            dokanReturn =  Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("CloseFile OUT dokanReturn[{0]}", dokanReturn);
         }
         return dokanReturn;
      }

      /// <summary>
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawBuffer"></param>
      /// <param name="rawBufferLength"></param>
      /// <param name="rawReadLength"></param>
      /// <param name="rawOffset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      /// <remarks>
      // Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      // complete the I/O operations. The file system application should also properly work in this case.      
      /// </remarks>
      public int ReadFileNative(string filename, IntPtr rawBuffer, uint rawBufferLength, ref uint rawReadLength, long rawOffset, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         bool closeOnReturn = false;
         NativeFileOps fileStream = null;
         try
         {
            Log.Debug("ReadFile [{0}] IN offset=[{2}] DokanProcessId[{1}]", filename, info.ProcessId, rawOffset);
            rawReadLength = 0;
            Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            using (openFilesSync.ReadLock())
            {
               openFiles.TryGetValue(info.refFileHandleContext, out fileStream);
            }
            if ((fileStream == null)
               || fileStream.IsInvalid
               )
            {
               NativeFileOps fsi = roots.GetPath(filename);
               PID.Invoke(info.ProcessId, delegate
                  {
                     Log.Warn("No context handle for [{0}]", fsi.FullName);
                  const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = info.IsDirectory
                                                  ? Proxy.FILE_FLAG_BACKUP_SEMANTICS
                                                  : 0;
                  fileStream = NativeFileOps.CreateFile(fsi.FullName, rawAccessMode, rawShare,
                                                   rawCreationDisposition, rawFlagsAndAttributes);
                     closeOnReturn = true;
                  }); 
            }

            // Some programs the file offset to extend the file length to write past the end of the file
            // Commented the check of rawOffset being off the size of the file.
            //if (rawOffset > fileStream.Length)
            //{
            //   errorCode = Dokan.DOKAN_ERROR;
            //}
            //else
            //{
            // Use the current offset as a check first to speed up access in large sequential file reads

            fileStream.SetFilePointer(rawOffset, SeekOrigin.Begin);
            if (0 == fileStream.ReadFile(rawBuffer, rawBufferLength, out rawReadLength))
            {
               throw new System.ComponentModel.Win32Exception();
            }
            ShellChangeNotify.ReadFileNative(fileStream.FullName, info.refFileHandleContext);
         }
         catch (Exception ex)
         {
            Log.ErrorException("ReadFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (closeOnReturn
                  && (fileStream != null)
                  )
                  fileStream.Close();
            }
            catch (Exception ex)
            {
               Log.ErrorException("closeOnReturn threw: ", ex);
            }
            Log.Debug("ReadFile OUT readBytes=[{0}], dokanReturn[{1}]", rawReadLength, dokanReturn);
         }
         return dokanReturn;
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="rawBuffer"></param>
      /// <param name="rawNumberOfBytesToWrite"></param>
      /// <param name="rawNumberOfBytesWritten"></param>
      /// <param name="rawOffset"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      /// <remarks>
      // Note: when user uses memory mapped file, WriteFile or ReadFile function may be invoked after Cleanup in order to 
      // complete the I/O operations. The file system application should also properly work in this case.      
      /// </remarks>
      public int WriteFileNative(string filename, IntPtr rawBuffer, uint rawNumberOfBytesToWrite, ref uint rawNumberOfBytesWritten, long rawOffset, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         rawNumberOfBytesWritten = 0;
         NativeFileOps fileStream = null;
         bool closeOnReturn = false;
         try
         {
            Log.Debug("WriteFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            if (info.refFileHandleContext != 0)
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               using (openFilesSync.ReadLock())
               {
                  openFiles.TryGetValue(info.refFileHandleContext, out fileStream);
               }
            }
            if ( (fileStream == null)
               || fileStream.IsInvalid
               )
            {
               NativeFileOps fsi = roots.GetPath(filename);
               PID.Invoke(info.ProcessId, delegate
               {
                  Log.Warn("No context handle for [{0}]", fsi.FullName);
                  const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = info.IsDirectory
                                                   ? Proxy.FILE_FLAG_BACKUP_SEMANTICS
                                                   : 0;
                  fileStream = NativeFileOps.CreateFile(fsi.FullName, rawAccessMode, rawShare,
                                                rawCreationDisposition, rawFlagsAndAttributes );
                  closeOnReturn = true;
               });
            }

            if (info.WriteToEndOfFile)//  If true, write to the current end of file instead of Offset parameter.
               fileStream.SetFilePointer( 0, SeekOrigin.End);
            else
               // Use the current offset as a check first to speed up access in large sequential file reads
               fileStream.SetFilePointer(rawOffset, SeekOrigin.Begin);

            if (0 == fileStream.WriteFile(rawBuffer, rawNumberOfBytesToWrite, out rawNumberOfBytesWritten))
            {
               throw new System.ComponentModel.Win32Exception();
            }
            notifyOf.WriteFileNative(fileStream.FullName, info.refFileHandleContext);
         }
         catch (Exception ex)
         {
            Log.ErrorException("WriteFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (closeOnReturn
                  && (fileStream != null)
                  )
                  fileStream.Close();
            }
            catch (Exception ex)
            {
               Log.ErrorException("closeOnReturn threw: ", ex);
            }
            Log.Debug("WriteFile OUT Written[{0}] dokanReturn[{1}]", rawNumberOfBytesWritten, dokanReturn);
         }
         return dokanReturn;
      }


      public int FlushFileBuffersNative(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.ERROR_PATH_NOT_FOUND;
         try
         {
            Log.Debug("FlushFileBuffers IN [{0}] DokanProcessId[{1}]", filename, info.ProcessId);
            Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            using (openFilesSync.ReadLock())
            {
               NativeFileOps fileStream;
               if (openFiles.TryGetValue(info.refFileHandleContext, out fileStream))
               {
                  fileStream.FlushFileBuffers();
                  ShellChangeNotify.FlushFileBuffers(fileStream.FullName, info.refFileHandleContext);
                  dokanReturn = Dokan.DOKAN_SUCCESS;
               }
               else
               {
                  dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("FlushFileBuffers OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int GetFileInformationNative(string filename, ref BY_HANDLE_FILE_INFORMATION lpFileInformation, DokanFileInfo info)
      {
         int dokanReturn;
         NativeFileOps stream = null;
         bool needToClose = false;
         try
         {
            Log.Debug("GetFileInformationNative [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);

            using (openFilesSync.ReadLock())
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               openFiles.TryGetValue(info.refFileHandleContext, out stream);
            }
            if ( (stream == null)
               || stream.IsInvalid
               )
            {
               NativeFileOps fsi = roots.GetPath(filename);
               // Now attempt workaround for issue "http://code.google.com/p/dokan/issues/detail?id=239" 
               if (!fsi.Exists
                  && ProcessIdentity.CouldBeSMB(info.ProcessId)
                  )
                  fsi = roots.GetPathRelatedtoShare(filename);
               PID.Invoke(info.ProcessId, delegate
               {
                  const uint rawAccessMode = Proxy.GENERIC_READ/* | Proxy.GENERIC_WRITE*/;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = info.IsDirectory
                                                  ? Proxy.FILE_FLAG_BACKUP_SEMANTICS
                                                  : 0;
                  stream = NativeFileOps.CreateFile(fsi.FullName, rawAccessMode, rawShare,
                                               rawCreationDisposition, rawFlagsAndAttributes);
                  needToClose = true;
                });
            }

            if ((stream != null)
                  && !stream.IsInvalid
               )
            {
               stream.GetFileInformationByHandle(ref lpFileInformation);
               ShellChangeNotify.GetFileInformationNative(stream.FullName, info.refFileHandleContext);
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
            else
            {
               throw new System.ComponentModel.Win32Exception();
            }
            if (Log.IsTraceEnabled)
            {
               StringBuilder fileInfo = new StringBuilder("lpFileInformation=");
               fileInfo.AppendLine().AppendFormat("dwFileAttributes[{0}]", lpFileInformation.dwFileAttributes);
               fileInfo.AppendLine().AppendFormat("ftCreationTime[{0}]", NativeFileOps.ConvertFileTimeToDateTime(lpFileInformation.ftCreationTime));
               fileInfo.AppendLine().AppendFormat("ftLastAccessTime[{0}]", NativeFileOps.ConvertFileTimeToDateTime(lpFileInformation.ftLastAccessTime));
               fileInfo.AppendLine().AppendFormat("ftLastWriteTime[{0}]", NativeFileOps.ConvertFileTimeToDateTime(lpFileInformation.ftLastWriteTime));
               fileInfo.AppendLine().AppendFormat("dwVolumeSerialNumber[{0}]", lpFileInformation.dwVolumeSerialNumber);
               fileInfo.AppendLine().AppendFormat("nFileSizeHigh[{0}]", lpFileInformation.nFileSizeHigh);
               fileInfo.AppendLine().AppendFormat("nFileSizeLow[{0}]", lpFileInformation.nFileSizeLow);
               fileInfo.AppendLine().AppendFormat("dwNumberOfLinks[{0}]", lpFileInformation.dwNumberOfLinks);
               fileInfo.AppendLine().AppendFormat("nFileIndexHigh[{0}]", lpFileInformation.nFileIndexHigh);
               fileInfo.AppendLine().AppendFormat("nFileIndexLow[{0}]", lpFileInformation.nFileIndexLow);
               Log.Trace(fileInfo);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileInformationNative threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (needToClose
                   && (stream != null)
                   && !stream.IsInvalid
                  )
               {
                  stream.Close();
               }
            }
            catch
            {
            }
         }
         Log.Debug("GetFileInformation OUT dokanReturn[{0}]", dokanReturn);
         return dokanReturn;
      }

      public int FindFilesWithPattern(string filename, string pattern, out WIN32_FIND_DATA[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files, pattern);
      }

      public int FindFiles(string filename, out WIN32_FIND_DATA[] files, DokanFileInfo info)
      {
         return FindFiles(filename, info.ProcessId, out files);
      }

      private int FindFiles(string filename, uint processId, out WIN32_FIND_DATA[] files, string pattern = "*")
      {
         int dokanReturn;
         files = null;
         try
         {
            Log.Debug("FindFiles IN [{0}], pattern[{1}]", filename, pattern);
            Dictionary<string, WIN32_FIND_DATA> uniqueFiles = new Dictionary<string, WIN32_FIND_DATA>();
            //uniqueFiles.Add(".",
            // FindFiles should not return an empty array. It should return parent ("..") and self ("."). That wasn't obvious!
            PID.Invoke(processId, delegate
            {
               // Do this in reverse, so that the preferred refreences overwrite the older files
               for (int i = configDetails.SourceLocations.Count - 1; i >= 0; i--)
               {
                  NativeFileFind.AddFiles(configDetails.SourceLocations[i] + filename, uniqueFiles, pattern);
               }
            });
            // If these are not found then the loop speed of a "failed remove" and "not finding" is the same !
            uniqueFiles.Remove(@"System Volume Information");
            uniqueFiles.Remove(@"$RECYCLE.BIN");
            uniqueFiles.Remove(@"Recycle Bin");
            files = new WIN32_FIND_DATA[uniqueFiles.Values.Count];
            uniqueFiles.Values.CopyTo(files, 0);
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("FindFiles threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
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
         return dokanReturn;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         int dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
         try
         {
            Log.Debug("SetFileAttributes [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            NativeFileOps stream = null;
            using (openFilesSync.ReadLock())
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               openFiles.TryGetValue(info.refFileHandleContext, out stream);
            }
            if ((stream == null)
               || stream.IsInvalid
               )
            {
               NativeFileOps foundFileInfo = roots.GetPath(filename);
               if (foundFileInfo.Exists)
               {
                  PID.Invoke(info.ProcessId, delegate
                                                {
                                                   // This uses  if (!Win32Native.SetFileAttributes(fullPathInternal, (int) fileAttributes))
                                                   // And can throw PathTOOLong
                                                   foundFileInfo.SetFileAttributes(attr);
                                                });
                  notifyOf.SetFileAttributes(foundFileInfo.FullName, info.refFileHandleContext);
               }
            }
            else
            {
               stream.SetFileAttributes(attr);
               notifyOf.SetFileAttributes(stream.FullName, info.refFileHandleContext);
            }
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileAttributes threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("SetFileAttributes OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetFileTimeNative(string filename, ref WIN32_FIND_FILETIME rawCreationTime, ref WIN32_FIND_FILETIME rawLastAccessTime,
          ref WIN32_FIND_FILETIME rawLastWriteTime, DokanFileInfo info)
      {
         int dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
         NativeFileOps stream = null;
         bool needToClose = false;
         try
         {
            Log.Debug("SetFileTime [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);

            using (openFilesSync.ReadLock())
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               openFiles.TryGetValue(info.refFileHandleContext, out stream);
            }
            if ( (stream == null) 
               || stream.IsInvalid 
               )
            {
               NativeFileOps fsi = roots.GetPath(filename);
               if (fsi.IsDirectory)
                  info.IsDirectory = true;
               PID.Invoke(info.ProcessId, delegate
               {
                  // Workaround the dir set
                  // ERROR LiquesceSvc.LiquesceOps: SetFileTime threw:  System.UnauthorizedAccessException: Access to the path 'G:\_backup\Kylie Minogue\Dir1' is denied.
                  // To create a handle to a directory, you have to use FILE_FLAG_BACK_SEMANTICS.
                  const uint rawAccessMode = Proxy.GENERIC_READ | Proxy.GENERIC_WRITE;
                  const uint rawShare = Proxy.FILE_SHARE_READ | Proxy.FILE_SHARE_WRITE;
                  const uint rawCreationDisposition = Proxy.OPEN_EXISTING;
                  uint rawFlagsAndAttributes = info.IsDirectory
                                                   ? Proxy.FILE_FLAG_BACKUP_SEMANTICS
                                                   : 0;
                  stream = NativeFileOps.CreateFile(fsi.FullName, rawAccessMode, rawShare,
                                                rawCreationDisposition, rawFlagsAndAttributes);
                  needToClose = true;
               });
            }
            if ((stream != null)
                  && !stream.IsInvalid
               )
            {
               stream.SetFileTime(ref rawCreationTime, ref rawLastAccessTime, ref rawLastWriteTime);
               notifyOf.SetFileTimeNative(stream.FullName, info.refFileHandleContext);
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
            else
            {
               dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileTime threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            try
            {
               if (needToClose
                   && (stream != null)
                   && !stream.IsInvalid
                  )
               {
                  stream.Close();
               }
            }
            catch
            {
            }
            Log.Debug("SetFileTime OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      /// <summary>
      /// Because of how Dokan works:
      /// You should not delete file on DeleteFile or DeleteDirectory.
      // When DeleteFile or DeleteDirectory, you must check whether
      // you can delete or not, and return 0 (when you can delete it)
      // or appropriate error codes such as -ERROR_DIR_NOT_EMPTY,
      // -ERROR_SHARING_VIOLATION.
      // When you return 0 (ERROR_SUCCESS), you get Cleanup with
      // FileInfo->DeleteOnClose set TRUE, you delete the file.
      //
      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int DeleteFile(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("DeleteFile [{0}] IN DokanProcessId[{1}], refFileHandleContext[{2}]", filename, info.ProcessId, info.refFileHandleContext);
            NativeFileOps stream;
            using (openFilesSync.ReadLock())
            {
               openFiles.TryGetValue(info.refFileHandleContext, out stream);
            }
            if (stream != null)
               dokanReturn = Dokan.DOKAN_SUCCESS;
            else
            PID.Invoke(info.ProcessId, delegate
            {
               NativeFileOps foundFileInfo = roots.GetPath(filename);
               dokanReturn = (foundFileInfo.Exists && !foundFileInfo.IsDirectory)
                                ? Dokan.DOKAN_SUCCESS
                                : Dokan.ERROR_FILE_NOT_FOUND;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("DeleteFile OUT dokanReturn[(0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("DeleteDirectory [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            PID.Invoke(info.ProcessId, delegate
            {
               NativeFileOps dirInfo = roots.GetPath(filename);
               if (dirInfo.Exists
                   && dirInfo.IsDirectory
                  )
               {
                  dokanReturn = dirInfo.IsEmptyDirectory ? Dokan.DOKAN_SUCCESS : Dokan.ERROR_DIR_NOT_EMPTY;
               }
               else
                  dokanReturn = Dokan.ERROR_PATH_NOT_FOUND;
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("DeleteDirectory threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("DeleteDirectory OUT dokanReturn[(0}]", dokanReturn);
         }

         return dokanReturn;
      }

      public int MoveFile(string dokanFilename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("MoveFile [{0}] to [{1}] IN DokanProcessId[{2}] context[{2}]", dokanFilename, newname, info.ProcessId, info.refFileHandleContext);
            if (dokanFilename == newname) // This is some weirdness that SyncToy tries to pull !!
            {
               return (dokanReturn = Dokan.DOKAN_SUCCESS);
            }

            NativeFileOps nfo = roots.GetPath(dokanFilename);
            if (!nfo.Exists)
               dokanReturn = nfo.IsDirectory ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
            else
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               NativeFileOps stream;
               using (openFilesSync.WriteLock())
               {
                  openFiles.TryGetValue(info.refFileHandleContext, out stream);
               }
               if (stream != null)
               {
                  stream.Close();
                  // In order to be opened the security must have been passed so, do this quickly
                  XMoveFile.Move(roots, dokanFilename, newname, replaceIfExisting, info.IsDirectory, info.ProcessId);
               }
               else
               {
                  PID.Invoke(info.ProcessId, delegate
                  {
                     XMoveFile.Move(roots, dokanFilename, newname, replaceIfExisting, info.IsDirectory, info.ProcessId);
                  });
               }
               // If we get this far, then everything is probably ok (No exeptions thrown :-)
               notifyOf.MoveFile(nfo.FullName, roots.GetPath(newname).FullName, (nfo is DirectoryInfo), info.refFileHandleContext);
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("MoveFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("MoveFile OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("SetEndOfFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            PID.Invoke(info.ProcessId, delegate
            {
               Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
               dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
               using (openFilesSync.ReadLock())
               {
                  NativeFileOps stream;
                  if ( openFiles.TryGetValue(info.refFileHandleContext, out stream ) )
                  {
                     stream.SetLength(length);
                     notifyOf.SetEndOfFile(stream.FullName, info.refFileHandleContext);
                     dokanReturn = Dokan.DOKAN_SUCCESS;
                  }
               }
            });
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetEndOfFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("SetEndOfFile OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("SetAllocationSize [{0}] IN DokanProcessId[{0}]", filename, info.ProcessId);
            /*
            This property specifies the number of bytes that the file consumes on disk. Its value may be less than the Size if the file is 'sparse'.
             * The following paragraph is taken from SSH File Transfer Protocol draft-ietf-secsh-filexfer-10, part 7.4:

             * When present during file creation, the file SHOULD be created and the specified number of bytes preallocated. 
             * If the preallocation fails, the file should be removed (if it was created) and an error returned.
             * If this field is present during a setstat operation, the file SHOULD be extended or truncated to the specified size. 
             * The 'size' of the file may be affected by this operation. If the operation succeeds, the 'size' should be the minimum 
             * of the 'size' before the operation and the new 'allocation-size'.
             * Querying the 'allocation-size' after setting it MUST return a value that is greater-than or equal to the value set, 
             * but it MAY not return the precise value set.
             * If both 'size' and 'allocation-size' are set during a setstat operation, and 'allocation-size' is less than 'size', 
             * the server MUST return SSH_FX_INVALID_PARAMETER.
            */
            NativeFileOps stream;
            if (openFiles.TryGetValue(info.refFileHandleContext, out stream))
            {
               BY_HANDLE_FILE_INFORMATION lpFileInformation = new BY_HANDLE_FILE_INFORMATION();
               stream.GetFileInformationByHandle(ref lpFileInformation);
               long thisFileSize = (lpFileInformation.nFileSizeHigh << 32) + lpFileInformation.nFileSizeLow;
               if (thisFileSize < length)
               {
                  // Need to check that the source FullName drive has enough free space for this "Potential" allocation
                  NativeFileOps confirmSpace = roots.GetPath(filename, (ulong)(length - thisFileSize));
                  if (confirmSpace.FullName != stream.FullName)
                  {
                     Log.Warn(
                        "There is a problem, in the amount of space required to fullfill this request with respect to the amount of space originally allocated");
                     // TODO: Move the file, or return not enough space ??

                  }
               }
               stream.SetLength(length);
               notifyOf.SetAllocationSize(stream.FullName, info.refFileHandleContext);
               dokanReturn = Dokan.DOKAN_SUCCESS;
            }
            else
            {
               // Setting file pointers positions is done with open handles !
               dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("SetAllocationSize threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("SetAllocationSize OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("LockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;
            Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            using (openFilesSync.ReadLock())
            {
               NativeFileOps fileIO;
               if (openFiles.TryGetValue(info.refFileHandleContext, out fileIO))
               {
                  fileIO.LockFile(offset, length);
                  ShellChangeNotify.LockFile(fileIO.FullName, info.refFileHandleContext);
                  dokanReturn = Dokan.DOKAN_SUCCESS;
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("LockFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("LockFile OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("UnlockFile [{0}] IN DokanProcessId[{1}]", filename, info.ProcessId);
            dokanReturn = Dokan.ERROR_FILE_NOT_FOUND;

            Log.Trace("info.refFileHandleContext [{0}]", info.refFileHandleContext);
            using (openFilesSync.ReadLock())
            {
               NativeFileOps fileIO;
               if (openFiles.TryGetValue(info.refFileHandleContext, out fileIO))
               {
                  fileIO.UnlockFile(offset, length);
                  ShellChangeNotify.UnlockFile(fileIO.FullName, info.refFileHandleContext);
                  dokanReturn = Dokan.DOKAN_SUCCESS;

               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("UnLockFile OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("GetDiskFreeSpace IN DokanProcessId[{0}]", info.ProcessId);
            ulong localFreeBytesAvailable = 0, localTotalBytes = 0, localTotalFreeBytes = 0;
            configDetails.SourceLocations.ForEach(str =>
                                                     {
                                                        ulong num;
                                                        ulong num2;
                                                        ulong num3;
                                                        if (GetDiskFreeSpaceExW(str, out num, out num2, out num3))
                                                        {
                                                           localFreeBytesAvailable += num;
                                                           localTotalBytes += num2;
                                                           localTotalFreeBytes += num3;
                                                        }
                                                     });
            freeBytesAvailable = localFreeBytesAvailable;
            totalBytes = localTotalBytes;
            totalFreeBytes = localTotalFreeBytes;
            dokanReturn = Dokan.DOKAN_SUCCESS;
         }
         catch (Exception ex)
         {
            Log.ErrorException("UnlockFile threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Trace("GetDiskFreeSpace OUT dokanReturn[{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Warn("Unmount IN DokanProcessId[{0}]", info.ProcessId);
         using (openFilesSync.WriteLock())
         {
            foreach (NativeFileOps obj2 in openFiles.Values)
            {
               try
               {
                  if (obj2 != null)
                  {
                     obj2.Close();
                  }
               }
               catch (Exception ex)
               {
                  Log.InfoException("Unmount closing files threw: ", ex);
               }
            }
            openFiles.Clear();
         }
         //notifyOf.Unmount(foundDirInfo.FullName, info.refFileHandleContext);
         Log.Trace("Unmount out");
         return Dokan.DOKAN_SUCCESS;
      }

      // http://groups.google.com/group/dokan/browse_thread/thread/15c3f3cfd84c5ac1?pli=1
      // see http://code.google.com/p/dokan/issues/detail?id=209
      public int GetFileSecurityNative(string filename, ref SECURITY_INFORMATION rawRequestedInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, ref uint rawSecurityDescriptorLengthNeeded, DokanFileInfo info)
      {
         Log.Debug("GetFileSecurityNative [{0}] IN GetFileSecurity[{1}][{2}]", filename, info.ProcessId, rawRequestedInformation);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            return (dokanReturn = -120); // 
            SECURITY_INFORMATION reqInfo = rawRequestedInformation;
            byte[] managedDescriptor = null;
                  AccessControlSections includeSections = AccessControlSections.None;
                  if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
                     includeSections |= AccessControlSections.Owner;
                  if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
                     includeSections |= AccessControlSections.Group;
                  if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
                     includeSections |= AccessControlSections.Access;
                  if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
                     includeSections |= AccessControlSections.Audit;
            FileSystemSecurity pSD = null;
            NativeFileOps stream;
            using (openFilesSync.ReadLock())
            {
               openFiles.TryGetValue(info.refFileHandleContext, out stream);
            }
            string FullName;
            if (stream != null)
            {
               FullName = stream.FullName;
               pSD = (!stream.IsDirectory)? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                                                      : Directory.GetAccessControl(FullName, includeSections);
            }
            else
            {
               NativeFileOps foundFileInfo = roots.GetPath(filename);
               FullName = foundFileInfo.FullName;
               if (foundFileInfo.Exists)
               {
                  PID.Invoke(info.ProcessId, delegate
                                                {
                                                   pSD = (!foundFileInfo.IsDirectory) ? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                                                      : Directory.GetAccessControl(FullName, includeSections);
                                                });
               }
               else
               {
                  dokanReturn = info.IsDirectory ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
               }
            }
            if (pSD != null)
            {
               if (Log.IsTraceEnabled)
               {
                  string getFileSecurityNative = pSD.GetSecurityDescriptorSddlForm(includeSections);
                  Log.Trace("GetFileSecurityNative on {0} Retrieved [{1}]", FullName, getFileSecurityNative);
               }
               managedDescriptor = pSD.GetSecurityDescriptorBinaryForm();
            }
            if (managedDescriptor != null)
            {
               rawSecurityDescriptorLengthNeeded = (uint) managedDescriptor.Length;
               // if the buffer is not enough the we must pass the correct error
               // If the returned number of bytes is less than or equal to nLength, the entire security descriptor is returned in the output buffer; otherwise, none of the descriptor is returned.
               if (rawSecurityDescriptorLength < rawSecurityDescriptorLengthNeeded)
               {
                  dokanReturn = Dokan.ERROR_INSUFFICIENT_BUFFER;
               }
               else
               {
                  Marshal.Copy(managedDescriptor, 0, rawSecurityDescriptor, managedDescriptor.Length);
                  ShellChangeNotify.GetFileSecurityNative(FullName, info.refFileHandleContext);
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("GetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("GetFileSecurity OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      // see http://code.google.com/p/dokan/issues/detail?id=209
      public int SetFileSecurityNative(string filename, ref SECURITY_INFORMATION rawSecurityInformation, IntPtr /*ref SECURITY_DESCRIPTOR*/ rawSecurityDescriptor, uint rawSecurityDescriptorLength, DokanFileInfo info)
      {
         Log.Debug("SetFileSecurityNative IN [{0}] SetFileSecurity[{1}][{2}]", filename, info.ProcessId, rawSecurityInformation);
         int dokanReturn = Dokan.DOKAN_SUCCESS;
         try
         {
            return (dokanReturn = -120); // 50 = ERROR_NOT_SUPPORTED 120 = ERROR_CALL_NOT_IMPLEMENTED
            NativeFileOps foundFileInfo = roots.GetPath(filename);
            if (foundFileInfo.Exists)
            {
               SECURITY_INFORMATION reqInfo = rawSecurityInformation;
               PID.Invoke(info.ProcessId, delegate
                  {
                     AccessControlSections includeSections = AccessControlSections.None;
                     if ((reqInfo & SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) == SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Owner;
                     if ((reqInfo & SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION) == SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Group;
                     if ((reqInfo & SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.DACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Access;
                     if ((reqInfo & SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.SACL_SECURITY_INFORMATION)
                        includeSections |= AccessControlSections.Audit;
                     string FullName = foundFileInfo.FullName;
                     FileSystemSecurity pSD = (!foundFileInfo.IsDirectory) ? (FileSystemSecurity)File.GetAccessControl(FullName, includeSections)
                                                      : Directory.GetAccessControl(FullName, includeSections);
                     byte[] binaryForm = new byte[rawSecurityDescriptorLength];
                     Marshal.Copy(rawSecurityDescriptor, binaryForm, 0, binaryForm.Length);
                     pSD.SetAccessRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
                     pSD.SetAuditRuleProtection(
                        (reqInfo & SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION,
                        (reqInfo & SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION) == SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
                     pSD.SetSecurityDescriptorBinaryForm(binaryForm, includeSections);
                     // Apply these changes.
                     if (foundFileInfo.IsDirectory)
                        Directory.SetAccessControl(FullName, (DirectorySecurity)pSD);
                     else
                     {
                        File.SetAccessControl(FullName, (FileSecurity)pSD);
                     }
                     notifyOf.SetFileSecurityNative(foundFileInfo.FullName, info.refFileHandleContext);
                  });
            }
            else
            {
               dokanReturn = info.IsDirectory ? Dokan.ERROR_PATH_NOT_FOUND : Dokan.ERROR_FILE_NOT_FOUND;
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("SetFileSecurity threw: ", ex);
            dokanReturn = Utils.BestAttemptToWin32(ex);
         }
         finally
         {
            Log.Debug("SetFileSecurity OUT [{0}]", dokanReturn);
         }
         return dokanReturn;
      }

      #endregion


      private void CloseAndRemove(DokanFileInfo info)
      {
         Log.Debug("info.refFileHandleContext[{0}]", info.refFileHandleContext);
         if (info.refFileHandleContext != 0)
         {
            using (openFilesSync.UpgradableReadLock())
            {
               // The File can be closed by the remote client via Delete (as it does not know to close first!)
               NativeFileOps fileStream;
               if (openFiles.TryGetValue(info.refFileHandleContext, out fileStream))
               {
                  Log.Debug("Remove and then close stream [{0}]", fileStream.FullName);
                  using (openFilesSync.WriteLock())
                  {
                     openFiles.Remove(info.refFileHandleContext);
                  }
                  fileStream.Close();
                  ShellChangeNotify.CloseFile(fileStream.FullName, info.refFileHandleContext);
               }
               else
               {
                  Log.Debug("Something has already closed info.refFileHandleContext [{0}]", info.refFileHandleContext);
               }
            }
         }
         info.refFileHandleContext = 0;
      }

      public void InitialiseShares(object state)
      {
         Log.Debug("InitialiseShares IN");
         try
         {
            Thread.Sleep(250); // Give the driver some time to mount
            // Now check (in 2 phases) the existence of the drive
            ShellChangeNotify.Mount(configDetails.DriveLetter[0]);
            string path = configDetails.DriveLetter + ":" + Roots.PathDirectorySeparatorChar;
            while (!Directory.Exists(path))
            {
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares");
               Thread.Sleep(1000);
            }
            // 2nd phase as the above is supposed to be cheap but can return false +ves
            do
            {
               string[] drives = Environment.GetLogicalDrives();
               if (Array.Exists(drives, dr => dr.Remove(1) == configDetails.DriveLetter))
                  break;
               Log.Info("Waiting for Dokan to create the drive letter before reapplying the shares (Phase 2)");
               Thread.Sleep(100);
            } while (ManagementLayer.Instance.State == LiquesceSvcState.Running);

            configDetails.KnownSharePaths = new List<string>(configDetails.SharesToRestore.Count);
            foreach (LanManShareDetails shareDetails in configDetails.SharesToRestore)
            {
               configDetails.KnownSharePaths.Add(shareDetails.Path);
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