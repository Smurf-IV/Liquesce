﻿using System;
using System.Runtime.InteropServices;

// David Amenta - dave@DaveAmenta.com
// 05/05/08
// Updated 04/18/2010 for x64

// ReSharper disable CheckNamespace
namespace DeleteToRecycleBin
// ReSharper restore CheckNamespace
{
   /// <summary>
   /// Send files directly to the recycle bin.
   /// </summary>
   public static class RecybleBin
   {
      /// <summary>
      /// Possible flags for the SHFileOperation method.
      /// </summary>
      [Flags]
      public enum FileOperationFlags : ushort
      {
         /// <summary>
         /// Do not show a dialog during the process
         /// </summary>
         FOF_SILENT = 0x0004,
         /// <summary>
         /// Do not ask the user to confirm selection
         /// </summary>
         FOF_NOCONFIRMATION = 0x0010,
         /// <summary>
         /// Delete the file to the recycle bin.  (Required flag to send a file to the bin
         /// </summary>
         FOF_ALLOWUNDO = 0x0040,
         /// <summary>
         /// Do not show the names of the files or folders that are being recycled.
         /// </summary>
         FOF_SIMPLEPROGRESS = 0x0100,
         /// <summary>
         /// Surpress errors, if any occur during the process.
         /// </summary>
         FOF_NOERRORUI = 0x0400,
         /// <summary>
         /// Warn if files are too big to fit in the recycle bin and will need
         /// to be deleted completely.
         /// </summary>
         FOF_WANTNUKEWARNING = 0x4000,
      }

      /// <summary>
      /// File Operation Function Type for SHFileOperation
      /// </summary>
      private enum FileOperationType : uint
      {
         // ReSharper disable UnusedMember.Local
         /// <summary>
         /// Move the objects
         /// </summary>
         FO_MOVE = 0x0001,
         /// <summary>
         /// Copy the objects
         /// </summary>
         FO_COPY = 0x0002,
         /// <summary>
         /// Delete (or recycle) the objects
         /// </summary>
         FO_DELETE = 0x0003,
         /// <summary>
         /// Rename the object(s)
         /// </summary>
         FO_RENAME = 0x0004,
         // ReSharper restore UnusedMember.Local
      }

      /// <summary>
      /// SHFILEOPSTRUCT for SHFileOperation from COM
      /// </summary>
      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
      private struct SHFILEOPSTRUCT_x86
      {
         private readonly IntPtr hwnd;
         [MarshalAs(UnmanagedType.U4)]
         public FileOperationType wFunc;
         public string pFrom;
         private readonly string pTo;
         public FileOperationFlags fFlags;
         [MarshalAs(UnmanagedType.Bool)]
         private readonly bool fAnyOperationsAborted;
         private readonly IntPtr hNameMappings;
         private readonly string lpszProgressTitle;
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      private struct SHFILEOPSTRUCT_x64
      {
         private readonly IntPtr hwnd;
         [MarshalAs(UnmanagedType.U4)]
         public FileOperationType wFunc;
         public string pFrom;
         private readonly string pTo;
         public FileOperationFlags fFlags;
         [MarshalAs(UnmanagedType.Bool)] 
         private readonly bool fAnyOperationsAborted;
         private readonly IntPtr hNameMappings;
         private readonly string lpszProgressTitle;
      }

      [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
      private static extern int SHFileOperation_x86(ref SHFILEOPSTRUCT_x86 FileOp);

      [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
      private static extern int SHFileOperation_x64(ref SHFILEOPSTRUCT_x64 FileOp);

      private static bool IsWOW64Process()
      {
         return IntPtr.Size == 8;
      }

      /// <summary>
      /// Send file to recycle bin
      /// </summary>
      /// <param name="path">Location of directory or file to recycle</param>
      /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
      public static bool Send(string path, FileOperationFlags flags)
      {
         try
         {
            if (IsWOW64Process())
            {
               SHFILEOPSTRUCT_x64 fs = new SHFILEOPSTRUCT_x64
                                          {
                                             wFunc = FileOperationType.FO_DELETE,
                                             pFrom = path + '\0' + '\0',
                                             fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
                                          };
               // important to double-terminate the string.
               SHFileOperation_x64(ref fs);
            }
            else
            {
               SHFILEOPSTRUCT_x86 fs = new SHFILEOPSTRUCT_x86
                                          {
                                             wFunc = FileOperationType.FO_DELETE,
                                             pFrom = path + '\0' + '\0',
                                             fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
                                          };
               // important to double-terminate the string.
               SHFileOperation_x86(ref fs);
            }
            return true;
         }
         catch
         {
            return false;
         }
      }

      /// <summary>
      /// Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
      /// </summary>
      /// <param name="path">Location of directory or file to recycle</param>
      public static bool Send(string path)
      {
         return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
      }

      /// <summary>
      /// Send file silently to recycle bin.  Surpress dialog, surpress errors, delete if too large.
      /// </summary>
      /// <param name="path">Location of directory or file to recycle</param>
      public static bool SendSilent(string path)
      {
         return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT);
      }
   }
}
