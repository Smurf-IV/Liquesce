// Implement API's based on http://dokan-dev.net/en/docs/dokan-readme/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using DokanNet;
using LiquesceFacade;
using NLog;

namespace ClientLiquesceSvc
{
   internal class ClientLiquesceOps : IDokanOperations
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private readonly ClientShareDetail configDetail;

      public ClientLiquesceOps(ClientShareDetail configDetail)
      {
         this.configDetail = configDetail;
      }

      #region IDokanOperations Implementation

      /// <summary>
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
         int actualErrorCode = Dokan.DOKAN_SUCCESS;
         try
         {
            Log.Debug(
               "CreateFile IN filename [{0}], rawAccessMode[{1}], rawShare[{2}], rawCreationDisposition[{3}], rawFlagsAndAttributes[{4}], ProcessId[{5}]",
               filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable
                  && (((rawAccessMode & Proxy.FILE_WRITE_DATA) == Proxy.FILE_WRITE_DATA))
                  )
               {
                  actualErrorCode = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  bool isDirectory = info.IsDirectory;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  actualErrorCode = remoteIF.CreateFile(filename, rawAccessMode, rawShare, rawCreationDisposition, rawFlagsAndAttributes, ref DokanContext, ref isDirectory);
                  info.Context = DokanContext;
                  info.IsDirectory = isDirectory;
               }
            }
            else
            {
               actualErrorCode = Dokan.ERROR_ACCESS_DENIED;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CreateFile threw: ", ex);
            actualErrorCode = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateFile OUT actualErrorCode=[{0}]", actualErrorCode);
         }
         return actualErrorCode;
      }

      private static readonly Dictionary<string, int> validatedDomainUsers = new Dictionary<string, int>();
      private static readonly ReaderWriterLockSlim validatedDomainUsersLock = new ReaderWriterLockSlim();

      private bool IsValidatedUser(string getDomainUserFromPid, out bool writeable)
      {
         int state = 0; // Should really be an enum, denotes that a user has already been validated, AND if they have write
         using (validatedDomainUsersLock.UpgradableReadLock())
         {
            if (!validatedDomainUsers.ContainsKey(getDomainUserFromPid))
            {
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               if (remoteIF.CanIdentityUseThis(getDomainUserFromPid, configDetail.TargetShareName, out writeable))
                  state = 1;
               if (writeable)
                  state = 2;
               using (validatedDomainUsersLock.WriteLock())
                  validatedDomainUsers[getDomainUserFromPid] = state;
            }
            else
            {
               state = validatedDomainUsers[getDomainUserFromPid];
               writeable = state > 1;
            }
         }
         return state > 0;
      }

      public int OpenDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               Object DokanContext = info.Context;
               bool isDirectory = info.IsDirectory;
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               dokanError = remoteIF.OpenDirectory(filename, ref DokanContext, ref isDirectory);
               info.Context = DokanContext;
               info.IsDirectory = isDirectory;
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         finally
         {
            Log.Trace("OpenDirectory OUT. dokanError[{0}]", dokanError);
         }
         return dokanError;
      }


      public int CreateDirectory(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable )
               {
                  dokanError = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  bool isDirectory = info.IsDirectory;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanError = remoteIF.CreateDirectory(filename, ref DokanContext, ref isDirectory);
                  info.Context = DokanContext;
                  info.IsDirectory = isDirectory;
               }
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CreateDirectory threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CreateDirectory OUT dokanError[{0}]", dokanError);
         }
         return dokanError;
      }

      /*
      Cleanup is invoked when the function CloseHandle in Windows API is executed. 
      If the file system application stored file handle in the Context variable when the function CreateFile is invoked, 
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
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               Object DokanContext = info.Context;
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               dokanError = remoteIF.Cleanup(filename, ref DokanContext, info.IsDirectory);
               info.Context = DokanContext;
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("Cleanup threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("Cleanup OUT");
         }
         return dokanError;
      }

      public int CloseFile(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("CloseFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
         {
               Object DokanContext = info.Context;
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               dokanError = remoteIF.CloseFile(filename, ref DokanContext);
               info.Context = DokanContext;
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("CloseFile threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("CloseFile OUT");
         }
         return dokanError;
      }

      public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
      {
         int errorCode = Dokan.DOKAN_SUCCESS;
         try
         {
            Log.Debug("ReadFile IN offset=[{1}] DokanProcessId[{0}]", info.ProcessId, offset);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               Object DokanContext = info.Context;
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               errorCode = remoteIF.ReadFile(filename, buffer, ref readBytes, offset, ref DokanContext);
               info.Context = DokanContext;
            }
            else
            {
               errorCode = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("ReadFile threw: ", ex);
            errorCode = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Debug("ReadFile OUT readBytes=[{0}], errorCode[{1}]", readBytes, errorCode);
         }
         return errorCode;
      }

      public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("WriteFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable )
               {
                  dokanError = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanError = remoteIF.WriteFile(filename, buffer, ref writtenBytes, offset, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("WriteFile threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("WriteFile OUT");
         }
         return dokanError;
      }

      public int FlushFileBuffers(string filename, DokanFileInfo info)
      {
         int dokanError = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("FlushFileBuffers IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanError = remoteIF.FlushFileBuffers(filename, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanError = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            dokanError = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("FlushFileBuffers OUT");
         }
         return dokanError;
      }

      public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("GetFileInformation IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.GetFileInformation(filename, fileinfo, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FlushFileBuffers threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("GetFileInformation OUT Attributes[{0}] Length[{1}] dokanReturn[{2}]", fileinfo.Attributes, fileinfo.Length, dokanReturn);
         }
         return dokanReturn;
      }

      public int FindFiles(string filename, out FileInformation[] files, DokanFileInfo info)
      {
         files = null;
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("FindFiles IN [{0}]", filename);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               Object DokanContext = info.Context;
               ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
               IShareEnabler remoteIF = factory.CreateChannel();
               dokanReturn = remoteIF.FindFiles(filename, out files, ref DokanContext);
               info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FindFiles threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
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
                  for (int index = 0; index < files.Length; index++)
                  {
                     FileInformation fileInformation = files[index];
                     sb.AppendLine(fileInformation.FileName);
                  }
                  Log.Trace(sb.ToString());
               }
            }
         }
         return dokanReturn;
      }

      public int FindFilesWithPattern(string filename, string pattern, out FileInformation[] files, DokanFileInfo info)
      {
         files = null;
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Debug("FindFilesWithPattern IN [{0}], pattern[{1}]", filename, pattern);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.FindFilesWithPattern(filename, pattern, out files, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("FindFilesWithPattern threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Debug("FindFilesWithPattern OUT [found {0}]", (files != null ? files.Length : 0));
            if (Log.IsTraceEnabled)
            {
               if (files != null)
               {
                  StringBuilder sb = new StringBuilder();
                  sb.AppendLine();
                  for (int index = 0; index < files.Length; index++)
                  {
                     FileInformation fileInformation = files[index];
                     sb.AppendLine(fileInformation.FileName);
                  }
                  Log.Trace(sb.ToString());
               }
            }
         }
         return dokanReturn;
      }

      public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetFileAttributes IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable )
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.SetFileAttributes(filename, attr, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetFileAttributes threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetFileAttributes OUT");
         }
         return dokanReturn;
      }

      public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetFileTime IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.SetFileTime(filename, ctime, atime, mtime, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetFileTime threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetFileTime OUT");
         }
         return dokanReturn;
      }

      /// </summary>
      /// <param name="filename"></param>
      /// <param name="info"></param>
      /// <returns></returns>
      public int DeleteFile(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.DeleteFile(filename, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteFile OUT dokanReturn[(0}]", dokanReturn);
         }
         return dokanReturn;
      }

      public int DeleteDirectory(string filename, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("DeleteDirectory IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  bool isDirectory = info.IsDirectory;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.DeleteDirectory(filename, ref DokanContext, ref isDirectory);
                  info.Context = DokanContext;
                  info.IsDirectory = isDirectory;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("DeleteDirectory threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("DeleteDirectory OUT dokanReturn[(0}]", dokanReturn);
         }

         return dokanReturn;
      }


      public int MoveFile(string filename, string newname, bool replaceIfExisting, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("MoveFile IN DokanProcessId[{0}]", info.ProcessId);
            Log.Info("MoveFile replaceIfExisting [{0}] filename: [{1}] newname: [{2}]", replaceIfExisting, filename, newname);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  bool isDirectory = info.IsDirectory;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.MoveFile(filename, newname, replaceIfExisting, ref DokanContext, ref isDirectory);
                  info.Context = DokanContext;
                  info.IsDirectory = isDirectory;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("MoveFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("MoveFile OUT");
         }
         return dokanReturn;
      }

      public int SetEndOfFile(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetEndOfFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.SetEndOfFile(filename, length, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetEndOfFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetEndOfFile OUT", dokanReturn);
         }
         return dokanReturn;
      }

      public int SetAllocationSize(string filename, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("SetAllocationSize IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               if (!writeable)
               {
                  dokanReturn = Dokan.ERROR_ACCESS_DENIED;
               }
               else
               {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.SetAllocationSize(filename, length, ref DokanContext);
                  info.Context = DokanContext;
               }
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("SetAllocationSize threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("SetAllocationSize OUT");
         }
         return dokanReturn;
      }

      public int LockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("LockFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
               
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.LockFile(filename, offset, length, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }

         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("LockFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("LockFile OUT");
         }
         return dokanReturn;
      }

      public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("UnlockFile IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.UnlockFile(filename, offset, length, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("UnlockFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("UnlockFile OUT");
         }
         return dokanReturn;
      }

      public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
      {
         int dokanReturn = Dokan.DOKAN_ERROR;
         try
         {
            Log.Trace("GetDiskFreeSpace IN DokanProcessId[{0}]", info.ProcessId);
            bool writeable;
            if (IsValidatedUser(ProcessIDToUser.GetDomainUserFromPID(info.ProcessId), out writeable))
            {
                  Object DokanContext = info.Context;
                  ChannelFactory<IShareEnabler> factory = new ChannelFactory<IShareEnabler>("ShareEnabler");
                  IShareEnabler remoteIF = factory.CreateChannel();
                  dokanReturn = remoteIF.GetDiskFreeSpace(ref freeBytesAvailable, ref totalBytes, ref totalFreeBytes, ref DokanContext);
                  info.Context = DokanContext;
            }
            else
            {
               dokanReturn = Dokan.ERROR_ACCESS_DENIED;
            }
         }
         catch (Exception ex)
         {
            int win32 = ((short)Marshal.GetHRForException(ex) * -1);
            Log.ErrorException("UnlockFile threw: ", ex);
            dokanReturn = (win32 != 0) ? win32 : Dokan.ERROR_ACCESS_DENIED;
         }
         finally
         {
            Log.Trace("GetDiskFreeSpace OUT");
         }
         return dokanReturn;
      }

      public int Unmount(DokanFileInfo info)
      {
         Log.Trace("Unmount IN DokanProcessId[{0}]", info.ProcessId);
         try
         {
         }
         finally
         {
            // openFilesSync.ExitWriteLock();
         }
         Log.Trace("Unmount out");
         return Dokan.DOKAN_SUCCESS;
      }

      #endregion


   }
}