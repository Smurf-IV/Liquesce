using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.AccessControl;

namespace LiquesceFaçade
{

   public class LanManShareHandler
   {
      static public List<LanManShareDetails> GetLanManShares()
      {
         List<LanManShareDetails> shares = new List<LanManShareDetails>();
         ManagementClass objClass = new ManagementClass("Win32_Share");
         foreach (ManagementObject objShare in objClass.GetInstances())
         {
            try
            {
               PropertyData ong = objShare.Properties["MaximumAllowed"];
               LanManShareDetails shareDetails = new LanManShareDetails
                                                    {
                                                       MaxConnectionsNum = (ong.Value == null) ? UInt32.MaxValue : Convert.ToUInt32(ong.Value),
                                                       Name = Convert.ToString(objShare.Properties["Name"].Value),
                                                       Path = Convert.ToString(objShare.Properties["Path"].Value),
                                                       Description =
                                                          Convert.ToString(objShare.Properties["Description"].Value)
                                                    };
               shares.Add(shareDetails);
            }
            catch
            {
            }
         }
         return shares;
      }

      static public List<LanManShareDetails> MatchDriveLanManShares(string DriveLetter)
      {
         List<LanManShareDetails> lmsd = GetLanManShares().FindAll(share => share.Path.StartsWith(DriveLetter));
         foreach (LanManShareDetails details in lmsd)
         {
            DirectorySecurity dirSec = new DirectorySecurity(details.Path, AccessControlSections.All);
            details.ExportedRules = new List<FileSystemAccessRuleExport>();
            foreach (FileSystemAccessRule fsar in dirSec.GetAccessRules(true, false, typeof (System.Security.Principal.NTAccount)) )
            {
               details.ExportedRules.Add(new FileSystemAccessRuleExport
               {
                  Identity = fsar.IdentityReference.Value,
                  fileSystemRights = ((int)fsar.FileSystemRights == -1) ? FileSystemRights.FullControl : fsar.FileSystemRights,
                  inheritanceFlags = fsar.InheritanceFlags,
                  propagationFlags = fsar.PropagationFlags,
                  Type = fsar.AccessControlType
               } );
            } 
         }
         return lmsd;
      }

      // http://msdn.microsoft.com/en-us/library/aa389393%28VS.85%29.aspx
      public static void SetLanManShare(LanManShareDetails share)
      {
         // Create a ManagementClass object
         ManagementClass managementClass = new ManagementClass("Win32_Share");

         // Create ManagementBaseObjects for in and out parameters
         ManagementBaseObject inParams = managementClass.GetMethodParameters("Create");

         // Set the input parameters
         inParams["Description"] = share.Description;
         inParams["Name"] = share.Name;
         inParams["Path"] = share.Path;
         inParams["MaximumAllowed"] = share.MaxConnectionsNum;
         inParams["Type"] = 0; // Disk Drive


         // Invoke the method on the ManagementClass object
         ManagementBaseObject outParams = managementClass.InvokeMethod("Create", inParams, null);

         // Check to see if the method invocation was successful
         string exceptionText = String.Empty;
         if (outParams != null)
         {
            UInt32 ret = Convert.ToUInt32(outParams.Properties["ReturnValue"].Value);
            switch (ret)
            {
               case 0: // Success
                  break;
               case 2:
                  exceptionText = "Access Denied";
                  break;
               default:
               case 8:
                  exceptionText = String.Format("Unknown Failure [{0}]", ret);
                  break;
               case 9:
                  exceptionText = "Invalid Name";
                  break;
               case 10:
                  exceptionText = "Invalid Level";
                  break;
               case 21:
                  exceptionText = "Invalid Parameter";
                  break;
               case 22:
                  // This one is okay as we are recreating the share !
                  // exceptionText = "Duplicate Share";
                  break;
               case 23:
                  exceptionText = "Redirected Path";
                  break;
               case 24:
                  exceptionText = "Unknown Device or Directory";
                  break;
               case 25:
                  exceptionText = "Net Name Not Found";
                  break;

            }
         }
         else
            exceptionText = "Unable to get return state";

         if (!String.IsNullOrEmpty(exceptionText))
            throw new Exception(exceptionText);
         // It appears that trying to use dirInfo.GetAccessControl and then AddAccessRule et al. work okay on NTFS but as this
         // is a DOKAN FS, it goes and cries (Well actually does not report a failure !!!!)


         DirectoryInfo dirInfo = new DirectoryInfo(share.Path);
         DirectorySecurity security = dirInfo.GetAccessControl(AccessControlSections.Access);
         foreach (FileSystemAccessRuleExport fsare in share.ExportedRules)
         {
            // Add Access rule for the inheritance
            FileSystemAccessRule fsar = new FileSystemAccessRule(fsare.Identity, fsare.fileSystemRights,
               fsare.inheritanceFlags, fsare.propagationFlags, fsare.Type);
            security.AddAccessRule(fsar);
         }
         dirInfo.SetAccessControl(security);
      }
   }
}
