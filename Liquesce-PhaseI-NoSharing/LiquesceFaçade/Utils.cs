#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="Utils.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DokanNet;

namespace LiquesceFacade
{
   public static class Utils
   {
      public static int HiWord(int number)
      {
         return ((number & 0x80000000) == 0x80000000) ? number >> 16 : (number >> 16) & 0xffff;
      }

      public static int LoWord(int number)
      {
         return number & 0xffff;
      }

      /// <summary>
      /// #define ERROR_DISK_OPERATION_FAILED 1127L //  While accessing the hard disk, a disk operation failed even after retries.
      /// The above might be a better error code ??
      /// </summary>
      /// <param name="ex">The list of exception types can grow in here</param>
      /// <returns>!! Must be negative !!</returns>
      public static int BestAttemptToWin32(Exception ex)
      {
         if (ex is System.ComponentModel.Win32Exception)
         {
            return (ex as System.ComponentModel.Win32Exception).NativeErrorCode * -1;
         }
         else if ( ex.InnerException is SocketException)
         {
            return -((SocketException) ex.InnerException).ErrorCode;
         }
         else
         {
            int HrForException = Marshal.GetHRForException(ex);
            // Check http://msdn.microsoft.com/en-us/library/ms819772.aspx (WinError.h) for error codes
            return (HiWord(HrForException) == -32761/*0x8007*/) ? -LoWord(HrForException) : Dokan.ERROR_EXCEPTION_IN_SERVICE;
         }
      }

      public static void ResizeDescriptionArea(ref PropertyGrid grid, int nNumLines)
      {
         try
         {
            System.Reflection.PropertyInfo pi = grid.GetType().GetProperty("Controls");
            Control.ControlCollection cc = (Control.ControlCollection)pi.GetValue(grid, null);

            foreach (Control c in cc)
            {
               Type ct = c.GetType();
               string sName = ct.Name;

               if (sName == "DocComment")
               {
                  pi = ct.GetProperty("Lines");
                  if (pi != null)
                  {
#pragma warning disable 168
                     int i = (int)pi.GetValue(c, null);
#pragma warning restore 168
                     pi.SetValue(c, nNumLines, null);
                  }

                  if (ct.BaseType != null)
                  {
                     System.Reflection.FieldInfo fi = ct.BaseType.GetField("userSized",
                                                                           System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                     if (fi != null)
                        fi.SetValue(c, true);
                  }
                  break;
               }
            }
         }
         catch
         {
         }
      }

   }
}
