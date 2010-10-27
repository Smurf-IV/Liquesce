using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using NLog;

// Forward declarations
using HANDLE = System.IntPtr;

namespace ClientLiquesceSvc
{
   // Code stolen from http://www.codeproject.com/KB/cs/processownersid.aspx
   //
   internal static class ProcessIDToUser
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private const int TOKEN_QUERY = 0X00000008;

      enum TOKEN_INFORMATION_CLASS
      {
         TokenUser = 1,
         TokenGroups,
         TokenPrivileges,
         TokenOwner,
         TokenPrimaryGroup,
         TokenDefaultDacl,
         TokenSource,
         TokenType,
         TokenImpersonationLevel,
         TokenStatistics,
         TokenRestrictedSids,
         TokenSessionId
      }


      [StructLayout(LayoutKind.Sequential)]
      struct TOKEN_USER
      {
         public _SID_AND_ATTRIBUTES User;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct _SID_AND_ATTRIBUTES
      {
         public readonly IntPtr Sid;
         private readonly int Attributes;
      }

      [DllImport("advapi32")]
      static extern bool OpenProcessToken(
          HANDLE ProcessHandle, // handle to process
          int DesiredAccess, // desired access to process
          ref IntPtr TokenHandle // handle to open access token
      );

      [DllImport("kernel32")]
      static extern HANDLE GetCurrentProcess();

      [DllImport("advapi32", CharSet = CharSet.Auto)]
      static extern bool GetTokenInformation(
          HANDLE hToken,
          TOKEN_INFORMATION_CLASS tokenInfoClass,
          IntPtr TokenInformation,
          int tokeInfoLength,
          ref int reqLength
      );

      [DllImport("kernel32")]
      static extern bool CloseHandle(HANDLE handle);

      [DllImport("advapi32", CharSet = CharSet.Auto)]
      static extern bool ConvertSidToStringSid(
          IntPtr pSID,
          [In, Out, MarshalAs(UnmanagedType.LPTStr)] ref string pStringSid
      );

      [DllImport("advapi32", CharSet = CharSet.Auto)]
      static extern bool ConvertStringSidToSid(
          [In, MarshalAs(UnmanagedType.LPTStr)] string pStringSid,
          ref IntPtr pSID
      );

      /// <summary>
      /// Collect User Info
      /// </summary>
      /// <param name="pToken">Process Handle</param>
      /// <param name="SID"></param>
      private static bool GetUserSidPtr(HANDLE pToken, out IntPtr SID)
      {
         const int Access = TOKEN_QUERY;
         HANDLE procToken = IntPtr.Zero;
         bool ret = false;
         SID = IntPtr.Zero;
         try
         {
            if (OpenProcessToken(pToken, Access, ref procToken))
            {
               ret = ProcessTokenToSid(procToken, out SID);
               CloseHandle(procToken);
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.WarnException("GetUserSidPtr", ex);
            return false;
         }
      }

      private static bool ProcessTokenToSid(HANDLE token, out IntPtr SID)
      {
         TOKEN_USER tokUser;
         const int bufLength = 256;
         IntPtr tu = Marshal.AllocHGlobal(bufLength);
         SID = IntPtr.Zero;
         try
         {
            int cb = bufLength;
            bool ret = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenUser, tu, cb, ref cb);
            if (ret)
            {
               tokUser = (TOKEN_USER)Marshal.PtrToStructure(tu, typeof(TOKEN_USER));
               SID = tokUser.User.Sid;
            }
            return ret;
         }
         catch (Exception ex)
         {
            Log.WarnException("ProcessTokenToSid", ex);
            return false;
         }
         finally
         {
            Marshal.FreeHGlobal(tu);
         }
      }


      /// <summary>
      /// From the ProcessID, this will attempt to get the Domain and UserName that currently owns it
      /// </summary>
      /// <param name="PID">Process ID</param>
      /// <returns>Domain then User</returns>
      public static string GetDomainUserFromPID(uint PID)
      {
         string domainUser = "Unknown";
         string processName = String.Empty;
         try
         {
            Process process = Process.GetProcessById((int)PID);
            // Having had a go at trying to work out how to do this with out having to use the 
            // wieght of the internal calls to get the processName, it appears that you still need a process.Handle to the Win32 API's
            // The idea was to make a Map of process ID / App name to the user for quick lookup, as the ProcessID's can be recycled
            // Also the internal Get### calls all use a processManager which seems to handle a lot of this in the cache;
            // So speed will be the same unless this is forked out to a C++ / Win32 DLL to make the calls directly !
            processName = process.ProcessName;
            ProcessStartInfo startInfo = process.StartInfo;
            if (!String.IsNullOrEmpty(startInfo.Domain)
               && !String.IsNullOrEmpty(startInfo.UserName)
               )
            {
               domainUser = String.Format(@"{0}\{1}", startInfo.Domain, startInfo.UserName);
            }
            else
            {
               IntPtr pSid;
               if (GetUserSidPtr(process.Handle, out pSid))
               {
                  // convert the user sid to a domain\name
                  domainUser = new SecurityIdentifier(pSid).Translate(typeof (NTAccount)).ToString();
               }
            }
         }
         catch
         {
            if (String.IsNullOrEmpty(processName))
               processName = PID.ToString();
         }
         Log.Trace("domainUser[{0}] found for PID[{1}] = {2}", domainUser, PID, processName);
         return domainUser;
      }
   }
}
