using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: "MFCT" SP time-val SP pathname CRLF
      /// http://www.omz13.com/downloads/draft-somers-ftp-mfxx-02.html#MFCT
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MFCT_Command(string cmdArguments)
      {
         int split = cmdArguments.IndexOf(' ');
         string time = cmdArguments.Substring(0, split);
         string filename = cmdArguments.Substring(split).Trim();

         try
         {
            FileSystemInfo info;
            string Path = GetExactPath(filename);
            info = new DirectoryInfo(Path);
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
               info.CreationTimeUtc = SetFormattedTime(time);
               SendOnControlStream(string.Format("213 Create={0}; {1}", time, filename));
            }

         }
         catch (Exception ex)
         {
            Log.ErrorException("MFCT Exception: ", ex);
            SendOnControlStream("500 MFCT Exception: " + ex.Message );
         } 
      }

      private static void MFCT_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" MFCT");
      }
   }
}
