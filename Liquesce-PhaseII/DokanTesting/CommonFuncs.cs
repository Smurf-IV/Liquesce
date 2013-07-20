using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;

namespace DokanTesting
{
   static public class CommonFuncs
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public static bool CheckExistenceOfMount(string mountPoint)
      {
         if (mountPoint.Length == 1)
         {
            string path = string.Format("{0}:{1}", mountPoint, Path.DirectorySeparatorChar);
            if (!Directory.Exists(path))
            {
               return false;
            }
            // 2nd phase as the above is supposed to be cheap but can return false +ves
            {
               string[] drives = Environment.GetLogicalDrives();
               return (Array.Exists(drives, dr => dr.Remove(1) == mountPoint));
            }
         }
         else
         {
            DirectoryInfo di = new DirectoryInfo(mountPoint);
            return ((di.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
         }
      }
   }
}
