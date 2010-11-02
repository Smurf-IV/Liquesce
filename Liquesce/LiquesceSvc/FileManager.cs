using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LiquesceFacade;
using NLog;

namespace LiquesceSvc
{
    class FileManager
    {
        private static Object lockvarMirrorToDo = "";
        //private static List<string> MirrorToDo = new List<string>();
        private static MirrorToDoList MirrorToDo = new MirrorToDoList();

        static private readonly Logger Log = LogManager.GetCurrentClassLogger();

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


        // returns all elements of the to do list and removes all elements
        public static MirrorToDoList ConsumeMirrorToDo()
        {
            lock (lockvarMirrorToDo)
            {
                MirrorToDoList temp = new MirrorToDoList();
                temp.Add(MirrorToDo);
                MirrorToDo.Clear();
                return temp;
            }
        }


        public static void AddMirrorToDo(MirrorToDo entry)
        {
            lock (lockvarMirrorToDo)
            {
                MirrorToDo.Add(entry);
            }
        }


        public static string GetLocationFromFilePath(string path)
        {
            return path.Substring(0, path.LastIndexOf("\\"));
        }


    }
}
