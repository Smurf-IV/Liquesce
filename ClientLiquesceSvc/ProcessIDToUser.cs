using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Threading;
using NLog;

// Forward declarations
using HANDLE = System.IntPtr;

namespace ClientLiquesceSvc
{
   // Some code stolen from http://bytes.com/topic/c-sharp/answers/463942-using-openprocesstoken
   //
   internal static class ProcessIDToUser
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      static private readonly Dictionary<string, string> quickLookup = new Dictionary<string, string>();
      static private readonly ReaderWriterLockSlim quickLookupSync = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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
            // weight of the internal calls to get the processName, it appears that you still need a process.Handle to the Win32 API's
            // Also the internal Get### calls all use a CLR .Net processManager which seems to handle a lot of this in the cache;
            // So speed will be the same unless this is forked out to a C++ / Win32 DLL to make the calls directly !
            processName = process.ProcessName;
            string key = processName + PID;
            try
            {
               quickLookupSync.EnterUpgradeableReadLock();
               if (!quickLookup.TryGetValue(key, out domainUser))
               {
                  quickLookupSync.EnterWriteLock();
                  ProcessStartInfo startInfo = process.StartInfo;
                  if (!String.IsNullOrEmpty(startInfo.Domain)
                      && !String.IsNullOrEmpty(startInfo.UserName)
                     )
                  {
                     domainUser = String.Format(@"{0}\{1}", startInfo.Domain, startInfo.UserName);
                  }
                  else
                  {
                     HANDLE hToken = new HANDLE();
                     try
                     {
                        if (!OpenProcessToken(process.Handle, (int) TOKEN_READ, ref hToken))
                           throw new ApplicationException("Could not get process token.  Win32 Error Code: " +
                                                          Marshal.GetLastWin32Error());
                        else
                        {
                           domainUser = new WindowsIdentity(hToken).Name;
                        }
                     }
                     finally
                     {
                        CloseHandle(hToken);
                     }
                     quickLookup[key] = domainUser;
                  }
               }
            }
            finally
            {
               if (quickLookupSync.IsWriteLockHeld)
                  quickLookupSync.ExitWriteLock();
               quickLookupSync.ExitUpgradeableReadLock();
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

      private const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
      private const uint STANDARD_RIGHTS_READ = 0x00020000;
      private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
      private const uint TOKEN_DUPLICATE = 0x0002;
      private const uint TOKEN_IMPERSONATE = 0x0004;
      private const uint TOKEN_QUERY = 0x0008;
      private const uint TOKEN_QUERY_SOURCE = 0x0010;
      private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
      private const uint TOKEN_ADJUST_GROUPS = 0x0040;
      private const uint TOKEN_ADJUST_DEFAULT = 0x0080;
      private const uint TOKEN_ADJUST_SESSIONID = 0x0100;
      private const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
      private const uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
          TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
          TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
          TOKEN_ADJUST_SESSIONID);

      [DllImport("advapi32", SetLastError = true),
      SuppressUnmanagedCodeSecurityAttribute]
      static extern bool OpenProcessToken(
          HANDLE ProcessHandle, // handle to process
          int DesiredAccess, // desired access to process
          ref IntPtr TokenHandle // handle to open access token
      );

      [DllImport("kernel32", SetLastError = true),
      SuppressUnmanagedCodeSecurityAttribute]
      static extern bool CloseHandle(HANDLE handle);

   }
}
