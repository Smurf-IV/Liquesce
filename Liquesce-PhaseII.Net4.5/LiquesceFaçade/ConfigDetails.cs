#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ConfigDetails.cs" company="Smurf-IV">
//
//  Copyright (C) 2010-2014 Simon Coghlan (Aka Smurf-IV)
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

#endregion Copyright (C)

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
         MountDetails.Add(mt);
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
      public ushort ThreadCount;

      [DataMember]
      public string ServiceLogLevel = LogLevel.Fatal.Name;

      [DataMember]
      public UInt16 CacheLifetimeSeconds = 32; // Set to zero to disable

      [DataMember]
      public bool UseInternalDriverCaches = true;

      [DataMember]
      public bool UseInplaceRenaming = false;


      [DataMember]
      public List<MountDetail> MountDetails = new List<MountDetail>();

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("DelayStartMilliSec=[{0}]", DelayStartMilliSec).AppendLine();
         sb = sb.AppendFormat("ThreadCount=[{0}]", ThreadCount).AppendLine();
         sb = sb.AppendFormat("ServiceLogLevel[{0}]", ServiceLogLevel).AppendLine();
         sb = sb.AppendFormat("CacheLifetimeSeconds=[{0}]", CacheLifetimeSeconds).AppendLine();
         sb = sb.AppendFormat("UseInternalDriverCaches=[{0}]", UseInternalDriverCaches).AppendLine();
         sb = sb.AppendFormat("UseInplaceRenaming=[{0}]", UseInplaceRenaming).AppendLine();
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
      public string DriveLetter; // InitConfig populates this to the last unused.

      [DataMember(IsRequired = true)]
      public string VolumeLabel = "Mirror of C";

      [DataMember]
      public AllocationModes AllocationMode = AllocationModes.Folder;

      [DataMember]
      public UInt64 HoldOffBufferBytes = 1L << 10 << 10 << 10; // ==1GB;

      [DataMember(IsRequired = true)]
      public List<SourceLocation> SourceLocations = new List<SourceLocation>();

      [DataMember]
      public List<LanManShareDetails> SharesToRestore;

      public bool UseInplaceRenaming = false;

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("\tDriveLetter=[{0}]", DriveLetter).AppendLine();
         sb = sb.AppendFormat("\tVolumeLabel=[{0}]", VolumeLabel).AppendLine();
         sb = sb.AppendFormat("\tAllocationMode=[{0}]", AllocationMode).AppendLine();
         sb = sb.AppendFormat("\tHoldOffBufferBytes=[{0:N0}]", HoldOffBufferBytes).AppendLine();
         sb = sb.AppendLine("\tSourceLocations:");
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
      [DataMember]
      public string SourcePath = string.Empty;

      [DataMember]
      public bool UseIsReadOnly;

      // paramterless contrsuctor to allow serialisation
      public SourceLocation()
      {
      }

      public SourceLocation(string path, bool isReadOnly = false)
      {
         SourcePath = path;
         UseIsReadOnly = isReadOnly;
      }

      public new string ToString()
      {
         StringBuilder sb = new StringBuilder();
         sb = sb.AppendFormat("\t\tSourceLocation=[{0}]", SourcePath).AppendLine();
         sb = sb.AppendFormat("\t\t\tUseIsReadOnly=[{0}]", UseIsReadOnly);
         return sb.ToString();
      }
   }

   #region Lan Share details
   // ReSharper disable UnusedMember.Global
   [Flags]
   public enum Mask : uint
   {
      FILE_READ_DATA = 0x00000001,
      FILE_WRITE_DATA = 0x00000002,
      FILE_APPEND_DATA = 0x00000004,
      FILE_READ_EA = 0x00000008,
      FILE_WRITE_EA = 0x00000010,
      FILE_EXECUTE = 0x00000020,
      FILE_DELETE_CHILD = 0x00000040,
      FILE_READ_ATTRIBUTES = 0x00000080,
      FILE_WRITE_ATTRIBUTES = 0x00000100,

      DELETE = 0x00010000,
      READ_CONTROL = 0x00020000,
      WRITE_DAC = 0x00040000,
      WRITE_OWNER = 0x00080000,
      SYNCHRONIZE = 0x00100000,

      ACCESS_SYSTEM_SECURITY = 0x01000000,
      MAXIMUM_ALLOWED = 0x02000000,

      GENERIC_ALL = 0x10000000,
      GENERIC_EXECUTE = 0x20000000,
      GENERIC_WRITE = 0x40000000,
      GENERIC_READ = 0x80000000,

      READ_NTFS = FILE_READ_DATA | FILE_READ_EA | FILE_EXECUTE | FILE_READ_ATTRIBUTES | READ_CONTROL | SYNCHRONIZE,
      CHANGE_NTFS = READ_NTFS + FILE_WRITE_DATA + FILE_APPEND_DATA + FILE_WRITE_EA + FILE_WRITE_ATTRIBUTES + DELETE,
      FULLCONTROL_NTFS = CHANGE_NTFS + FILE_DELETE_CHILD + WRITE_DAC + WRITE_OWNER,
   }

   [Flags]
   public enum AceFlags : int
   {
      ObjectInheritAce = 1,
      ContainerInheritAce = 2,
      NoPropagateInheritAce = 4,
      InheritOnlyAce = 8,
      InheritedAce = 16
   }

   [Flags]
   public enum AceType : int
   {
      AccessAllowed = 0,
      AccessDenied = 1,
      Audit = 2
   }
   // ReSharper restore UnusedMember.Global

   [DataContract]
   public class UserAccessRuleExport
   {
      [DataMember(IsRequired = true)]
      public string DomainUserIdentity;
      [DataMember(IsRequired = true)]
      public Mask AccessMask;
      [DataMember(IsRequired = true)]
      public AceFlags InheritanceFlags;
      [DataMember(IsRequired = true)]
      public AceType Type;
   }
   /// <summary>
   /// struct to hold the details required be the WMI objects to recreate the share
   /// </summary>
   [DataContract]
   public class LanManShareDetails
   {
      [DataMember(IsRequired = true)]
      public string Name;
      [DataMember(IsRequired = true)]
      public string Path; // *** Strip off trailing backslash - it isn't supported
      [DataMember]
      public string Description;
      [DataMember]
      public UInt32 MaxConnectionsNum;
      [DataMember]
      public List<UserAccessRuleExport> UserAccessRules;
   }


   #endregion

}