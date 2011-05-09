using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: "MFMT" SP time-val SP pathname CRLF
      /// http://www.omz13.com/downloads/draft-somers-ftp-mfxx-02.html#MFMT
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MFMT_Command(string cmdArguments)
      {
         int split = cmdArguments.IndexOf(' ');
         string time = cmdArguments.Substring(0, split);
         string filename = cmdArguments.Substring(split).Trim();

         try
         {
            string Path = GetExactPath(filename);
            FileSystemInfo info = new DirectoryInfo(Path);
            if (!info.Exists)
            {
               info = new FileInfo(Path);
            }

            if (!info.Exists)
            {
               SendOnControlStream("550 filename does not exist: " + filename);
            }
            else
            {
               info.LastWriteTimeUtc = SetFormattedTime(time);
               SendOnControlStream(string.Format("213 Create={0}; {1}", time, filename));
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("MFMT Exception: ", ex);
            SendOnControlStream("500 MFMT Exception: " + ex.Message);
         }
      }

      private static void MFMT_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" MFMT");
      }
   }
}
