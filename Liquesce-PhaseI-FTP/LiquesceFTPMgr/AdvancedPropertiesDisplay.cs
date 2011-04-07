using System;
using System.ComponentModel;
using LiquesceFTPFacade;
using NLog;

namespace LiquesceFTPMgr
{
    // ReSharper disable MemberCanBePrivate.Global
    // Needs to be global to allow the propertgrid reflector to the accessors
    public class AdvancedPropertiesDisplay
    {
        public AdvancedPropertiesDisplay(ConfigDetails cd)
        {
            if (cd != null)
            {
                AllocationMode = cd.AllocationMode.ToString();
                HoldOffMBytes = cd.HoldOffBufferBytes / (1024 * 1024);
                BufferReadSizeKBytes = cd.BufferReadSize / 1024;
                ServiceLogLevel = cd.ServiceLogLevel;
            }
        }

        private uint bufferReadSizeKBytes;

        [DescriptionAttribute("The number of KBytes allocated to buffered    file reading (4KB is the OS default!).\rRange 1 <-> 256"),
        DisplayName("Buffer Read Size")
        , CategoryAttribute("File")
        ]
        public uint BufferReadSizeKBytes
        {
            get { return bufferReadSizeKBytes; }
            set
            {
                if (value >= 1
                && value <= 256) bufferReadSizeKBytes = value;
            }
        }

        private ulong holdOffMBytes;

        [DescriptionAttribute("Number of free MegaBytes to leave, before attempting to use another drive to write to.\rRange 1 <-> 1024000"),
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


        [DescriptionAttribute("The amount of information that will be placed into the Log files (Trace means slower performance!)."),
        DisplayName("Service Logging Level"),
        TypeConverter(typeof(ServiceLogLevelValues))
        , CategoryAttribute("Service")
        ]
        public string ServiceLogLevel { get; set; }


        [DescriptionAttribute("The allocation strategy how new files or folders are placed on the storage disks:\n" +
            "folder = try to keep files together on one disk (classic behavior)\n" +
            "priority = strict one disk after the other method\n" +
            "balanced = balance the availabel space on all storage disks\n" + 
            "backup = balanced with a \"_backup\" folder to get a secure allocated backup"
            ),
        DisplayName("Disk Allocation Mode")
        , TypeConverter(typeof(AllocationModeValues))
        , CategoryAttribute("File")
        ]
        public String AllocationMode  { get; set; }
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
            } );
        }
    }



}