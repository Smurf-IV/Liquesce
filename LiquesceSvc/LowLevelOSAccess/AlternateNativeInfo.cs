using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LiquesceSvc
{
   public class AlternateNativeInfo
   {
      public FileStreamType StreamType { get; set; }
      public FileStreamAttributes StreamAttributes { get; set; }
      public string StreamName { get; set; }
      public long StreamSize { get; set; }
   }

   public class AlternateNameWrapper : IDisposable
   {
      private static readonly SafeHGlobalHandle _invalidBlock = SafeHGlobalHandle.Invalid();

      public AlternateNameWrapper()
      {
         MemoryBlock = _invalidBlock;
      }

      /// <summary>
      /// Returns the handle to the block of memory.
      /// </summary>
      /// <value>The <see cref="SafeHGlobalHandle"/> representing the block of memory.</value>
      public SafeHGlobalHandle MemoryBlock { get; set; }

      #region Methods

      /// <summary>
      /// Performs application-defined tasks associated with freeing, 
      /// releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
         if (!MemoryBlock.IsInvalid)
         {
            MemoryBlock.Dispose();
            MemoryBlock = _invalidBlock;
         }
      }

      /// <summary>
      /// Ensures that there is sufficient memory allocated.
      /// </summary>
      /// <param name="capacity">The required capacity of the block, in bytes.</param>
      /// <exception cref="OutOfMemoryException">There is insufficient memory to satisfy the request.</exception>
      public void EnsureCapacity(uint capacity)
      {
         uint currentSize = MemoryBlock.IsInvalid ? 0 : MemoryBlock.Size;
         if (capacity > currentSize)
         {
            if (0 != currentSize)
            {
               currentSize <<= 1;
            }
            if (capacity > currentSize)
            {
               currentSize = capacity;
            }
            if (!MemoryBlock.IsInvalid)
            {
               MemoryBlock.Dispose();
            }
            MemoryBlock = SafeHGlobalHandle.Allocate(currentSize);
         }
      }

      /// <summary>
      /// Reads the Unicode string from the memory block.
      /// </summary>
      /// <param name="length">The length of the string to read, in characters.</param>
      /// <returns>The string read from the memory block.</returns>
      public string ReadString(uint length)
      {
         if ((0 >= length)
             || MemoryBlock.IsInvalid
            )
         {
            return null;
         }
         if (length > MemoryBlock.Size)
         {
            length = MemoryBlock.Size;
         }
         return Marshal.PtrToStringUni(MemoryBlock.DangerousGetHandle(), (int) length);
      }

      /// <summary>
      /// Reads the string, and extracts the stream name.
      /// </summary>
      /// <param name="length">The length of the string to read, in characters.</param>
      /// <returns>The stream name./// </returns>
      public string ReadAlternateStreamName(uint length)
      {
         string name = ReadString(length);
         if (!string.IsNullOrEmpty(name))
         {
            // Name is of the format ":NAME:$DATA\0"
            int separatorIndex = name.IndexOf(NativeFileFind.StreamSeparator, 1);
            if (-1 != separatorIndex)
            {
               name = name.Substring(1, separatorIndex - 1);
            }
            else
            {
               // Should never happen!
               separatorIndex = name.IndexOf('\0');
               name = 1 < separatorIndex ? name.Substring(1, separatorIndex - 1) : null;
            }
         }

         return name;
      }

      #endregion
   }

   
   internal static class AlternativeStreamSupport
   {

      // "Characters whose integer representations are in the range from 1 through 31,
      // except for alternate streams where these characters are allowed"
      // http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
      private static readonly char[] InvalidStreamNameChars = Path.GetInvalidFileNameChars().Where(c => c < 1 || c > 31).ToArray();

      // ReSharper disable once UnusedParameter.Local
      private static void ValidateStreamName(string streamName)
      {
         if (!String.IsNullOrEmpty(streamName)
             && (-1 != streamName.IndexOfAny(InvalidStreamNameChars))
            )
         {
            // ERROR_INVALID_NAME = 123
            throw new Win32Exception(123, "The stream name contains invalid characters");
         }
      }

      public static List<AlternateNativeInfo> ListAlternateDataStreams(SafeFileHandle hFile)
      {
         List<AlternateNativeInfo> result = new List<AlternateNativeInfo>();

         using (AlternateNameWrapper alternateName = new AlternateNameWrapper())
         {
            if (!hFile.IsInvalid)
            {
               WIN32_STREAM_ID streamId = new WIN32_STREAM_ID();
               UInt32 dwStreamHeaderSize = (uint) Marshal.SizeOf(streamId);
               bool finished = false;
               IntPtr context = IntPtr.Zero;
               UInt32 bytesRead;

               try
               {
                  while (!finished)
                  {
                     // Read the next stream header:
                     if (!BackupRead(hFile, ref streamId, dwStreamHeaderSize, out bytesRead, false, false, ref context)
                         || (dwStreamHeaderSize != bytesRead)
                        )
                     {
                        finished = true;
                     }
                     else
                     {
                        // Read the stream name:
                        string foundStreamName = String.Empty;
                        if (0 != streamId.dwStreamNameSize)
                        {
                           alternateName.EnsureCapacity(streamId.dwStreamNameSize);
                           if (
                              !BackupRead(hFile, alternateName.MemoryBlock, streamId.dwStreamNameSize, out bytesRead, false, false,
                                 ref context))
                           {
                              foundStreamName = null;
                              finished = true;
                           }
                           else
                           {
                              // Unicode chars are 2 bytes:
                              foundStreamName = alternateName.ReadAlternateStreamName(bytesRead >> 1);
                           }
                        }

                        // Add the stream info to the result:
                        if (!String.IsNullOrEmpty(foundStreamName))
                        {
                           result.Add(new AlternateNativeInfo
                           {
                              StreamType = (FileStreamType) streamId.dwStreamType,
                              StreamAttributes = (FileStreamAttributes) streamId.dwStreamAttributes,
                              StreamSize = streamId.Size,
                              StreamName = foundStreamName
                           });
                        }

                        // Skip the contents of the stream:
                        uint bytesSeekedLow;
                        uint bytesSeekedHigh;
                        if (!finished
                            &&
                            !BackupSeek(hFile, (uint) (streamId.Size & 0xFFFFFFFF), (uint) (streamId.Size >> 32), out bytesSeekedLow,
                               out bytesSeekedHigh, ref context)
                           )
                        {
                           finished = true;
                        }
                     }
                  }
               }
               finally
               {
                  // Abort the backup:
                  BackupRead(hFile, alternateName.MemoryBlock, 0, out bytesRead, true, false, ref context);
               }
            }
         }

         return result;
      }

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool BackupRead(SafeFileHandle hFile, ref WIN32_STREAM_ID pBuffer,
                                            [In] UInt32 numberOfBytesToRead, out UInt32 numberOfBytesRead,
                                            [In] [MarshalAs(UnmanagedType.Bool)] bool abort,
                                            [In] [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
                                            ref IntPtr context);

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool BackupRead(SafeFileHandle hFile, SafeHGlobalHandle pBuffer,
                                            [In] UInt32 numberOfBytesToRead, out UInt32 numberOfBytesRead,
                                            [In] [MarshalAs(UnmanagedType.Bool)] bool abort,
                                            [In] [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
                                            ref IntPtr context);

      [DllImport("kernel32", SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool BackupSeek(SafeFileHandle hFile,
                                            [In] UInt32 bytesToSeekLow, [In] UInt32 bytesToSeekHigh,
                                            out UInt32 bytesSeekedLow,
                                            out UInt32 bytesSeekedHigh,
                                            [In] ref IntPtr context);

   }
}
