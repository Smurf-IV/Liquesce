#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="AdvancedPropertiesDisplay.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2013 Simon Coghlan (Aka Smurf-IV)
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
using System.Drawing.Design;
using LiquesceFacade;
using NLog;

namespace Liquesce
{
   // ReSharper disable MemberCanBePrivate.Global
   // ReSharper disable UnusedMember.Global
   // Needs to be global to allow the propertgrid reflector to the accessors
   public class AdvancedPropertiesDisplay
   {
      private readonly ConfigDetails cd;

      public AdvancedPropertiesDisplay(ConfigDetails cd)
      {
         this.cd = cd;
      }
      [DescriptionAttribute("The number of MilliSecs to wait before the service attempts to start to 'Load' the mount point.\r" +
                            "Used to allow USB devices or network to become enabled before committing the mount space.\r" +
                            "Range 0 <-> 10000000"),
       DisplayName("Delay Start in MilliSec")
      , CategoryAttribute("Service")
      ]
      [TypeConverter(typeof(NumericUpDownTypeConverter))]
      [Editor(typeof(NumericUpDownTypeEditor), typeof(UITypeEditor)), MinMaxAttribute(0, 10000000, 500)]
      public uint DelayStartMilliSec
      {
         get { return cd.DelayStartMilliSec; }
         set { cd.DelayStartMilliSec = value; }
      }


      [DescriptionAttribute("Number of free MegaBytes to leave, before attempting to use another drive to write to.\r" +
                            "Used in Priority and Folder modes.\r" +
                            "Range 1 <-> 1024000"),
       DisplayName("Hold Off Buffer")
      , CategoryAttribute("File")
      ]
      [TypeConverter(typeof (NumericUpDownTypeConverter))]
      [Editor(typeof (NumericUpDownTypeEditor), typeof (UITypeEditor)), MinMaxAttribute(1, 1024000, 1024)]
      public ulong HoldOffMBytes
      {
         get { return cd.HoldOffBufferBytes/(1024*1024); }
         set { cd.HoldOffBufferBytes = value*(1024*1024); }
      }

      [DescriptionAttribute("0 is automatic (Number of processing units * 2), use 1 for problem finding scenario's.\rRange 0 <-> 31"),
       DisplayName("Thread Count")
      , CategoryAttribute("CBFS")
      ]
      [TypeConverter(typeof (NumericUpDownTypeConverter))]
      [Editor(typeof (NumericUpDownTypeEditor), typeof (UITypeEditor)), MinMaxAttribute(0, 31)]
      public ushort ThreadCount
      {
         get { return cd.ThreadCount; }
         set { cd.ThreadCount = value; }
      }

      [DescriptionAttribute("The amount of information that will be placed into the Log files.\r" +
                            "Trace means slower performance!\r." +
                            "Useful for creating bug reports - set Thread Count to 1 as well"),
       DisplayName("Service Logging Level")
      , CategoryAttribute("Service")
      ]
      [TypeConverter(typeof (ServiceLogLevelValues))]
      public string ServiceLogLevel
      {
         get { return cd.ServiceLogLevel; }
         set { cd.ServiceLogLevel = value; }
      }


      [DescriptionAttribute("The allocation strategy applied to new files or folders on how they are placed on the storage disks:\r" +
                            "Folder = try to keep files together on one disk (classic behavior)\r" +
                            "Priority = strict one disk after the other method\r" +
                            "Balanced = balance the available space on all storage disks; Whichever disc has the most space will be used\n" +
                            "See http://liquesce.codeplex.com/wikipage?title=How%20are%20the%20files%20spread%20across%20the%20drives%20%3f"
         ),
       DisplayName("Disk Allocation Mode")]
      [TypeConverter(typeof (AllocationModeValues)), CategoryAttribute("File")]
      public String AllocationMode
      {
         get { return cd.AllocationMode.ToString(); }
         set { Enum.TryParse(value, out cd.AllocationMode); }
      }

      [DescriptionAttribute("Cache the file details. This will improve the speed of file discovery and opening.\r" +
                            "Range is 0 <-> 65535"),
       DisplayName("File Detail cache seconds")
      , CategoryAttribute("File")
      ]
      [TypeConverter(typeof (NumericUpDownTypeConverter))]
      [Editor(typeof (NumericUpDownTypeEditor), typeof (UITypeEditor)), MinMaxAttribute(UInt16.MaxValue)]
      public UInt16 CacheLifetimeSeconds
      {
         get { return cd.CacheLifetimeSeconds; }
         set { cd.CacheLifetimeSeconds = value; }
      }
   }
   // ReSharper restore UnusedMember.Global
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
         return new StandardValuesCollection(new[] { LogLevel.Fatal.Name, LogLevel.Error.Name, LogLevel.Warn.Name, LogLevel.Info.Name, LogLevel.Debug.Name, LogLevel.Trace.Name });
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