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
      static private readonly string configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductNameCBFS, "Properties.config.xml");

      public void InitConfigDetails()
      {
         string[] drives = Environment.GetLogicalDrives();
         List<char> driveLetters = new List<char>(26);
         driveLetters.AddRange(drives.Select(dr => dr.ToUpper()[0]));
         // Reverse find the 1st letter not used 
         for (int i = 0; i < 26; i++)
         {
            char letter = (char) ('Z' - i);
            if ( !driveLetters.Contains(letter) )
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
      }

      public static void ReadConfigDetails(ref ConfigDetails currentConfigDetails)
      {
         try
         {
            InitialiseToDefault(ref currentConfigDetails);
            XmlSerializer x = new XmlSerializer(currentConfigDetails.GetType());
            Log.Info("Attempting to read Drive details from: [{0}]", configFile);
            using (TextReader textReader = new StreamReader(configFile))
            {
               currentConfigDetails = x.Deserialize(textReader) as ConfigDetails;
            }
            Log.Info("Now normalise the paths to allow the file finders to work correctly");
            if (currentConfigDetails != null)
            {
               List<string> fileSourceLocations = new List<string>(currentConfigDetails.SourceLocations);
               currentConfigDetails.SourceLocations.Clear();

               foreach (
                  string location in
                     fileSourceLocations.Select(
                        fileSourceLocation => Path.GetFullPath(fileSourceLocation).TrimEnd(Path.DirectorySeparatorChar))
                                        .Where(location => OkToAddThisDriveType(location))
                  )
               {
                  currentConfigDetails.SourceLocations.Add(location);
               }

            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot read the configDetails: ", ex);
            currentConfigDetails = null;
         }
         finally
         {
            if (currentConfigDetails == null)
            {
               InitialiseToDefault(ref currentConfigDetails);
               if (!File.Exists(configFile))
                  WriteOutConfigDetails(currentConfigDetails);
            }
         }
      }

      private static bool OkToAddThisDriveType(string dr)
      {
         bool seemsOK = false;
         try
         {
            Log.Debug(dr);
            DriveInfo di = new DriveInfo(dr);
            DriveType driveType = di.DriveType;
            switch (driveType)
            {
               case DriveType.Removable:
               case DriveType.Fixed:
                  {
                     string di_DriveFormat = di.DriveFormat;
                     switch (di_DriveFormat.ToUpper())
                     {
                        case "CBFS":
                           Log.Warn("Removing the existing CBFS drive as this would cause confusion ! [{0}]",
                                    di.Name);
                           seemsOK = false;
                           break;
                        case "FAT":
                           Log.Warn("Removing FAT formated drive type, as this causes ACL Failures [{0}]", di.Name);
                           seemsOK = false;
                           break;
                        default:
                           seemsOK = true;
                           break;
                     }
                  }
                  break;
               case DriveType.Unknown:
               case DriveType.NoRootDirectory:
               case DriveType.Network:
               case DriveType.CDRom:
               case DriveType.Ram:
                  seemsOK = true;
                  break;
               default:
                  throw new ArgumentOutOfRangeException("driveType", "Unknown type detected");
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Check Drive Format Type threw:", ex);
            seemsOK = false;

         }
         return seemsOK;

      }

      public static void InitialiseToDefault(ref ConfigDetails currentConfigDetails)
      {
         try
         {
            if (currentConfigDetails == null)
            {
               currentConfigDetails = new ConfigDetails();
               currentConfigDetails.InitConfigDetails();
               currentConfigDetails.SourceLocations.Add(@"C:\");
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Cannot create the default configDetails: ", ex);
            currentConfigDetails = null;
         }
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
      public enum AllocationModes
      {
         folder
        ,priority
        ,balanced
      };

      [DataMember(IsRequired = true)]
      public uint DelayStartMilliSec = 250;

      // Make this is a string so that the XML looks better (Rather than exporting 72 for 'N')
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
      public string ServiceLogLevel = LogLevel.Fatal.Name; 

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