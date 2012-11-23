#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ExtractIcon.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2012 Simon Coghlan (Aka Smurf-IV)
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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace Liquesce
{
   internal class ExtractIcon
   {
      /// <summary>
      /// Get the icon used for folders.
      /// </summary>
      /// <param name="small">Whether to get the small icon instead of the large one</param>
      public static Icon GetFolderIcon( bool small )
      {
         return GetIconForFilename( Environment.GetFolderPath( Environment.SpecialFolder.System ), small );
      }

      /// <summary>
      /// Get the icon used for files that do not have their own icon
      /// </summary>
      /// <param name="small">Whether to get the small icon instead of the large one</param>
      public static Icon GetFileIcon( bool small )
      {
         return GetExtensionIcon( "", small );
      }

      /// <summary>
      /// Get the icon used by files of a given extension.
      /// </summary>
      /// <param name="extension">The extension without leading dot</param>
      /// <param name="small">Whether to get the small icon instead of the large one</param>
      public static Icon GetExtensionIcon( string extension, bool small )
      {
         string tmp = Path.GetTempFileName();
         File.Delete( tmp );
         string fn = tmp + "." + extension;
         try
         {
            File.Create( fn ).Close();
            return GetIconForFilename( fn, small );
         }
         finally
         {
            File.Delete( fn );
         }
      }

      /// <summary>
      /// Get the icon used for a given, existing file.
      /// </summary>
      /// <param name="fileName">Name of the file</param>
      /// <param name="small">Whether to get the small icon instead of the large one</param>
      public static Icon GetIconForFilename( string fileName, bool small )
      {
         SHFILEINFO shinfo = new SHFILEINFO();

         if (small)
         {
            SHGetFileInfo(fileName, 0, ref shinfo,
                          (uint) Marshal.SizeOf(shinfo),
                          SHGFI_ICON |
                          SHGFI_SMALLICON);
         }
         else
         {
            SHGetFileInfo(fileName, 0,
                          ref shinfo, (uint) Marshal.SizeOf(shinfo),
                          SHGFI_ICON | SHGFI_LARGEICON);
         }

         if (shinfo.hIcon == IntPtr.Zero)
            return null;

         return Icon.FromHandle(shinfo.hIcon);
      }

 
      #region PInvoke Declarations

      // ReSharper disable InconsistentNaming
      // ReSharper disable InconsistentNaming
      private const uint SHGFI_ICON = 0x100;
      private const uint SHGFI_LARGEICON = 0x0; // 'Large icon
      private const uint SHGFI_SMALLICON = 0x1; // 'Small icon

      [DllImport( "shell32.dll" )]
      private static extern IntPtr SHGetFileInfo( string pszPath,
      uint dwFileAttributes,
      ref SHFILEINFO psfi,
      uint cbSizeFileInfo,
      uint uFlags );

      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      private struct SHFILEINFO
      {
         public IntPtr hIcon;
         public IntPtr iIcon;
         public uint dwAttributes;
         [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 260 )]
         public string szDisplayName;
         [MarshalAs( UnmanagedType.ByValTStr, SizeConst = 80 )]
         public string szTypeName;
      };
      // ReSharper restore InconsistentNaming
      // ReSharper restore InconsistentNaming
      #endregion
   }
}
