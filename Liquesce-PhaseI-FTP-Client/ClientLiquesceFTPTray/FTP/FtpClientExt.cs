using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray.FTP
{
   [Flags]
   internal enum Features
   {
      LIST,
      CWD = 0x1,
      REST = 1 << 2,
      MLST = 1 << 3,
      MLSD = 1 << 4,
      SIZE = 1 << 5,
      MFMT = 1 << 6,
      MFCT = 1 << 7,
      MDTM = 1 << 8,
   }

   /// <summary>
   /// Extends the FtpClient to deal with the features that the server may be presenting, otherwise drop back to the older API's.
   /// Will also perform Command locking to allow multiple threads to use the main command Instance
   /// </summary>
   internal class FtpClientExt
   {
      private readonly FtpClient ftpInstance;
      private readonly object commandLock = new object();
      private bool featuresFound;
      private Features supportedFeatures = Features.LIST | Features.CWD;

      private Features SupportedFeatures
      {
         get
         {
            if (!featuresFound)
               EnumerateFeatures();
            return supportedFeatures;
         }
      }



      public FtpClientExt(FtpClient ftpInstance)
      {
         this.ftpInstance = ftpInstance;
      }

      public FtpClientExt(FtpClient ftpInstance, uint tcpBufferSize)
      {
         this.ftpInstance = ftpInstance;
         ftpInstance.TcpBufferSize = tcpBufferSize;
      }

      public void Open(string userName, string password)
      {
         lock (commandLock)
         {
            ftpInstance.Open(userName, password);
         }
      }

      public void Close()
      {
         lock (commandLock)
         {
            ftpInstance.Close();
         }
      }


      public void SetAttributes(string path, FileAttributes value)
      {
         lock (commandLock)
         {
            CheckConnected();
            // ftpInstance.ChangeMode(path, (int) value);
            /*
There is no command in the FTP protocol to do this.

We are not talking about a limitation on just Microsoft's IIS FTP server, we
are talking about a feature that just isn't a part of the protocol at all.

A few clients, and maybe a server or two, have implemented a rather
unreliable hack centering around the MDTM command, but it's been debated by
the working group responsible for FTP RFCs, and rejected as being
unreliable, and an inappropriate extension to a command that is only
designed to display the last-write time.
             */
         }

      }

      public void MakeDirectory(string path)
      {
         lock (commandLock)
         {
            CheckConnected();
            ftpInstance.MakeDirectory(path);
         }
      }

      public long GetFileSize(string path)
      {
         lock (commandLock)
         {
            CheckConnected();
            return ftpInstance.GetFileSize(path);
         }
      }

      private void EnumerateFeatures()
      {
         lock (commandLock)
         {
            CheckConnected();
            string features = ftpInstance.GetFeatures().ToUpper();
            if (features.Contains(Features.REST.ToString()))
               supportedFeatures |= Features.REST;
            if (features.Contains(Features.MLST.ToString()))
               supportedFeatures |= Features.MLST;
            if (features.Contains(Features.MLSD.ToString()))
               supportedFeatures |= Features.MLSD;
            if (features.Contains(Features.SIZE.ToString()))
               supportedFeatures |= Features.SIZE;
            if (features.Contains(Features.MFMT.ToString()))
               supportedFeatures |= Features.MFMT;
            if (features.Contains(Features.MFCT.ToString()))
               supportedFeatures |= Features.MFCT;
            if (features.Contains(Features.MDTM.ToString()))
               supportedFeatures |= Features.MDTM;

            featuresFound = true;
         }
      }

      public FtpResponseCollection Feature(bool useNewResponseList, Features feature, bool UTFEncodeArg = false, params string[] arguments)
      {
         if ((SupportedFeatures & feature) != feature)
            throw new ActionNotSupportedException(string.Format("FTP Target server does not support the {0} command", feature));
         lock (commandLock)
            return Quote(FtpRequest.BuildCommandText(feature.ToString(), UTFEncodeArg?ftpInstance.CharacterEncoding:Encoding.ASCII, arguments), useNewResponseList);

      }

      private FtpResponseCode[] happyCodes = null;

      private FtpResponseCollection Quote(byte[] preEncodedCommand, bool useNewResponseList = false)
      {
         try
         {
            FtpResponseCollection response = null;
            FtpRequest request = new FtpRequest(preEncodedCommand);
            if (happyCodes != null)
               request.HappyCodes = happyCodes;
            if (useNewResponseList)
            {
               response = new FtpResponseCollection(ftpInstance.TransferText(request));
            }
            else
            {
               ftpInstance.SendRequest(request);
               response = ftpInstance.LastResponseList;
            }
            return response;

         }
         finally
         {
            happyCodes = null;
         }
      }

      public void GetFile(string path, MemoryStream memStream)
      {
         lock (commandLock)
         {
            FtpResponseCollection response = null;
            try
            {
               CheckConnected();
               FtpRequest request = new FtpRequest(FtpRequest.BuildCommandText("RETR", ftpInstance.CharacterEncoding, new string[] { path }));
               ftpInstance.TransferData(TransferDirection.ToClient, request, memStream);
               response = ftpInstance.LastResponseList;
            }
            catch( Exception ex )
            {
               if ((response == null)
                  && (memStream.Position != memStream.Length)
                  )
               {
                  // This means that the expected read of the fragment did not complete and aborted for another reason
                  throw;
               }
            }
         }
      }

      /// <summary>
      /// Put the stream into the specified file
      /// </summary>
      /// <param name="inputStream"></param>
      /// <param name="remotePath"></param>
      /// <param name="append">add to an existing file</param>
      public void PutFile(Stream inputStream, string remotePath, bool append)
      {
         lock (commandLock)
         {
            CheckConnected();
            ftpInstance.PutFile(inputStream, remotePath, (append ? FileAction.CreateOrAppend : FileAction.Create));
         }
      }

      public bool ChangeDirectory(string path)
      {
         lock (commandLock)
         {
            FtpRequest request = new FtpRequest(FtpCmd.Cwd, ftpInstance.CharacterEncoding, path)
                                    {
                                       HappyCodes =
                                          FtpRequest.BuildResponseArray(
                                             FtpResponseCode.RequestedFileActionOkayAndCompleted,
                                             FtpResponseCode.RequestedActionNotTakenFileUnavailable
                                          )
                                    };
            CheckConnected();
            ftpInstance.SendRequest(request);
            return ftpInstance.LastResponse.Code == FtpResponseCode.RequestedFileActionOkayAndCompleted;
         }
      }

      public List<FileSystemFTPInfo> GetDirList(string path)
      {
         List<FileSystemFTPInfo> foundValues;
         lock (commandLock)
         {
            Features featureToUse = ((SupportedFeatures & Features.MLSD) == Features.MLSD)
                                       ? Features.MLSD
                                       : Features.LIST;
            if (featureToUse == Features.MLSD)
            {
               happyCodes = FtpRequest.BuildResponseArray(FtpResponseCode.ClosingDataConnection,
                           FtpResponseCode.RequestedFileActionOkayAndCompleted);
            }
            else
            {
               happyCodes = FtpRequest.BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting,
                           FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection,
                           FtpResponseCode.ClosingDataConnection,
                           FtpResponseCode.RequestedFileActionOkayAndCompleted);
            }
            FtpResponseCollection dirResults = (string.IsNullOrEmpty(path))
                                                  ? Feature(true, featureToUse, true)
                                                  : Feature(true, featureToUse, true, path);
            if (featureToUse == Features.MLSD)
            {
               foundValues = new MlstCollection(this, path, dirResults);
            }
            else
            {
               // Do it the harder way ??
               FtpItemCollection results = new FtpItemCollection(path, dirResults.GetRawText(), ftpInstance.ItemParser);
               foundValues = new List<FileSystemFTPInfo>(results.Count);
               foundValues.AddRange(
                  results.Select(
                     item =>
                     (item.ItemType == FtpItemType.Directory)
                        ? (FileSystemFTPInfo) new DirectoryFTPInfo(this, item.FullPath)
                        : new FileFTPInfo(this, item.FullPath)));
            }
         }
         return foundValues;
      }

      public void DeleteDirectory(string path)
      {
         lock (commandLock)
         {
            CheckConnected();
            ftpInstance.DeleteDirectory(path);
         }
      }

      public void DeleteFile(string path)
      {
         lock (commandLock)
         {
            CheckConnected();
            ftpInstance.DeleteFile(path);
         }
      }

      /// <summary>
      /// Get as amany details as it can about the target.
      /// </summary>
      /// <param name="target">If empty then will assume CWD has been used</param>
      /// <returns>May return null if nothing found</returns>
      public FileSystemFTPInfo GetFileDetails(string target)
      {
         List<FileSystemFTPInfo> foundValues;
         lock (commandLock)
         {
            Features featureToUse = ((SupportedFeatures & Features.MLST) == Features.MLST)
                                       ? Features.MLST
                                       : Features.LIST;
            if (featureToUse == Features.MLST)
            {
               happyCodes = FtpRequest.BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted,
                              FtpResponseCode.SyntaxErrorInParametersOrArguments // Stop using exceptions to detect missing
                              );
            }
            else
            {
               happyCodes = FtpRequest.BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting,
                           FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection,
                           FtpResponseCode.ClosingDataConnection,
                           FtpResponseCode.RequestedFileActionOkayAndCompleted
                           );
            }
            FtpResponseCollection dirResults = (string.IsNullOrEmpty(target))
                                                  ? Feature(false, featureToUse)
                                                  : Feature(false, featureToUse, true, target);
            if (featureToUse == Features.MLST)
            {
               foundValues = new MlstCollection(this, target, dirResults);
            }
            else
            {
               // Do it the harder way ??
               FtpItemCollection results = new FtpItemCollection(target, dirResults.GetRawText(), ftpInstance.ItemParser);
               foundValues = new List<FileSystemFTPInfo>(results.Count);
               foundValues.AddRange(
                  results.Select(
                     item =>
                     (item.ItemType == FtpItemType.Directory)
                        ? (FileSystemFTPInfo) new DirectoryFTPInfo(this, item.FullPath)
                        : new FileFTPInfo(this, item.FullPath)));
            }
         }
         return foundValues.Count > 0 ? foundValues[0] : null;
      }

      internal void GetDiskFreeSpace( long testSize, ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes)
      {
         lock (commandLock)
         {
            CheckConnected();
            ftpInstance.AllocateStorage(testSize);
            foreach (string[] values in from response in ftpInstance.LastResponseList
                                        where response.IsInformational
                                        select response.Text.Split('[', ']')
                                        into values where values.Length == 7 select values)
            {
               ulong.TryParse(values[1].Trim(), out freeBytesAvailable);
               ulong.TryParse(values[3].Trim(), out totalBytes);
               ulong.TryParse(values[5].Trim(), out totalFreeBytes);
               break;
            }
         }
      }

      private void CheckConnected()
      {
         if (!ftpInstance.IsConnected)
            ftpInstance.Reopen();
      }

   }
}
