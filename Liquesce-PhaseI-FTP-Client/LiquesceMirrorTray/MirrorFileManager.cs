using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiquesceMirrorTray
{
    class MirrorFileManager
    {
        public const string TO_DO_PRE__DELETE_DIR = "DeleteDir|";
        public const string TO_DO_PRE__DELETE_FILE = "DeleteFile|";
        public const string TO_DO_PRE__COPY_DIR = "CopyDir|";
        public const string TO_DO_PRE__COPY_FILE = "CopyFile|";

        public const char SEPARATOR = '|';


        public static void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }


    }
}
