#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="LanManShareHandler.cs" company="Smurf-IV">
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

#endregion Copyright (C)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using NLog;

// using System.Security.Principal;

namespace LiquesceFacade
{
   public class LanManShareHandler
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private List<LanManShareDetails> GotShares;

      private void GetLanManShares()
      {
         List<LanManShareDetails> shares = new List<LanManShareDetails>();
         ManagementClass objClass = new ManagementClass("Win32_Share");
         foreach (ManagementObject objShare in objClass.GetInstances().Cast<ManagementObject>())
         {
            try
            {
               string type = objShare["Type"].ToString();

               if (type == "0") // 0 = DiskDrive (1 = Print Queue, 2 = Device, 3 = IPH)
               {
                  PropertyData ong = objShare.Properties["MaximumAllowed"];
                  LanManShareDetails shareDetails = new LanManShareDetails
                  {
                     MaxConnectionsNum = (ong.Value == null) ? UInt32.MaxValue : Convert.ToUInt32(ong.Value),
                     Name = Convert.ToString(objShare.Properties["Name"].Value),
                     Path = Convert.ToString(objShare.Properties["Path"].Value),
                     Description = Convert.ToString(objShare.Properties["Description"].Value)   // TODO: Should this be "Caption" ??
                  };
                  shares.Add(shareDetails);
               }
            }
            catch
            {
            }
         }
         GotShares = shares;
      }

      public List<LanManShareDetails> MatchDriveLanManShares(string DriveLetter)
      {
         if (GotShares == null)
         {
            GetLanManShares();
         }
         Debug.Assert(GotShares != null, "GotShares != null");
         List<LanManShareDetails> lmsd = GotShares.FindAll(share => share.Path.StartsWith(DriveLetter));
         foreach (LanManShareDetails details in lmsd)
         {
            details.UserAccessRules = new List<UserAccessRuleExport>();
            ManagementBaseObject securityDescriptor;
            GetSharedFolder(details.Name, out securityDescriptor);
            if (securityDescriptor != null)
            {
               //The DACL is an array of Win32_ACE objects.

               ManagementBaseObject[] dacl = ((ManagementBaseObject[])(securityDescriptor.Properties["Dacl"].Value));
               foreach (ManagementBaseObject mbo in dacl)
               {
                  try
                  {
                     ManagementBaseObject Trustee = ((ManagementBaseObject)(mbo["Trustee"]));
                     int aceFlags = Convert.ToInt32(mbo["AceFlags"]);
                     if (aceFlags != 19)
                     {
                        int accessMask = Convert.ToInt32(mbo["AccessMask"]);
                        int aceType = Convert.ToInt32(mbo["AceType"]);
                        details.UserAccessRules.Add(new UserAccessRuleExport
                        {
                           DomainUserIdentity = String.Format(@"{0}\{1}", Trustee.Properties["Domain"].Value, Trustee.Properties["Name"].Value),
                           AccessMask = (Mask)accessMask,
                           InheritanceFlags = (AceFlags)aceFlags,
                           Type = (AceType)aceType
                        });
                     }
                  }
                  catch (Exception e)
                  {
                     Log.ErrorException("The Trustee extraction failed:", e);
                  }
               }
            }
         }
         return lmsd;
      }

      // http://msdn.microsoft.com/en-us/library/aa389393%28VS.85%29.aspx
      public static void SetLanManShare(LanManShareDetails share)
      {
         try
         {
            ManagementObject shareMO = new ManagementObject(string.Format("Win32_Share='{0}'", share.Name));
            if (shareMO != null)
            {
               Log.Warn("[{0}] may already be shared.", share.Name);
               ManagementBaseObject inDeleteParams = null;
               ManagementBaseObject outDeleteParams = shareMO.InvokeMethod("Delete", inDeleteParams, null);
               if (outDeleteParams != null)
               {
                  Log.Warn("Attempt to remove share returned [{0}]",
                           Convert.ToUInt32(outDeleteParams.Properties["ReturnValue"].Value));
               }
               else
                  Log.Error("Attempt to remove share returned null!");
            }
         }
         catch (Exception ex)
         {
            Log.WarnException("Sharing may not work if this is not \"Not Found\"", ex);
         }
         ManagementClass managementClass = new ManagementClass("Win32_Share");

         ManagementBaseObject inParams = managementClass.GetMethodParameters("Create");

         object[] userobj = new object[share.UserAccessRules.Count];
         for (int index = 0; index < share.UserAccessRules.Count; index++)
         {
            UserAccessRuleExport userAccessRule = share.UserAccessRules[index];
            ManagementObject mUser = new ManagementClass(new ManagementPath("Win32_Trustee"), null);
            string domainName = string.Empty;
            string userName = string.Empty;
            SplitUserName(userAccessRule.DomainUserIdentity, ref domainName, ref userName);

            NTAccount ntAccountUser = new NTAccount(domainName, userName);
            SecurityIdentifier sidUser = (SecurityIdentifier)ntAccountUser.Translate(typeof(SecurityIdentifier));
            byte[] sidArrayUser = new byte[sidUser.BinaryLength];
            sidUser.GetBinaryForm(sidArrayUser, 0);
            mUser["SID"] = sidArrayUser;

            ManagementObject mACEUser = new ManagementClass(new ManagementPath("Win32_Ace"), null);
            mACEUser["AccessMask"] = userAccessRule.AccessMask;
            mACEUser["AceFlags"] = userAccessRule.InheritanceFlags;
            mACEUser["AceType"] = userAccessRule.Type;
            mACEUser["Trustee"] = mUser;

            userobj[index] = mACEUser;
         }

         ManagementObject secDescriptor = new ManagementClass(new ManagementPath("Win32_SecurityDescriptor"), null);
         // securityDescriptorDefault.Properties["ControlFlags"].Value = 0x8; // Indicates an SD with a default DACL. the object receives the default DACL from the access token of the creator
         secDescriptor["ControlFlags"] = 4 + 256;  //
         secDescriptor["DACL"] = userobj;

         inParams["Description"] = share.Description;
         inParams["Access"] = secDescriptor;
         inParams["Name"] = share.Name;
         inParams["Path"] = share.Path;
         inParams["MaximumAllowed"] = share.MaxConnectionsNum;
         inParams["Type"] = 0x0;

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
                  //case 8:
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
      }

      private static void GetSharedFolder(string shareName, out ManagementBaseObject securityDescriptor)
      {
         securityDescriptor = null;
         ManagementObject sharedFolder = GetSharedFolderSecuritySettingObject(shareName);
         if (sharedFolder == null)
         {
            Log.Info("The shared folder with given name does not exist");
            return;
         }
         ManagementBaseObject securityDescriptorObject = sharedFolder.InvokeMethod("GetSecurityDescriptor", null, null);
         if (securityDescriptorObject == null)
         {
            Log.Info("Error extracting security descriptor of the shared path {0}.", shareName);
            return;
         }
         int returnCode = Convert.ToInt32(securityDescriptorObject.Properties["ReturnValue"].Value);
         if (returnCode != 0)
         {
            Log.Error("Error extracting security descriptor of the shared path {0}. Error Code{1}.", shareName, returnCode);
         }

         securityDescriptor = (ManagementBaseObject)securityDescriptorObject.Properties["Descriptor"].Value;
         return;
      }

      /// <summary>
      /// The method returns SecuritySetting ManagementObject object for the shared folder with given name
      /// </summary>
      /// <param name="sharedFolderName">string containing name of shared folder</param>
      /// <returns>Object of type ManagementObject for the shared folder.</returns>
      private static ManagementObject GetSharedFolderSecuritySettingObject(string sharedFolderName)
      {
         ManagementObject sharedFolderObject = null;

         //Creating a searcher object to search
         ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("Select * from Win32_LogicalShareSecuritySetting where Name = '{0}'", sharedFolderName));
         ManagementObjectCollection resultOfSearch = searcher.Get();
         if (resultOfSearch.Count > 0)
         {
            //The search might return a number of objects with same shared name. I assume there is just going to be one
            sharedFolderObject = resultOfSearch.Cast<ManagementObject>().FirstOrDefault();
         }
         return sharedFolderObject;
      }

      // Splits full user name into domain name and username
      // assumes that username and domain name are split by '\'
      private static void SplitUserName(string fullName, ref string domainName, ref string userName)
      {
         domainName = string.Empty;
         userName = string.Empty;

         // splitting domain name and user name
         if (fullName.Contains("\\"))
         {
            string[] info = fullName.Split('\\');
            if (info.Length >= 2)
            {
               domainName = info[0].Trim();
               userName = info[1].Trim();
            }
            else
            {
               userName = info[0].Trim();
            }
         }
         else
         {
            userName = fullName;
         }
      }
   }
}