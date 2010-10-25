using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
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
      private static bool DumpUserInfo(HANDLE pToken, out IntPtr SID)
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
            Log.WarnException("DumpUserInfo", ex);
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

      public static string ExGetProcessInfoByPID(int PID, out string SID)//, out string OwnerSID)
      {
         SID = String.Empty;
         try
         {
            Process process = Process.GetProcessById(PID);
            IntPtr _SID;
            if (DumpUserInfo(process.Handle, out _SID))
            {
               ConvertSidToStringSid(_SID, ref SID);
            }
            return process.ProcessName;
         }
         catch
         {
            return "Unknown";
         }
      }
   }
}
