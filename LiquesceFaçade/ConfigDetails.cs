#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ConfigDetails.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LiquesceFacade
{

   /// <summary>
   /// This is the class that will dump out the details to the XML File.
   /// </summary>
   [DataContract]
   public class ConfigDetails
   {
      public void InitConfigDetails()
      {
         string[] drives = Environment.GetLogicalDrives();
         List<char> driveLetters = new List<char>(26);
         driveLetters.AddRange(drives.Select(dr => dr.ToUpper()[0]));
         // Reverse find the 1st letter not used (Dokan does not support ower than D)
         for (int i = 0; i < 22; i++)
         {
            char letter = (char) ('Z' - i);
            if ( !driveLetters.Contains(letter) )
            {
               DriveLetter = letter.ToString(CultureInfo.InvariantCulture);
               break;
            }
         }
         if (string.IsNullOrEmpty(DriveLetter))
         {
            DriveLetter = "C:\\" + new Guid();
            Directory.CreateDirectory(DriveLetter);
         }
      }

      public enum AllocationModes
      {
         folder
        ,priority
        ,balanced
      };

      [DataMember(IsRequired = true)]
      public uint DelayStartMilliSec = 250;

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      // Also the V 0.6 of Dokan is supposed to be able to use Mount points so this can then be reused for that..
      [DataMember(IsRequired = true)]
      public string DriveLetter;

      [DataMember]
      public ushort ThreadCount = 0;

      [DataMember(IsRequired = true)]
      public string VolumeLabel = "Mirror of C";

      [DataMember]
      public AllocationModes AllocationMode = AllocationModes.folder;

      [DataMember]
      public UInt64 HoldOffBufferBytes = 1L << 10 << 10 << 10; // ==1GB;

      [DataMember(IsRequired = true)]
      public List<string> SourceLocations = new List<string>();

      [DataMember]
      public string ServiceLogLevel = "Warn"; // NLog's LogLevel.Debug.ToString()

      [DataMember]
      public UInt16 CacheLifetimeSeconds = 32; // Set to zero to disable

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("DelayStartMilliSec=[{0}]",DelayStartMilliSec).AppendLine();
         sb = sb.AppendFormat("DriveLetter=[{0}]", DriveLetter).AppendLine();
         sb = sb.AppendFormat("ThreadCount=[{0}]",ThreadCount).AppendLine();
         sb = sb.AppendFormat("VolumeLabel=[{0}]",VolumeLabel).AppendLine();
         sb = sb.AppendFormat("AllocationMode=[{0}]",AllocationMode).AppendLine();
         sb = sb.AppendFormat("HoldOffBufferBytes=[{0}]", HoldOffBufferBytes).AppendLine();
         sb = sb.AppendLine("SourceLocations:");
         sb = SourceLocations.Aggregate(sb, (current, location) => current.AppendLine(location));
         sb = sb.AppendFormat("ServiceLogLevel[{0}]",ServiceLogLevel).AppendLine();
         sb = sb.AppendFormat("CacheLifetimeSeconds=[{0}]", CacheLifetimeSeconds).AppendLine();
         return sb.ToString();
      }
   }
}