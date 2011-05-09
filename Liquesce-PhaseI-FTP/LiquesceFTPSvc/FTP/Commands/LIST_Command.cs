using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace LiquesceFTPSvc.FTP
{
   /// <summary>
   /// Syntax: LIST [remote-filespec]
   /// If remote-filespec refers to a file, sends information about that file. 
   /// If remote-filespec refers to a directory, sends information about each file in that directory. 
   /// remote-filespec defaults to the current directory. This command must be preceded by a PORT or PASV command. 
   /// </summary>
   partial class FTPClientCommander
   {
      void LIST_Command(string CmdArguments)
      {
         DirectoryInfo dirInfo = null;
         try
         {
            // TODO: Deal with -a -l -la options to this command
            string Path = GetExactPath(CmdArguments);

            dirInfo = new DirectoryInfo(Path);

            if (((dirInfo.Attributes & FileAttributes.System) == FileAttributes.System)
               || (!ConnectedUser.CanViewHiddenFolders
                  && ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                  )
               )
            {
               SendOnControlStream("550 Invalid path specified.");
               return;
            }

         }
         catch (Exception ex)
         {
            SendOnControlStream("550 Invalid path specified: " + ex.Message);
         }

         NetworkStream DataSocket = GetDataSocket();
         if (DataSocket == null)
         {
            return;
         }

         try
         {
            if (dirInfo != null)
            {
               FileSystemInfo[] foldersList = dirInfo.GetFileSystemInfos("*.*", SearchOption.TopDirectoryOnly);

                  foreach (FileSystemInfo info in
                     foldersList.Where(info2 => ((info2.Attributes & FileAttributes.System) != FileAttributes.System)
                           && (ConnectedUser.CanViewHiddenFolders
                              || ((info2.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden))
                              )
                        )
                  {
                  DataSocket.WriteAsciiInfo(info.CreationTimeUtc.ToString("MM-dd-yy hh:mmtt"));
                  DataSocket.WriteAsciiInfo(((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                                               ? " <DIR> "
                                               : string.Format(" {0} ", ((FileInfo) info).Length)
                     ).WritePathNameCRLN(UseUTF8, info.Name);
               }
            }
            DataSocket.Flush();
            SendOnControlStream("226 Transfer Complete.");
         }
         catch (DirectoryNotFoundException ex)
         {
            Log.ErrorException("LIST_Command: ", ex);
            SendOnControlStream("550 Invalid path specified." + ex.Message);
         }
         catch (UnauthorizedAccessException uaex)
         {
            Log.ErrorException("LIST_Command: ", uaex);
            SendOnControlStream("550 Requested action not taken. permission denied. " + uaex.Message);
         }
         catch (Exception ex)
         {
            Log.ErrorException("LIST_Command: ", ex);
            SendOnControlStream("426 Connection closed; transfer aborted.");
         }
         finally
         {
            DataSocket.Close(15);
            // ReSharper disable RedundantAssignment
            DataSocket = null;
            // ReSharper restore RedundantAssignment
         }
      }

   }
}
