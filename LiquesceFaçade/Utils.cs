using System;
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
/*
System.ArgumentException: path is a zero-length string, contains only white space, or contains one or more invalid characters as defined by System.IO.Path.InvalidPathChars. 

System.ArgumentNullException: path is null. 

System.IO.PathTooLongException: The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters. 

System.IO.DirectoryNotFoundException: The specified path is invalid (for example, it is on an unmapped drive). 

System.IO.IOException: An I/O error occurred while opening the file. 

System.UnauthorizedAccessException: This operation is not supported on the current platform.
  -or- 
 path specified a directory.
  -or- 
  The caller does not have the required permission. 

System.IO.FileNotFoundException: The file specified in path was not found. 

System.NotSupportedException: path is in an invalid format. 

System.Security.SecurityException: The caller does not have the required permission. 
*/         if ( ex.InnerException is SocketException)
         {
            return -((SocketException) ex.InnerException).ErrorCode;
         }
         else
         {
            int HrForException = Marshal.GetHRForException(ex);
            return (HiWord(HrForException) == 0x8007) ? -LoWord(HrForException) : Dokan.ERROR_EXCEPTION_IN_SERVICE;
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
