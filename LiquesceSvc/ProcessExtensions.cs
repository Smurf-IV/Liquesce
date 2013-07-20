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
      /// <summary>
      /// Required to query an access token.
      /// </summary>
      /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa374905%28v=vs.85%29.aspx
      private const uint TOKEN_DUPLICATE = 0x0002;
      private const uint TOKEN_IMPERSONATE = 0x0004;
      private const uint TOKEN_QUERY = 0x0008;
      const int SecurityImpersonation = 2;

      /// <summary>
      /// Returns the WindowsIdentity associated to a Process
      /// </summary>
      /// <param name="process">The Windows Process.</param>
      /// <returns>The WindowsIdentity of the Process.</returns>
      /// <remarks>Be prepared for 'Access Denied' Exceptions</remarks>
      [SecuritySafeCritical]
      [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
      [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlPrincipal)]
      public static WindowsIdentity WindowsIdentity(this Process process)
      {
         IntPtr ph = IntPtr.Zero;
         IntPtr dupeTokenHandle = IntPtr.Zero;
         WindowsIdentity wi;

         try
         {
            OpenProcessToken(process.Handle, TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY, out ph);
            // Hint of Duplicate handle from
            // http://msdn.microsoft.com/en-us/library/system.security.principal.windowsidentity.impersonate%28v=vs.71%29.aspx
            wi = new WindowsIdentity(DuplicateToken(ph, SecurityImpersonation, ref dupeTokenHandle)?dupeTokenHandle:ph);
         }
         catch
         {
            throw;
         }
         finally
         {
            if (dupeTokenHandle != IntPtr.Zero)
               CloseHandle(dupeTokenHandle);

            if (ph != IntPtr.Zero)
               CloseHandle(ph);
         }
         return wi;
      }


      [DllImport("advapi32.dll", SetLastError = true)]
      private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

      [DllImport("kernel32.dll", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CloseHandle(IntPtr hObject);

      [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private extern static bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

   }
}
