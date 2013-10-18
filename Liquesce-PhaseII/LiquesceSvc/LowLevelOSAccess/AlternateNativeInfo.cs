using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
			if ( (0 >= length) 
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
}
