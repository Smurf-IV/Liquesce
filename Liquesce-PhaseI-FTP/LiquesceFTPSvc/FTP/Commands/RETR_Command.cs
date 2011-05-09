using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax:  RETR remote-filename
      /// Begins transmission of a file from the remote host. 
      /// Must be preceded by either a PORT command or a PASV command to indicate where the server should send data. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void RETR_Command(string CmdArguments)
      {
         if (!ConnectedUser.CanCopyFiles)
         {
            SendOnControlStream("426 Access Denied.");
            return;
         }

         string ReturnMessage = string.Empty;
         const int readSize = 8096*4;

         FileStream FS = null;
         NetworkStream DataSocket = null;
         try
         {
            string path = GetExactPath(CmdArguments);
            
            FileInfo info = new FileInfo(path);

            if( ((info.Attributes & FileAttributes.System) == FileAttributes.System)
               || (!ConnectedUser.CanViewHiddenFolders
                  && ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                  )
               )
            {
               SendOnControlStream("550 Access Denied or invalid path.");
               return;
            }

            FS = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, readSize)
                              {
                                 Position = startOffset
                              };
         }
         catch (Exception ex)
         {
            ReturnMessage = "550 " + ex.Message + "!";
            goto FinaliseAll;
         }
         // Not needed any more
         startOffset = 0;


         DataSocket = GetDataSocket();
         if (DataSocket == null)
            goto FinaliseAll;

         try
         {

            byte[] data = new byte[(FS.Length > readSize) ? readSize : (int)FS.Length];

            do
            {
               int read = FS.Read(data, 0, data.Length);
               //Log.Trace("Sending[{0}] from {1}", read, path);
               if ((read > 0)
                  && !abortReceived
                  )
               {
                  DataSocket.Write(data, 0, read);
               }
               else
                  break;
            } while (true);
            // If it gets this far then probably a good send 
            // Clients tend to forcibly close rather than use ABOR.....
            ReturnMessage = abortReceived ? "426 Transfer aborted." : "226 Transfer Completed.";
         }
         catch ( Exception ex )
         {
            Log.ErrorException("Init Retr", ex);
            ReturnMessage = "426 Transfer aborted. " + ex.Message;
         }

      FinaliseAll:
         if (FS != null)
            FS.Close();
         if (DataSocket != null)
         {
            DataSocket.Flush();
            DataSocket.Close(15);
         }
         abortReceived = false;
         // ReSharper disable RedundantAssignment
         DataSocket = null;
         // ReSharper restore RedundantAssignment
         SendOnControlStream(ReturnMessage);
      }


   }
}
