using System;
using System.Runtime.InteropServices;

namespace LiquesceFaçade
{
   internal static class NetApi32
   {
      public enum NetError
      {
         NERR_Success = 0,
         NERR_BASE = 2100,
         NERR_UnknownDevDir = (NERR_BASE + 16),
         NERR_DuplicateShare = (NERR_BASE + 18),
         NERR_BufTooSmall = (NERR_BASE + 23),
      }

      public enum SHARE_TYPE : ulong
      {
         STYPE_DISKTREE = 0,
         STYPE_PRINTQ = 1,
         STYPE_DEVICE = 2,
         STYPE_IPC = 3,
         STYPE_SPECIAL = 0x80000000,
      }

      [DllImport("Netapi32.dll", SetLastError = true)]
      public static extern int NetShareGetInfo(
          [MarshalAs(UnmanagedType.LPWStr)] string serverName,
          [MarshalAs(UnmanagedType.LPWStr)] string netName,
          Int32 level,
          out IntPtr bufPtr);


      [StructLayout(LayoutKind.Sequential)]
      public struct SHARE_INFO_502
      {
         [MarshalAs(UnmanagedType.LPWStr)]
         public string shi502_netname;
         public uint shi502_type;
         [MarshalAs(UnmanagedType.LPWStr)]
         public string shi502_remark;
         public Int32 shi502_permissions;
         public Int32 shi502_max_uses;
         public Int32 shi502_current_uses;
         [MarshalAs(UnmanagedType.LPWStr)]
         public string shi502_path;
         public IntPtr shi502_passwd;
         public Int32 shi502_reserved;
         public IntPtr shi502_security_descriptor;
      }

      [DllImport("Netapi32.dll")]
      public static extern int NetShareAdd(
              [MarshalAs(UnmanagedType.LPWStr)]
        string strServer, Int32 dwLevel, IntPtr buf, IntPtr parm_err);


   }

   class AD_ShareUtil
   {
      [STAThread]
      static void Main(string[] args)
      {
         string strServer = @"HellRaiser";
         string strShareFolder = @"G:\Mp3folder";
         string strShareName = @"MyMP3Share";
         string strShareDesc = @"Share to store MP3 files";
         NetApi32.NetError nRetVal = 0;
         AD_ShareUtil shUtil = new AD_ShareUtil();
         nRetVal = shUtil.CreateShare(strServer,
           strShareFolder, strShareName, strShareDesc, false);
         if (nRetVal == NetApi32.NetError.NERR_Success)
         {
            Console.WriteLine("Share {0} created", strShareName);
         }
         else if (nRetVal == NetApi32.NetError.NERR_DuplicateShare)
         {
            Console.WriteLine("Share {0} already exists",
                      strShareName);
         }
      }

      NetApi32.NetError CreateShare(string strServer,
                                    string strPath,
                                    string strShareName,
                                    string strShareDesc,
                                    bool bAdmin)
      {
         NetApi32.SHARE_INFO_502 shInfo =new NetApi32.SHARE_INFO_502();
         shInfo.shi502_netname = strShareName;
         shInfo.shi502_type =
             (uint)NetApi32.SHARE_TYPE.STYPE_DISKTREE;
         if (bAdmin)
         {
            shInfo.shi502_type =
                (uint)NetApi32.SHARE_TYPE.STYPE_SPECIAL;
            shInfo.shi502_netname += "$";
         }
         shInfo.shi502_permissions = 0;
         shInfo.shi502_path = strPath;
         shInfo.shi502_passwd = IntPtr.Zero;
         shInfo.shi502_remark = strShareDesc;
         shInfo.shi502_max_uses = -1;
         shInfo.shi502_security_descriptor = IntPtr.Zero;

         string strTargetServer = strServer;
         if (strServer.Length != 0)
         {
            strTargetServer = strServer;
            if (strServer[0] != '\\')
            {
               strTargetServer = "\\\\" + strServer;
            }
         }
         int nRetValue = 0;
         // Call Net API to add the share..
         int nStSize = Marshal.SizeOf(shInfo);
         IntPtr buffer = Marshal.AllocCoTaskMem(nStSize);
         Marshal.StructureToPtr(shInfo, buffer, false);
         nRetValue = NetApi32.NetShareAdd(strTargetServer, 502,
                 buffer, IntPtr.Zero);
         Marshal.FreeCoTaskMem(buffer);

         return (NetApi32.NetError)nRetValue;
      }
   }
}
