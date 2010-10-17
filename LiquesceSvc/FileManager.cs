using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiquesceSvc
{
    class FileManager
    {
        public static List<string> MirrorDeleteToDo = new List<string>();


        public static void DeleteDirectory(string directory) 
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }



        public static void XMoveDirectory(string pathSource, string pathTarget, bool replaceIfExisting)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(pathSource);
            if (!Directory.Exists(pathTarget))
                Directory.CreateDirectory(pathTarget);
            foreach (FileInfo filein in currentDirectory.GetFiles())
            {
                string fileTarget = pathTarget + Path.DirectorySeparatorChar + filein.Name;
                filein.Delete();
            }
            foreach (DirectoryInfo dr in currentDirectory.GetDirectories())
            {
                XMoveDirectory(dr.FullName, pathTarget + Path.DirectorySeparatorChar + dr.Name, replaceIfExisting);
            }
            Directory.Delete(pathSource);
        }

    }
}
