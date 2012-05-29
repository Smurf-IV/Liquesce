#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ConfigDetails.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Smurf-IV
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
using System.Runtime.Serialization;

namespace LiquesceFacade
{
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

   /// <summary>
   /// This is the class that will dump out the details to the XML File.
   /// </summary>
   [DataContract]
   public class ConfigDetails
   {
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
      public string DriveLetter = "N";

      [DataMember]
      public ushort ThreadCount = 0;

      [DataMember]
      public bool DebugMode = false;

      [DataMember(IsRequired = true)]
      public string VolumeLabel = "Mirror of C";

      [DataMember]
      public AllocationModes AllocationMode = AllocationModes.folder;

      [DataMember]
      public UInt64 HoldOffBufferBytes = 1L << 10 << 10 << 10; // ==1GB;

      [DataMember(IsRequired = true)]
      public List<string> SourceLocations;

      [DataMember]
      public string ServiceLogLevel = "Warn"; // NLog's LogLevel.Debug.ToString()

      [DataMember]
      public List<LanManShareDetails> SharesToRestore;

      [DataMember]
      public UInt16 CacheLifetimeSeconds = 32; // Set to zero to disable

      public List<string> KnownSharePaths;
   }
}