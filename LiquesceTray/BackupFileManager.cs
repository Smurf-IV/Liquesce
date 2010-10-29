using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace LiquesceTray
{
    class BackupFileManager
    {
        public const string HIDDEN_BACKUP_FOLDER = ".backup";


        public static Backup backupform;


        public static List<string> missingFi = new List<string>();
        public static List<string> inconsistentFi = new List<string>();
        public static List<string> missingFo = new List<string>();
        public static List<string> inconsistentFo = new List<string>();

        public static bool cancel = false;

        public static string searchpath;





        public static void Init(Backup backupformin)
        {
            backupform = backupformin;
        }

        public static void Clear()
        {
            missingFi.Clear();
            inconsistentFi.Clear();
            missingFo.Clear();
            inconsistentFo.Clear();

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.listMissing.Items.Clear();
                backupform.listInconsistent.Items.Clear();
                backupform.textCurrent.Text = "";
            }));

            cancel = false;
        }



        public static void FindFoldersAndFiles()
        {
            FindFoldersAndFiles(searchpath);

            cancel = false;

            backupform.Invoke(new MethodInvoker(delegate {
                backupform.ResetControls();
                backupform.textCurrent.Text = "[COMPLETED CONSISTENCY CHECK]";
            }));
        }


        private static void FindFoldersAndFiles(string path)
        {
            if (cancel == false)
            {

                backupform.textCurrent.Invoke(new MethodInvoker(delegate { 
                    backupform.textCurrent.Text = path; 
                }));


                ////////////////////////////////////
                // find files of current directory
                string[] tempfiles = Directory.GetFiles(path);
                for (int i = 0; i < tempfiles.Length && cancel == false; i++)
                {
                    if (!TestBigBrother(tempfiles[i]))
                    {
                        AddMissingFi(tempfiles[i]);
                    }

                    string physBackup = GetPhysicalPath(tempfiles[i]);
                    string physOriginal = GetPhysicalPath(FilterFolder(tempfiles[i], HIDDEN_BACKUP_FOLDER));
                    if (physOriginal.Equals(FilterFolder(physBackup, HIDDEN_BACKUP_FOLDER)))
                    {
                        AddInconsistentFi(physBackup, physOriginal);
                    }
                }

                ///////////////////////////////////////////
                // find directories of current directory
                string[] tempdirs = Directory.GetDirectories(path);
                for (int i = 0; i < tempdirs.Length && cancel == false; i++)
                {
                    if (!TestBigBrother(tempdirs[i]))
                    {
                        AddMissingFo(tempdirs[i]);
                    }

                    // if empty dir
                    if ((Directory.GetDirectories(tempdirs[i]).Length == 0) &&
                        (Directory.GetFiles(tempdirs[i]).Length == 0)
                        )
                    {
                        string physBackup = GetPhysicalPath(tempdirs[i]);
                        string physOriginal = GetPhysicalPath(FilterFolder(tempdirs[i], HIDDEN_BACKUP_FOLDER));
                        if (physOriginal.Equals(FilterFolder(physBackup, HIDDEN_BACKUP_FOLDER)))
                        {
                            AddInconsistentFo(physBackup, physOriginal);
                        }
                    }
                    else
                    {
                        // recurse new folders
                        FindFoldersAndFiles(tempdirs[i]);
                    }
                } // dir for end
            } // cancel if end
        }



        private static bool TestBigBrother(string path)
        {
            string pathtotest = FilterFolder(path, HIDDEN_BACKUP_FOLDER);

            if (File.Exists(pathtotest) || Directory.Exists(pathtotest))
                return true;
            else
                return false;
        }



        private static void AddMissingFi(string path)
        {
            missingFi.Add(GetPhysicalPath(path));

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.listMissing.Items.Add("File: \"" + FilterFolder(path, HIDDEN_BACKUP_FOLDER) + "\"");
            }));
        }

        private static void AddMissingFo(string path)
        {
            missingFo.Add(GetPhysicalPath(path));

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.listMissing.Items.Add("Folder: \"" + FilterFolder(path, HIDDEN_BACKUP_FOLDER) + "\"");
            }));
        }

        private static void AddInconsistentFi(string backup, string original)
        {
            inconsistentFi.Add(backup);

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.listInconsistent.Items.Add("\"" + backup + "\" is on same disk as \"" + original + "\"");
            }));
        }

        private static void AddInconsistentFo(string backup, string original)
        {
            inconsistentFo.Add(backup);

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.listInconsistent.Items.Add("\"" + backup + "\" is on same disk as \"" + original + "\"");
            }));
        }

        private static string FilterFolder(string path, string folder)
        {
            return path.Replace("\\" + folder, "");
        }

        private static string GetPhysicalPath(string path)
        {
            string relative = path.Replace(backupform.config.DriveLetter + ":", "");

            for (int i = 0; i < backupform.config.SourceLocations.Count; i++)
            {
                string location = backupform.config.SourceLocations[i];
                string testpath = location + relative;
                if (File.Exists(testpath) || Directory.Exists(testpath))
                    return testpath;
            }

            return "";
        }


        public static void RemoveMissing()
        {
            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.progress.Maximum = missingFi.Count + missingFo.Count - 1;
            }));

            for (int i = 0; i < missingFi.Count && cancel == false; i++)
            {
                backupform.Invoke(new MethodInvoker(delegate
                {
                    backupform.progress.Value = i;
                    backupform.textCurrent.Text = missingFi[i];
                }));

                try
                {
                    File.Delete(missingFi[i]);
                }
                catch { }
            }

            for (int i = 0; i < missingFo.Count && cancel == false; i++)
            {
                backupform.Invoke(new MethodInvoker(delegate
                {
                    backupform.progress.Value = i + missingFi.Count;
                    backupform.textCurrent.Text = missingFo[i];
                }));

                try
                {
                    Directory.Delete(missingFo[i], true);
                }
                catch { }
            }

            cancel = false;

            missingFi.Clear();
            missingFo.Clear();

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.textCurrent.Text = "[COMPLETED REMOVE OF MISSING FILES/FOLDERS]";

                backupform.listMissing.Items.Clear();

                backupform.ResetControls();
            }));
        }



        public static void RemoveInconsistent()
        {
            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.progress.Maximum = inconsistentFi.Count + inconsistentFo.Count - 1;
            }));

            for (int i = 0; i < inconsistentFi.Count && cancel == false; i++)
            {
                backupform.Invoke(new MethodInvoker(delegate
                {
                    backupform.progress.Value = i;
                    backupform.textCurrent.Text = inconsistentFi[i];
                }));

                try
                {
                    File.Delete(inconsistentFi[i]);
                }
                catch { }
            }

            for (int i = 0; i < inconsistentFo.Count && cancel == false; i++)
            {
                backupform.Invoke(new MethodInvoker(delegate
                {
                    backupform.progress.Value = i + inconsistentFo.Count;
                    backupform.textCurrent.Text = inconsistentFo[i];
                }));

                try
                {
                    Directory.Delete(inconsistentFo[i], true);
                }
                catch { }
            }

            cancel = false;

            inconsistentFi.Clear();
            inconsistentFo.Clear();

            backupform.Invoke(new MethodInvoker(delegate
            {
                backupform.textCurrent.Text = "[COMPLETED REMOVE OF INCONSISTENT FILES/FOLDERS]";

                backupform.listMissing.Items.Clear();

                backupform.ResetControls();
            }));
        }
    }
}
