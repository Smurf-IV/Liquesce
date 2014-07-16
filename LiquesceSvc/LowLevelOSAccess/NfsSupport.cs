using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;
using NLog;

namespace LiquesceSvc.LowLevelOSAccess
{
   internal static class NfsSupport
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      #region GetByFileId Stuff

      private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

      /// <summary>
      /// Calls the necessary function to find the Filename from the Id
      /// stolen from the Mapper example application and code from http://www.osronline.com/showThread.cfm?link=123420
      /// </summary>
      /// <param name="rootToCheck"></param>
      /// <param name="fileId"></param>
      /// <returns> string.Empty for nothing found</returns>
      public static string GetByFileId(string rootToCheck, long fileId)
      {
         // Create them externally so that they can be tidied up in the finally statement.
         SafeFileHandle rootHandle = null;
         SafeFileHandle fileHandle = null;

         try
         {
            const string urlString = @"\\.\";
            rootToCheck = urlString + rootToCheck;
            rootHandle = CreateFileW(rootToCheck, 0,
               FileShare.ReadWrite | FileShare.Delete,
               IntPtr.Zero,
               FileMode.Open,
               FILE_FLAG_BACKUP_SEMANTICS,
               IntPtr.Zero);
            if (rootHandle.IsInvalid)
            {
               throw new Win32Exception();
            }

            using (HGlobal fileIdBuffer = new HGlobal(Marshal.SizeOf(fileId)))
            {
               Marshal.WriteInt64(fileIdBuffer, fileId);

               UNICODE_STRING name = new UNICODE_STRING();
               name.Length = name.MaximumLength = 8;
               name.Buffer = fileIdBuffer;

               using (HGlobal nameBuffer = new HGlobal(Marshal.SizeOf(name)))
               {
                  Marshal.StructureToPtr(name, nameBuffer, false);

                  OBJECT_ATTRIBUTES objAttributes = new OBJECT_ATTRIBUTES();
                  objAttributes.Length = (uint)Marshal.SizeOf(objAttributes);
                  objAttributes.ObjectName = nameBuffer;
                  objAttributes.RootDirectory = rootHandle.DangerousGetHandle();
                  objAttributes.Attributes = 0;
                  objAttributes.SecurityDescriptor = IntPtr.Zero;
                  objAttributes.SecurityQualityOfService = IntPtr.Zero;

                  IO_STATUS_BLOCK iosb = new IO_STATUS_BLOCK();

                  uint status = NtOpenFile(out fileHandle,
                     (uint)NativeFileOps.EFileAccess.FILE_READ_ATTRIBUTES,
                     ref objAttributes,
                     ref iosb,
                     FileShare.ReadWrite,
                     0x00002000 //FILE_OPEN_BY_FILE_ID
                     );
                  if (status != 0)
                  {
                     int win32Error = (int)LsaNtStatusToWinError(status);
                     throw new Win32Exception(win32Error);
                  }
               }
            }

            const int fileNameInfoBufferSize = 512;

            using (HGlobal fileNameInfoBuffer = new HGlobal(fileNameInfoBufferSize))
            {
               if (!GetFileInformationByHandleEx(fileHandle, FILE_INFO_BY_HANDLE_CLASS.FileNameInfo,
                  fileNameInfoBuffer,
                  fileNameInfoBufferSize))
               {
                  throw new Win32Exception();
               }

               // Get data from the output buffer
               int dataLength = Marshal.ReadInt32(fileNameInfoBuffer);
               IntPtr stringOffset = new IntPtr(((IntPtr)fileNameInfoBuffer).ToInt32() + sizeof(int));
               string filePath = Marshal.PtrToStringUni(stringOffset, dataLength / sizeof(char));
               // File path also contains root part of mRootPath (without drive letter).
               // Remove it.
               const string driveColon = "X:";
               return filePath.Substring(rootToCheck.Length - driveColon.Length);
            }
         }
         catch (Exception ex)
         {
            Log.InfoException(string.Format("GetByFileId could not open [{0}]", rootToCheck), ex);
            return string.Empty;
         }
         finally
         {
            try
            {
               if (fileHandle != null)
               {
                  fileHandle.Close();
               }
               if (rootHandle != null)
               {
                  rootHandle.Close();
               }
            }
            catch (Exception e)
            {
               Log.ErrorException("GetByFileId finally threw:", e);
            }
         }
      }

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall,
   SetLastError = true)]
      private static extern SafeFileHandle CreateFileW(
         string lpFileName,
         uint dwDesiredAccess,
         FileShare dwShareMode,
         IntPtr SecurityAttributes,
         FileMode dwCreationDisposition,
         uint dwFlagsAndAttributes,
         IntPtr hTemplateFile
         );

      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      private struct OBJECT_ATTRIBUTES
      {
         internal uint Length;
         internal IntPtr RootDirectory;
         internal IntPtr ObjectName;
         internal uint Attributes;
         internal IntPtr SecurityDescriptor;
         internal IntPtr SecurityQualityOfService;
      };

      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      private struct UNICODE_STRING
      {
         internal ushort Length;
         internal ushort MaximumLength;
         internal IntPtr Buffer;
      };

      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      private struct IO_STATUS_BLOCK
      {
         private readonly uint Status;
         private readonly ulong information;
      }

      [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = false)]
      private static extern uint NtOpenFile(
          out  SafeFileHandle FileHandle,
          uint DesiredAccess,
          ref OBJECT_ATTRIBUTES ObjectAttributes,
          ref IO_STATUS_BLOCK IoStatusBlock,
          FileShare ShareAccess,
          uint OpenOptions
          );

      // ReSharper disable UnusedMember.Local
      private enum FILE_ID_TYPE
      {
         FileIdType,
         MaximumFileIdType
      }

      // ReSharper disable NotAccessedField.Local
      private struct FILE_ID_DESCRIPTOR
      {
         private UInt32 dwSize;  // Size of the struct
         private FILE_ID_TYPE Type; // Describes the type of identifier passed in. 0 == Use the FileId member of the union.
         private Int64 FileId;   // A EXT_FILE_ID_128 structure containing the 128-bit file ID of the file. This is used on ReFS file systems.

         public FILE_ID_DESCRIPTOR(uint dwSize, FILE_ID_TYPE type, long fileId)
            : this()
         {
            this.dwSize = dwSize;
            Type = type;
            FileId = fileId;
         }
      };

      // ReSharper restore NotAccessedField.Local
      // ReSharper restore UnusedMember.Local

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern SafeFileHandle OpenFileById(
         SafeFileHandle hVolumeHint,
         FILE_ID_DESCRIPTOR lpFileId,
         UInt32 dwDesiredAccess,
         UInt32 dwShareMode,
         IntPtr lpSecurityAttributes, // Reserved.
         UInt32 dwFlagsAndAttributes
         );

      [DllImport("Advapi32.dll", ExactSpelling = true, SetLastError = false)]
      private static extern uint LsaNtStatusToWinError(uint Status);

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
      // ReSharper disable UnusedMember.Local
      private struct FILE_NAME_INFO
      {
         internal UInt32 FileNameLength;

         [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 512)]
         internal string FileName;
      }

      private enum FILE_INFO_BY_HANDLE_CLASS
      {
         FileNameInfo = 2,
         FileIdBothDirectoryInfo = 10
      }

      // ReSharper restore UnusedMember.Local

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool GetFileInformationByHandleEx(
          SafeFileHandle FileHandle,
          FILE_INFO_BY_HANDLE_CLASS FileInformationClass,
          IntPtr FileInformation,
          uint BufferSize
          );

      #endregion GetByFileId Stuff
   }
}