using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using NLog;

// using System.Security.Principal;

namespace LiquesceFaçade
{

   public class LanManShareHandler
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

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
            details.UserAccessRules = new List<UserAccessRuleExport>();
            ManagementBaseObject securityDescriptor;
            ManagementObject sharedFolder = GetSharedFolder(details.Name, out securityDescriptor);
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
                           DomainUserIdentity = String.Format( @"{0}\{1}", Trustee.Properties["Domain"].Value, Trustee.Properties["Name"].Value ),
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
         // Creating security descriptor for the share
         ManagementClass securityDescriptorDefault = new ManagementClass("Win32_SecurityDescriptor");
         securityDescriptorDefault.Properties["ControlFlags"].Value = 0x8; // Indicates an SD with a default DACL. the object receives the default DACL from the access token of the creator 
         inParams["Access"] = securityDescriptorDefault;


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
         if ((share.UserAccessRules != null) 
            && (share.UserAccessRules.Count > 0))
         {
            // share create succeeded -- assign permissions to given set of users now
            AssignPermissionsToUsers(share.Name, share.UserAccessRules);
         }
      }


      /// <summary>
      /// Assign permissions to given set of users in the specified share
      /// </summary>
      /// <param name="shareName">Name of the share to be changed</param>
      /// <param name="users">set of users</param>
      /// <param name="warnings"></param>
      /// <returns></returns>
      public static void AssignPermissionsToUsers(string shareName, List<UserAccessRuleExport> users)
      {
         List<string> warningsList = new List<string>();

         // Step 1: Get the security descriptor of the shared folder object
         ManagementBaseObject securityDescriptor;
         ManagementObject sharedFolder = GetSharedFolder(shareName, out securityDescriptor);
         if ((sharedFolder == null)
            || (securityDescriptor == null)
            )
         {
            return;
         }

         // Step 2: Create an access control list to be assigned
         List<ManagementObject> accessControlList = new List<ManagementObject>();

         // Step 3: Add all the users to the access control list
         foreach (UserAccessRuleExport user in users)
         {
            string domainName = string.Empty;
            string userName = string.Empty;

            SplitUserName(user.DomainUserIdentity, ref domainName, ref userName);

            // Getting the user account object
            ManagementObject userAccountObject = GetUserAccountObject(domainName, userName);
            if (userAccountObject != null)
            {
               ManagementObject securityIdentfierObject = new ManagementObject(string.Format("Win32_SID.SID='{0}'", (string)userAccountObject.Properties["SID"].Value));
               securityIdentfierObject.Get();

               // Creating Trustee Object
               ManagementObject trusteeObject = CreateTrustee(domainName, userName, securityIdentfierObject);

               // Creating Access Control Entry
               ManagementObject accessControlEntry = CreateAccessControlEntry(trusteeObject, user.InheritanceFlags, user.Type, user.AccessMask);

               // Adding entry to access control list
               accessControlList.Add(accessControlEntry);
            }
            else
            {
               Log.Warn("The user with Domain-'" + domainName + "' and Name-'" + userName
                            + "' could not be found. No permissions set for the user");
            }
         }

         if (accessControlList.Count > 0)
         {
            // Step 4: Assign access Control list to security desciptor
            securityDescriptor.Properties["DACL"].Value = accessControlList.ToArray();

            // Step 5: Setting the security descriptor for the shared folder
            ManagementBaseObject parameterForSetSecurityDescriptor = sharedFolder.GetMethodParameters("SetSecurityDescriptor");
            parameterForSetSecurityDescriptor["Descriptor"] = securityDescriptor;
            sharedFolder.InvokeMethod("SetSecurityDescriptor", parameterForSetSecurityDescriptor, null);
         }
         else
         {
            Log.Warn("No valid usernames given. Default permissions set.");
         }

      }

      private static ManagementObject GetSharedFolder(string shareName, out ManagementBaseObject securityDescriptor)
      {
         securityDescriptor = null;
         ManagementObject sharedFolder = GetSharedFolderSecuritySettingObject(shareName);
         if (sharedFolder == null)
         {
            Log.Error("The shared folder with given name does not exist");
            return sharedFolder;
         }
         ManagementBaseObject securityDescriptorObject = sharedFolder.InvokeMethod("GetSecurityDescriptor", null, null);
         if (securityDescriptorObject == null)
         {
            Log.Error("Error extracting security descriptor of the shared path {0}.", shareName);
            return sharedFolder;
         }
         int returnCode = Convert.ToInt32(securityDescriptorObject.Properties["ReturnValue"].Value);
         if (returnCode != 0)
         {
            Log.Error("Error extracting security descriptor of the shared path {0}. Error Code{1}.", shareName, returnCode);
         }

         securityDescriptor = (ManagementBaseObject)securityDescriptorObject.Properties["Descriptor"].Value;
         return sharedFolder;
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
         ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_LogicalShareSecuritySetting where Name = '" + sharedFolderName + "'");
         ManagementObjectCollection resultOfSearch = searcher.Get();
         if (resultOfSearch.Count > 0)
         {
            //The search might return a number of objects with same shared name. I assume there is just going to be one
            sharedFolderObject = resultOfSearch.Cast<ManagementObject>().FirstOrDefault();
         }
         return sharedFolderObject;
      }


      /// <summary>
      /// Create a trustee object for the given user
      /// </summary>
      /// <param name="domain">name of domain</param>
      /// <param name="userName">the network name of the user</param>
      /// <param name="securityIdentifierOfUser">Object containing User's sid</param>
      /// <returns></returns>
      private static ManagementObject CreateTrustee(string domain, string userName, ManagementObject securityIdentifierOfUser)
      {
         ManagementObject trusteeObject = new ManagementClass("Win32_Trustee").CreateInstance();
         trusteeObject.Properties["Domain"].Value = domain;
         trusteeObject.Properties["Name"].Value = userName;
         trusteeObject.Properties["SID"].Value = securityIdentifierOfUser.Properties["BinaryRepresentation"].Value;
         trusteeObject.Properties["SidLength"].Value = securityIdentifierOfUser.Properties["SidLength"].Value;
         trusteeObject.Properties["SIDString"].Value = securityIdentifierOfUser.Properties["SID"].Value;
         return trusteeObject;
      }

      /// <summary>
      /// The method returns ManagementObject object for the user folder with given name
      /// </summary>
      /// <param name="domain">string containing domain name of user </param>
      /// <param name="alias">string containing the user's network name </param>
      /// <returns>Object of type ManagementObject for the user folder.</returns>
      private static ManagementObject GetUserAccountObject(string domain, string alias)
      {
         ManagementObject userAccountObject = null;
         ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("select * from Win32_Account where Name = '{0}' and Domain='{1}'", alias, domain));
         ManagementObjectCollection resultOfSearch = searcher.Get();
         if (resultOfSearch.Count > 0)
         {
            userAccountObject = resultOfSearch.Cast<ManagementObject>().FirstOrDefault();
         }
         return userAccountObject;
      }

      // Splits full user name into domain name and username
      // assumes that username and domain name are split by '\'
      private static void SplitUserName(string fullName, ref string domainName, ref string userName)
      {
         domainName = string.Empty;
         userName = string.Empty;

         // splitting domain name and user name
         if (fullName.Contains("\\") == true)
         {
            string[] info = fullName.Split('\\');
            if (info.Length >= 2)
            {
               domainName = info[0].Trim();
               userName = info[1].Trim();
            }
            else
            {
               userName = fullName;
            }
         }
         else
         {
            userName = fullName;
         }
      }

      /// <summary>
      /// Create an Access Control Entry object for the given user
      /// </summary>
      /// <param name="trustee">The user's trustee object</param>
      /// <param name="deny">boolean to say if user permissions should be assigned or denied</param>
      /// <returns></returns>
      private static ManagementObject CreateAccessControlEntry(ManagementObject trustee, AceFlags inheritanceFlags, AceType type, Mask accessMask)
      {
         ManagementObject aceObject = new ManagementClass("Win32_ACE").CreateInstance();

         aceObject.Properties["AccessMask"].Value = accessMask;
         aceObject.Properties["AceFlags"].Value = inheritanceFlags;
         aceObject.Properties["AceType"].Value = type;
         aceObject.Properties["Trustee"].Value = trustee;
         return aceObject;
      }        

      //static void GetDACLBits(string strPath, string strFullPath)
      //{

      //   using (ManagementObject lfs = new

      //   ManagementObject(@"Win32_LogicalFileSecuritySetting.Path=" + "'" + strPath + "'"))
      //   {

      //      // Get the security descriptor for this object

      //      // Dump all trustees (this includes owner)

      //      ManagementBaseObject outParams =

      //      lfs.InvokeMethod("GetSecurityDescriptor", null, null);

      //      if (((uint)(outParams.Properties["ReturnValue"].Value)) == 0) // if success
      //      {

      //         ManagementBaseObject secDescriptor =

      //         ((ManagementBaseObject)(outParams.Properties["Descriptor"].Value));

      //         //The DACL is an array of Win32_ACE objects.

      //         ManagementBaseObject[] dacl =

      //         ((ManagementBaseObject[])(secDescriptor.Properties["Dacl"].Value));

      //         DumpACEs(dacl, strFullPath);

      //      }

      //   }

      //}

      static void DumpACEs(ManagementBaseObject[] dacl, string strFullPath)
      {

         foreach (ManagementBaseObject mbo in dacl)
         {

            try
            {

               ManagementBaseObject Trustee = ((ManagementBaseObject)(mbo["Trustee"]));

               if ((Convert.ToInt32(mbo["AceFlags"])) != 19)
               {

                  // Dump trustees

                  Console.WriteLine("Trustee: {1}" + @"\" + "{0}\n", Trustee.Properties["Name"].Value, Trustee.Properties["Domain"].Value);

                  // Dump ACE mask in readable form

                  UInt32 mask = (UInt32)mbo["AccessMask"];

                  Console.WriteLine(Enum.Format(typeof(Mask), mask, "g"));

                  Console.WriteLine("\n" + "AceFlags: " + mbo["AceFlags"].ToString());

                  Console.WriteLine("______________________________________\n" + strFullPath);

               }

            }

            catch (Exception e)
            {

               Console.WriteLine("The process failed: {0}", e.ToString());

            }

         }

      }
   }
}
