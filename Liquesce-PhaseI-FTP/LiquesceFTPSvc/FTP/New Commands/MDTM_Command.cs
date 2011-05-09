﻿using System;
using System.Globalization;
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
      /// <remarks>
      /// There is a means of setting the last-modified timestamp on files via the FTP server. 
      /// This is possible by implementing a hack of MDTM - using this to set timestamps is an abuse of RFC 3659
      /// How would one reverse engineer the UTF8 stuff ?
      /// http://forum.filezilla-project.org/viewtopic.php?f=1&t=15686
      /// </remarks>
      /// <param name="cmdArguments"></param>
      private void MDTM_Command(string cmdArguments)
      {
         string Path = GetExactPath(cmdArguments);
         FileInfo fi = new FileInfo(Path);
         if (fi.Exists)
            SendOnControlStream("213 " + GetFormattedTime(fi.LastWriteTimeUtc));
         else
            SendOnControlStream("550 File doe snot exist.");
      }

      private static void MDTM_Support(FTPClientCommander thisClient)
      {
         thisClient.SendOnControlStream(" MDTM");
      }

      private const string utcFTPFormat = "yyyyMMddHHmmss";
      private string GetFormattedTime(DateTime utcTime)
      {
         return utcTime.ToString(utcFTPFormat);
      }

      private DateTime SetFormattedTime(string stringTime)
      {
         return DateTime.ParseExact(stringTime, utcFTPFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
      }

   }
}
