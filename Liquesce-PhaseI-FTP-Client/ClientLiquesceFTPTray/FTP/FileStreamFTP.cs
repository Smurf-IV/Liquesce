using System;
using System.IO;
using System.ServiceModel;
using System.Threading;
using DokanNet;
using Starksoft.Net.Ftp;

namespace ClientLiquesceFTPTray.FTP
{
   class FileStreamFTP
   {
      private readonly ClientShareDetail csd;
      private readonly uint rawCreationDisposition; // http://msdn.microsoft.com/en-us/library/aa363858%28v=vs.85%29.aspx
      public FileFTPInfo fsi { get; set; }
      private bool completedOpen;
      private Exception exceptionThrown = null;
      private FtpClientExt ftpFileClient;

      public FileStreamFTP(ClientShareDetail csd, uint rawCreationDisposition, FileFTPInfo foundFileInfo)
      {
         this.csd = csd;
         this.rawCreationDisposition = rawCreationDisposition;
         fsi = foundFileInfo;
         // Queue the main work as a thread pool task as we want this method to finish promptly.
         ThreadPool.QueueUserWorkItem(ConnectFTP);
         fsi.Open((FileMode)rawCreationDisposition);
      }


      private void ConnectFTP(object state)
      {
         try
         {
            ftpFileClient = new FtpClientExt(new FtpClient(csd.TargetMachineName, csd.Port, csd.SecurityProtocol), csd.BufferWireTransferSize);
            ftpFileClient.Open(csd.UserName, csd.Password);
            completedOpen = true;
         }
         catch (Exception ex)
         {
            exceptionThrown = ex;
         }
      }

      public long Length
      {
         get
         {
            return fsi.Length;
         }
      }

      public string Name
      {
         get { return fsi.Name; }
      }

      private void CheckOpened()
      {
         if (!completedOpen)
         {
            if (exceptionThrown != null)
               throw exceptionThrown;
            Thread.Sleep(1000);
            CheckOpened();
         }
      }

      public void Close()
      {
         if (ftpFileClient != null)
            ftpFileClient.Close();
      }

      public void Seek(long rawOffset, SeekOrigin begin)
      {
         if (begin != SeekOrigin.Begin)
            throw new ArgumentOutOfRangeException("begin", "This function only supports offsets from SeekOrigin.Begin");
         CheckOpened();
         ftpFileClient.Feature(false, Features.REST, false, rawOffset.ToString());
      }

      public uint Read(byte[] buf, uint rawBufferLength)
      {
         CheckOpened();
         // TODO: Eventually use a temp file and use a continuous Asynch Filestream into it via
         //         internal void TransferData(TransferDirection direction, FtpRequest request, Stream data, long restartPosition)
         MemoryStream memStream = new MemoryStream(buf, true);
         ftpFileClient.GetFile(fsi.FullName, memStream);
         return (uint)memStream.Position;
      }

      /// <summary>
      /// Uses the current offset to write this buffer
      /// </summary>
      /// <param name="buf"></param>
      /// <param name="rawNumberOfBytesToWrite"></param>
      /// <returns></returns>
      public bool Write(byte[] buf, uint rawNumberOfBytesToWrite)
      {
         CheckOpened();
         // TODO: Eventually use a temp file and use a continuous Asynch Filestream into it via
         //         internal void TransferData(TransferDirection direction, FtpRequest request, Stream data, long restartPosition)
         MemoryStream memStream = new MemoryStream(buf, 0, (int) rawNumberOfBytesToWrite, false);
         ftpFileClient.PutFile(memStream, fsi.FullName, (rawCreationDisposition == (uint) FileMode.Append));
         return true;
      }
   }
}
