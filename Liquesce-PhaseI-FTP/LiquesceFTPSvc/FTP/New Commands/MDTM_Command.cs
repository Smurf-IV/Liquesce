﻿using System;
using System.IO;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: MDTM remote-filename
      /// Returns the last-modified time of the given file on the remote host in the format "YYYYMMDDhhmmss": 
      /// YYYY is the four-digit year, 
      /// MM is the month from 01 to 12, 
      /// DD is the day of the month from 01 to 31, 
      /// hh is the hour from 00 to 23, 
      /// mm is the minute from 00 to 59, and 
      /// ss is the second from 00 to 59. 
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MDTM_Command(string cmdArguments)
      {
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(cmdArguments);
         Path = Path.Substring(0, Path.Length - 1);
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
            SendMessage("213 " + GetFormattedTime(fi.LastWriteTimeUtc) + "\r\n");
         else
            SendMessage("550 File doe snot exist.\r\n");
      }

      private static void MDTM_Support(FTPClientCommander thisClient)
      {
         thisClient.SendMessage(" MDTM\r\n");
      }

      private string GetFormattedTime(DateTime utcTime)
      {
         return utcTime.ToString("yyyyMMddHHmmss");
      }
   }
}