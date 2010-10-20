using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Security.AccessControl;

namespace LiquesceMirrorToDo
{

    public enum ToDo_Type
    {
        FolderCreate,
        FolderDelete,
        FolderRename,
        FileCreate,
        FileDelete,
        FileRename,
        FileRefresh
    }


    [DataContract]
    public class MirrorToDoList
    {
        [DataMember]
        private List<MirrorToDo> list;


        public MirrorToDo CreateFolderCreate(string relative, string original, string mirror)
        {
            MirrorToDo temp = new MirrorToDo(ToDo_Type.FolderCreate);
            temp.CreateFolderCreate(relative, original, mirror);

            list.Add(temp);
            return temp;
        }

        public MirrorToDo CreateFolderDelete(string relative, string original, string mirror)
        {
            MirrorToDo temp = new MirrorToDo(ToDo_Type.FolderDelete);
            temp.CreateFolderDelete(relative, original, mirror);

            list.Add(temp);
            return temp;
        }

        public MirrorToDo CreateFolderRename(string relative1, string relative2, string original1, string original2, string mirror1, string mirror2, bool replace)
        {
            MirrorToDo temp = new MirrorToDo(ToDo_Type.FolderDelete);
            temp.CreateFolderRename(relative1, relative2, original1, original2, mirror1, mirror2, replace);

            list.Add(temp);
            return temp;
        }

        public MirrorToDoList()
        {
            list = new List<MirrorToDo>();
        }

        public MirrorToDoList(MirrorToDoList Copy)
        {
            list = new List<MirrorToDo>(Copy.Get());
        }

        public void Add(MirrorToDo item)
        {
            list.Add(item);
        }

        public void Add(MirrorToDoList newlist)
        {
            for (int i=0; i<newlist.Count(); i++)
            {
                list.Add(newlist.Get(i));
            }
        }

        public void Remove(MirrorToDo item)
        {
            list.Remove(item);
        }

        public int Count()
        {
            return list.Count;
        }

        public List<MirrorToDo> Get()
        {
            return list;
        }

        public MirrorToDo Get(int index)
        {
            return list[index];
        }

        public List<MirrorToDo> Get(ToDo_Type type)
        {
            List<MirrorToDo> filtered = new List<MirrorToDo>();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == type)
                {
                    filtered.Add(list[i]);
                }
            }
            return filtered;
        }

        public void Clear()
        {
            list.Clear();
        }

        public override string ToString()
        {
            string output = "";

            for (int i = 0; i < list.Count; i++)
            {
                output += list[i].ToString() + "\n";
            }

            if (output.Equals(""))
                output = "empty";
            return output;
        }
    }



    [DataContract]
    public class MirrorToDo
    {
        [DataMember]
        public ToDo_Type type;

        // the following strings are paths to the used files
        //  1 for source
        //  2 for destination
        // relative paths starting like "\path\..."
        [DataMember]
        public string Relative1 = null;
        [DataMember]
        public string Relative2 = null;

        // path of the original files on the physical disk
        [DataMember]
        public string Original1 = null;
        [DataMember]
        public string Original2 = null;

        // path of the mirrored file on the physical disk
        [DataMember]
        public string Mirror1 = null;
        [DataMember]
        public string Mirror2 = null;

        [DataMember]
        public bool ReplaceIfExisting = false;


        public MirrorToDo()
        {
        }

        public MirrorToDo(ToDo_Type type)
        {
            this.type = type;
        }

        public MirrorToDo CreateFolderCreate(string relative, string original, string mirror)
        {
            this.type = ToDo_Type.FolderCreate;

            this.Relative1 = relative;
            this.Original1 = original;
            this.Mirror1 = mirror;

            return this;
        }

        public MirrorToDo CreateFolderDelete(string relative, string original, string mirror)
        {
            this.type = ToDo_Type.FolderDelete;

            this.Relative1 = relative;
            this.Original1 = original;
            this.Mirror1 = mirror;

            return this;
        }

        public MirrorToDo CreateFolderRename(string relative1, string relative2, string original1, string original2, string mirror1, string mirror2, bool replace)
        {
            this.type = ToDo_Type.FolderRename;

            this.Relative1 = relative1;
            this.Relative2 = relative2;
            this.Original1 = original1;
            this.Original2 = original2;
            this.Mirror1 = mirror1;
            this.Mirror2 = mirror2;
            this.ReplaceIfExisting = replace;

            return this;
        }


        public override string ToString()
        {
            switch (type)
            {
                case ToDo_Type.FolderCreate:
                    return "FolderCreate:\t" + this.Relative1;
                case ToDo_Type.FolderDelete:
                    return "FolderDelete:\t" + this.Relative1;
                case ToDo_Type.FolderRename:
                    return "FolderRename:\t" + this.Relative1 + " to " + this.Relative2;
                default:
                    return "Unknown ToDo Action";
            }
        }

    }
}
