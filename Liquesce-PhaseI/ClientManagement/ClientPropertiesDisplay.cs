using System;
using System.ComponentModel;
using LiquesceFacade;

namespace ClientManagement
{
   // ReSharper disable UnusedAutoPropertyAccessor.Global
   // ReSharper disable MemberCanBePrivate.Global
    // Needs to be global to allow the propertgrid reflector to the accessors
   public class ClientPropertiesDisplay
   {
      public ClientPropertiesDisplay(ClientShareDetail csd)
      {
         if (csd != null)
         {
            TargetMachineName = csd.TargetMachineName;
            DomainUserIdentity = csd.DomainUserIdentity;
            TargetShareName = csd.TargetShareName;
            DriveLetter = csd.DriveLetter;
            VolumeLabel = csd.VolumeLabel;
            BufferWireTransferSize = csd.BufferWireTransferSize;
            ShowAsNetworkDrive = csd.ShowAsNetworkDrive;
         }
      }

      [DescriptionAttribute("This will let the Drive be displayed as if it was a network drive (Recommended)"),
      DisplayName("Show in Explorer as a Network drive")
      , CategoryAttribute("Local")
      ]
      public bool ShowAsNetworkDrive { get; set; }

      [DescriptionAttribute("The number of Bytes allocated to be chunked over the Wire.\rRange 4096 <-> near 1GB"),
      DisplayName("Buffer Wire Transfer Size")
      , CategoryAttribute("Local")
      ]
      public int BufferWireTransferSize
      {
         get {
            return bufferWireTransferSize;
         }
         set
         {
            if (value >= 1 << 12
                && value <= 1 << 30)
               bufferWireTransferSize = value;
         }
      }

      [DescriptionAttribute("The Name to be used in explorer."),
      DisplayName("Drive Label")
      , CategoryAttribute("Local")
      , ReadOnly(true)
      ]
      public string VolumeLabel { get; set; }

      [DescriptionAttribute("The drive letter used in explorer"),
      DisplayName("Drive Letter")
      , CategoryAttribute("Local")
      ]
      public string DriveLetter { get; set; }

      [DescriptionAttribute("The name allocated to the share on the target machine."),
      DisplayName("Target Share Connect")
      , CategoryAttribute("Remote")
      , ReadOnly(true)
      ]
      public string TargetShareName { get; set; }

      [DescriptionAttribute("The User to be used as the ACL Check up."),
      DisplayName("User to connect to the target share")
      , CategoryAttribute("Remote")
      , ReadOnly(true)
      ]
      public string DomainUserIdentity { get; set; }

      [DescriptionAttribute("The machine name or IP address of the Target share.\rPress test to ensure that it is connectable.\rNOTE: If the share is not visible then try disabling (temporarily) the firewalls between the machines to see if the applications are not allow communications."),
      DisplayName("Target Machine")
      , CategoryAttribute("Remote")
      ]
      public string TargetMachineName { get; set; }

      private int bufferWireTransferSize;
      // ReSharper restore MemberCanBePrivate.Global
      // ReSharper restore UnusedAutoPropertyAccessor.Global

   }
}