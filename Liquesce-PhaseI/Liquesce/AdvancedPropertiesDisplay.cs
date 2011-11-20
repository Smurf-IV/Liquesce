#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="AdvancedPropertiesDisplay.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.ComponentModel;
using LiquesceFacade;
using NLog;

namespace Liquesce
{
   // ReSharper disable MemberCanBePrivate.Global
   // Needs to be global to allow the propertgrid reflector to the accessors
   public class AdvancedPropertiesDisplay
   {
      public AdvancedPropertiesDisplay(ConfigDetails cd)
      {
         if (cd != null)
         {
            ThreadCount = cd.ThreadCount;
            DokanDebugMode = cd.DebugMode;
            AllocationMode = cd.AllocationMode.ToString();
            HoldOffMBytes = cd.HoldOffBufferBytes / (1024 * 1024);
            ServiceLogLevel = cd.ServiceLogLevel;
            CacheLifetimeSeconds = cd.CacheLifetimeSeconds;
         }
      }

      private ulong holdOffMBytes;

      [DescriptionAttribute("Number of free MegaBytes to leave, before attempting to use another drive to write to.\rUsed in Priority and Folder modes.\rRange 1 <-> 1024000"),
      DisplayName("Hold Off Buffer")
      , CategoryAttribute("File")
      ]
      public ulong HoldOffMBytes
      {
         get { return holdOffMBytes; }
         set
         {
            if (value >= 1
            && value <= 1024000) holdOffMBytes = value;
         }
      }

      [DescriptionAttribute("Turn Dokan Debug information on, to all it to be captured in an appropriate app."),
      DisplayName("Dokan Debug Mode")
      , CategoryAttribute("Dokan")
      ]
      public bool DokanDebugMode { get; set; }

      private ushort threadCount;

      [DescriptionAttribute("0 is automatic, use 1 for problem finding scenario's.\rRange 0 <-> 32"),
      DisplayName("Thread Count")
      , CategoryAttribute("Dokan")
      ]
      public ushort ThreadCount
      {
         get { return threadCount; }
         set
         {
            if (value >= 0
                && value <= 32)
               threadCount = value;
         }
      }

      [DescriptionAttribute("The amount of information that will be placed into the Log files (Trace means slower performance!)."),
      DisplayName("Service Logging Level"),
      TypeConverter(typeof(ServiceLogLevelValues))
      , CategoryAttribute("Service")
      ]
      public string ServiceLogLevel { get; set; }


      [DescriptionAttribute("The allocation strategy applied to new files or folders on how they are placed on the storage disks:\r" +
          "Folder = try to keep files together on one disk (classic behavior)\r" +
          "Priority = strict one disk after the other method\r" +
          "Balanced = balance the available space on all storage disks; Whichever disc has the most space will be used\n" +
          "See http://liquesce.codeplex.com/wikipage?title=How%20are%20the%20files%20spread%20across%20the%20drives%20%3f"
          ),
      DisplayName("Disk Allocation Mode")
      , TypeConverter(typeof(AllocationModeValues))
      , CategoryAttribute("File")
      ]
      public String AllocationMode { get; set; }

       [DescriptionAttribute("Cache the file details. This will improve the speed of file discovery and opening"),
      DisplayName("File Detail cache seconds"),
      TypeConverter(typeof(ServiceLogLevelValues))
      , CategoryAttribute("File")
      ]
      public UInt16 CacheLifetimeSeconds { get; set; }
   }
   // ReSharper restore MemberCanBePrivate.Global

   public class ServiceLogLevelValues : StringConverter
   {
      public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
      {
         //true means show a combobox
         return true;
      }
      public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
      {
         //true will limit to list. false will show the list, but allow free-form entry
         return true;
      }

      public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
      {
         return new StandardValuesCollection(new[] { LogLevel.Warn.ToString(), LogLevel.Debug.ToString(), LogLevel.Trace.ToString() });
      }
   }

   public class AllocationModeValues : StringConverter
   {
      public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
      {
         //true means show a combobox
         return true;
      }
      public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
      {
         //true will limit to list. false will show the list, but allow free-form entry
         return true;
      }

      public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
      {
         return new StandardValuesCollection(new[] { ConfigDetails.AllocationModes.folder.ToString(),
               ConfigDetails.AllocationModes.priority.ToString(), 
               ConfigDetails.AllocationModes.balanced.ToString()
            });
      }
   }



}