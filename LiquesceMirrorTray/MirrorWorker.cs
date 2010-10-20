using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Messaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using LiquesceMirrorToDo;


namespace LiquesceMirrorTray
{
    class MirrorWorker
    {
        private static Thread thread = new Thread(new ThreadStart(endlesswork));
        private volatile static Semaphore semWorker = new Semaphore(0, 1);

        public volatile static bool refreshToDo = false;


        private static Object lockvarList = "";
        private static MirrorToDoList MirrorToDoList = new MirrorToDoList();

        private static void endlesswork()
        {
            while(true)
            {

                // process deleteFolder
                lock (lockvarList)
                {
                }


                // refresh the ToDo list of the gui
                refreshToDo = true;

                // nothing to do? wait for the mutex
                semWorker.WaitOne();

            }
        }


        private static void processDeleteFolder(string paths)
        {
            string[] pathsarray = paths.Split(MirrorFileManager.SEPARATOR);
            // @@@ find all original folder manipulations and remove them from the list

            // if the original file was deleted
            if (!Directory.Exists(pathsarray[0]))
                MirrorFileManager.DeleteDirectory(pathsarray[1]);
            else
            {
                // @@@ sync folder ?!
            }
        }

   
        public static void addWork(MirrorToDoList newWork)
        {
            lock (lockvarList)
                MirrorToDoList.Add(newWork);

            if (newWork.Count() != 0)
                semWorker.Release();
        }

        public static void getToDo(ListBox.ObjectCollection collection)
        {
            collection.Clear();

            lock (lockvarList)
            {
                for (int i = 0; i < MirrorToDoList.Count(); i++)
                    collection.Add(MirrorToDoList.Get(i).ToString());
            }
        }

        public static void Start()
        {
            thread.Start();
        }

        public static void Stop()
        {
            thread.Abort();
        }

        public static void Pause()
        {
            
        }

        public static void Continue()
        {
            
        }

        private static string removePrefix(string prefix, string fullstring)
        {
            return fullstring.Replace(prefix, "");
        }

    }
}
