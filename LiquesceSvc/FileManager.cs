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

        private const char DOS_STAR = '<';
        private const char DOS_QM = '>';
        private const char DOS_DOT = '"';


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



        // check whether Name matches Expression
        // Expression can contain "?"(any one character) and "*" (any string)
        // when IgnoreCase is TRUE, do case insenstive matching
        public bool IsNameInExpression(string Expression, string Name, bool IgnoreCase)
        {
            int ei = 0;
            int ni = 0;

            while (ei < Expression.Length)
            {

                if (Expression[ei] == '*')
                {
                    ei++;
                    if (ei >= Expression.Length)
                        return true;

                    while (ni < Name.Length)
                    {
                        if (IsNameInExpression(Expression.Substring(ei), Name.Substring(ni), IgnoreCase))
                            return true;
                        ni++;
                    }

                }
                else if (Expression[ei] == DOS_STAR)
                {

                    int p = ni + 1;
                    int lastDot = 0;
                    ei++;

                    while (p < Name.Length)
                    {
                        if (Name[p] == '.')
                            lastDot = p;
                        p++;
                    }


                    while (true)
                    {
                        if (ni >= Name.Length || ni == lastDot)
                            break;

                        if (IsNameInExpression(Expression.Substring(ei), Name.Substring(ni), IgnoreCase))
                            return true;
                        ni++;
                    }

                }
                else if (Expression[ei] == DOS_QM)
                {

                    ei++;
                    if (Name[ni] != '.')
                    {
                        ni++;
                    }
                    else
                    {

                        int p = ni + 1;
                        while (p < Name.Length)
                        {
                            if (Name[p] == '.')
                                break;
                            p++;
                        }

                        if (Name[p] == '.')
                            ni++;
                    }

                }
                else if (Expression[ei] == DOS_DOT)
                {
                    ei++;

                    if (Name[ni] == '.')
                        ni++;

                }
                else
                {
                    if (Expression[ei] == '?')
                    {
                        ei++; ni++;
                    }
                    else if (IgnoreCase && CompareChars(Expression[ei], Name[ni], true))
                    {
                        ei++; ni++;
                    }
                    else if (!IgnoreCase && Expression[ei] == Name[ni])
                    {
                        ei++; ni++;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            if (ei == Expression.Length && ni == Name.Length)
                return true;

            return false;
        }


        private bool CompareChars(char char1, char char2, bool caseinsensitive)
        {
            if (caseinsensitive == false)
                return (char1 == char2);
            else
            {
                return char1.ToString().Equals(char2.ToString(), StringComparison.CurrentCultureIgnoreCase);
            }
        }

    }
}
