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
using System.Xml.Serialization;
using NLog;

namespace LiquesceFacade
{

   /// <summary>
   /// This is the class that will dump out the details to the XML File.
   /// </summary>
   [DataContract]
   public class ConfigDetails
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      public const string ProductNameCBFS = "LiquesceSvc";
      static public readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductNameCBFS, "Properties.config.xml");

      public void InitConfigDetails()
      {
         MountDetail mt = new MountDetail();
         mt.InitConfigDetails();
         MountDetails.Add( mt );
      }


      public void WriteOutConfigDetails()
      {
         WriteOutConfigDetails(this);
      }

      public static void WriteOutConfigDetails(ConfigDetails currentConfigDetails)
      {
         if (currentConfigDetails != null)
            try
            {
               XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
               using (TextWriter textWriter = new StreamWriter(configFile))
               {
                  x.Serialize(textWriter, currentConfigDetails);
               }
            }
            catch (Exception ex)
            {
               Log.ErrorException("Cannot save configDetails: ", ex);
            }
      }

      [DataMember(IsRequired = true)]
      public uint DelayStartMilliSec = 250;

      [DataMember]
      public ushort ThreadCount = 0;

      [DataMember]
      public string ServiceLogLevel = LogLevel.Fatal.Name; 

      [DataMember]
      public UInt16 CacheLifetimeSeconds = 32; // Set to zero to disable

      [DataMember] public List<MountDetail> MountDetails = new List<MountDetail>(); 

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("DelayStartMilliSec=[{0}]",DelayStartMilliSec).AppendLine();
         sb = sb.AppendFormat("ThreadCount=[{0}]",ThreadCount).AppendLine();
         sb = sb.AppendFormat("ServiceLogLevel[{0}]",ServiceLogLevel).AppendLine();
         sb = sb.AppendFormat("CacheLifetimeSeconds=[{0}]", CacheLifetimeSeconds).AppendLine();
         sb = sb.AppendLine("MountDetails:");
         sb = MountDetails.Aggregate(sb, (current, mountDetail) => current.AppendLine(mountDetail.ToString()));
         return sb.ToString();
      }

   }

   [DataContract]
   public class MountDetail
   {
      public enum AllocationModes
      {
         Folder,
         Priority,
         Balanced
      };

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
      [DataMember(IsRequired = true)]
      public string DriveLetter;

      [DataMember(IsRequired = true)]
      public string VolumeLabel = "Mirror of C";

      [DataMember]
      public AllocationModes AllocationMode = AllocationModes.Folder;

      [DataMember]
      public UInt64 HoldOffBufferBytes = 1L << 10 << 10 << 10; // ==1GB;

      [DataMember(IsRequired = true)]
      public List<SourceLocation> SourceLocations = new List<SourceLocation>();

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("DriveLetter=[{0}]", DriveLetter).AppendLine();
         sb = sb.AppendFormat("VolumeLabel=[{0}]", VolumeLabel).AppendLine();
         sb = sb.AppendFormat("AllocationMode=[{0}]", AllocationMode).AppendLine();
         sb = sb.AppendFormat("HoldOffBufferBytes=[{0}]", HoldOffBufferBytes).AppendLine();
         sb = sb.AppendLine("SourceLocations:");
         sb = SourceLocations.Aggregate(sb, (current, location) => current.AppendLine(location.ToString()));
         return sb.ToString();
      }

      public void InitConfigDetails()
      {
         string[] drives = Environment.GetLogicalDrives();
         List<char> driveLetters = new List<char>(26);
         driveLetters.AddRange(drives.Select(dr => dr.ToUpper()[0]));
         // Reverse find the 1st letter not used 
         for (int i = 0; i < 26; i++)
         {
            char letter = (char)('Z' - i);
            if (!driveLetters.Contains(letter))
            {
               DriveLetter = letter.ToString(CultureInfo.InvariantCulture);
               break;
            }
         }
         if (String.IsNullOrEmpty(DriveLetter))
         {
            DriveLetter = "C:\\" + new Guid();
            Directory.CreateDirectory(DriveLetter);
         }
         SourceLocations.Add(new SourceLocation(@"C:\"));
      }
   }

   [DataContract]
   public class SourceLocation
   {
      [DataMember] public string SourcePath = string.Empty;

      [DataMember] public bool UseAsNameRoot = false;

      [DataMember] public bool UseIsReadOnly = false;

      // paramterless contrsuctor to allow serialisation
      public SourceLocation()
      {
      }

      public SourceLocation(string path, bool useAsRoot=false, bool isReadOnly=false)
      {
         SourcePath = path;
         UseAsNameRoot = useAsRoot;
         UseIsReadOnly = isReadOnly;
      }

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("SourceLocation=[{0}]", SourcePath).AppendLine();
         sb = sb.AppendFormat("UseAsNameRoot=[{0}]", UseAsNameRoot).AppendLine();
         sb = sb.AppendFormat("UseIsReadOnly=[{0}]", UseIsReadOnly).AppendLine();
         return sb.ToString();
      }
   }
}