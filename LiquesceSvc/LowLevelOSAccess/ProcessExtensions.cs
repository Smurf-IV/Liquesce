//-----------------------------------------------------------------------
// <copyright file="ProcessExtensions.cs" company="DockOfTheBay">
//     http://dotbay.blogspot.com/2009/06/finding-owner-of-process-in-c.html
//    Modification by Simon Coghlan (Aka Smurf-IV)
// </copyright>
// <summary>Defines the ProcessExtensions class.</summary>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;

// ProcessExtensions.cs(15,40): warning CS0618: 'System.Security.Permissions.SecurityAction.RequestMinimum' is obsolete: 'Assembly level declarative security is obsolete and is no longer enforced by the CLR by default.
// [assembly: SecurityPermissionAttribute(SecurityAction.RequestMinimum, UnmanagedCode = true)]
// [assembly: PermissionSetAttribute(SecurityAction.RequestMinimum, Name = "FullTrust")]
namespace LiquesceSvc
{
   // If you incorporate this code into a DLL, be sure to demand FullTrust.
   //[PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
   public static class ProcessExtensions
   {
      private static readonly bool isWinVistaOrHigher = IsWinVistaOrHigher();

      private static bool IsWinVistaOrHigher()
      {
         OperatingSystem OS = Environment.OSVersion;
         return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6);
      }

      /// <summary>
      /// Returns the WindowsIdentity associated to a Process
      /// </summary>
      /// <param name="process">The Windows Process.</param>
      /// <returns>The WindowsIdentity of the Process.</returns>
      /// <remarks>Be prepared for 'Access Denied' Exceptions</remarks>
      [SecuritySafeCritical]
      [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
      [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
      [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
      public static WindowsIdentity WindowsIdentity(this Process process)
      {
         IntPtr ph = IntPtr.Zero;
         IntPtr dupeTokenHandle = IntPtr.Zero;
         IntPtr hprocess = IntPtr.Zero;

         try
         {
            WindowsIdentity wi;
            // If you absolutely need every process identity without exception or guesswork, you’ll need to run this code under an NT Service running as System.
            // process.Handle fails for applications like MsMpEng on Windows 8.1
            // So get a handle that hard way ! http://www.aboutmycode.com/net-framework/how-to-get-elevated-process-path-in-net/
            hprocess = OpenProcess((isWinVistaOrHigher ? ProcessAccessFlags.QueryLimitedInformation : ProcessAccessFlags.QueryInformation), false, process.Id);

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (OpenProcessToken(hprocess, TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY, out ph))
            {
               // Hint of Duplicate handle from
               // http://msdn.microsoft.com/en-us/library/system.security.principal.windowsidentity.impersonate%28v=vs.71%29.aspx
               wi = new WindowsIdentity(DuplicateToken(ph, SecurityImpersonationLevel.SecurityImpersonation, ref dupeTokenHandle) ? dupeTokenHandle : ph);
            }
            else
            {
               // Might be running under a console app without the correct permissions
               wi = System.Security.Principal.WindowsIdentity.GetCurrent();
            }
            return wi;
         }
         finally
         {
            if (dupeTokenHandle != IntPtr.Zero)
               CloseHandle(dupeTokenHandle);

            if (ph != IntPtr.Zero)
               CloseHandle(ph);

            if (hprocess != IntPtr.Zero)
               CloseHandle(hprocess);
         }
      }

      [Flags]
      private enum ProcessAccessFlags : uint
      {
         // ReSharper disable UnusedMember.Local
         All = 0x001F0FFF,  // This is the default used by the Process class
         Terminate = 0x00000001,
         CreateThread = 0x00000002,
         VMOperation = 0x00000008,
         VMRead = 0x00000010,
         VMWrite = 0x00000020,
         DupHandle = 0x00000040,
         SetInformation = 0x00000200,
         QueryInformation = 0x00000400,
         QueryLimitedInformation = 0x00001000,  // Windows Server 2003 and Windows XP:  This access right is not supported.
         Synchronize = 0x00100000
         // ReSharper restore UnusedMember.Local
      }
      [DllImport("kernel32.dll")]
      private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

      // ReSharper disable UnusedMember.Local
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

      // ReSharper restore UnusedMember.Local

      [DllImport("advapi32.dll", SetLastError = true)]
      private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CloseHandle(IntPtr hObject);

      private enum SecurityImpersonationLevel
      {
         // ReSharper disable UnusedMember.Local

         /// <summary>
         /// The server process cannot obtain identification information about the client,
         /// and it cannot impersonate the client. It is defined with no value given, and thus,
         /// by ANSI C rules, defaults to a value of zero.
         /// </summary>
         SecurityAnonymous = 0,

         /// <summary>
         /// The server process can obtain information about the client, such as security identifiers and privileges,
         /// but it cannot impersonate the client. This is useful for servers that export their own objects,
         /// for example, database products that export tables and views.
         /// Using the retrieved client-security information, the server can make access-validation decisions without
         /// being able to use other services that are using the client's security context.
         /// </summary>
         SecurityIdentification = 1,

         /// <summary>
         /// The server process can impersonate the client's security context on its local system.
         /// The server cannot impersonate the client on remote systems.
         /// </summary>
         SecurityImpersonation = 2,

         /// <summary>
         /// The server process can impersonate the client's security context on remote systems.
         /// NOTE: Windows NT:  This impersonation level is not supported.
         /// </summary>
         SecurityDelegation = 3,
         // ReSharper restore UnusedMember.Local
      }
      [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private extern static bool DuplicateToken(IntPtr ExistingTokenHandle, SecurityImpersonationLevel SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);
   }
}