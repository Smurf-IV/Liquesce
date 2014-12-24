#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Win32Structs.cs" company="Smurf-IV">
// 
//  Copyright (C) 2013 Smurf-IV
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Runtime.InteropServices;

namespace LiquesceSvc
{

   ////
   /// <summary>
   /// Structure used for Windows API calls related to file information.
   /// </summary>
   /// <remarks>
   /// Workaround from http://www.pinvoke.net/default.aspx/Structures/WIN32_FIND_DATA.html
   /// </remarks>
   // ReSharper disable FieldCanBeMadeReadOnly.Global
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct WIN32_FIND_FILETIME
   {
      public UInt32 dwLowDateTime;
      public UInt32 dwHighDateTime;
   }
   // ReSharper restore FieldCanBeMadeReadOnly.Global

   /// <summary>
   /// http://msdn.microsoft.com/en-us/library/aa363788%28VS.85%29.aspx
   /// </summary>
   // ReSharper disable FieldCanBeMadeReadOnly.Global
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct BY_HANDLE_FILE_INFORMATION
   {
      public uint dwFileAttributes;
      public WIN32_FIND_FILETIME ftCreationTime;
      public WIN32_FIND_FILETIME ftLastAccessTime;
      public WIN32_FIND_FILETIME ftLastWriteTime;
      public uint dwVolumeSerialNumber;
      public uint nFileSizeHigh;
      public uint nFileSizeLow;
      public uint dwNumberOfLinks;
      public uint nFileIndexHigh;
      public uint nFileIndexLow;
   }
   // ReSharper restore FieldCanBeMadeReadOnly.Global

   // ReSharper disable FieldCanBeMadeReadOnly.Global
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
   public struct WIN32_FIND_DATA
   {
      public uint dwFileAttributes;
      public WIN32_FIND_FILETIME ftCreationTime;
      public WIN32_FIND_FILETIME ftLastAccessTime;
      public WIN32_FIND_FILETIME ftLastWriteTime;
      public uint nFileSizeHigh;
      public uint nFileSizeLow;
      private readonly uint dwReserved0;
      private readonly uint dwReserved1;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
      public string cFileName;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public readonly string cAlternateFileName;
   }
   // ReSharper restore FieldCanBeMadeReadOnly.Global

   // ReSharper disable UnusedMember.Global
   // http://msdn.microsoft.com/en-us/library/windows/desktop/aa362667(v=vs.85).aspx
   public enum FileStreamType
   {
      Data = 1,
      ExternalData = 2,
      SecurityData = 3,
      AlternateData = 4,
      Link = 5,
      PropertyData = 6,
      ObjectID = 7,
      ReparseData = 8,
      SparseDock = 9,
      BACKUP_TXFS_DATA  // Transactional NTFS (TxF) data stream. This corresponds to the NTFS $TXF_DATA stream type.
   }
   [Flags]
   public enum FileStreamAttributes
   {
      None = 0,
      ModifiedWhenRead = 1,
      ContainsSecurity = 2,
      ContainsProperties = 4,
      Sparse = 8,
   }
   // ReSharper restore UnusedMember.Global

   // ReSharper disable FieldCanBeMadeReadOnly.Global
   [StructLayout(LayoutKind.Sequential, Pack = 4)]
   public struct WIN32_STREAM_ID
   {
      public UInt32 dwStreamType;
      public UInt32 dwStreamAttributes;
      public long Size;
      public UInt32 dwStreamNameSize;
      // WCHAR cStreamName[1]; 
   }
   // ReSharper restore FieldCanBeMadeReadOnly.Global


}