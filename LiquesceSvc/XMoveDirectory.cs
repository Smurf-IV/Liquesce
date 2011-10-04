using System.IO;

namespace LiquesceSvc
{
   static internal class XMoveDirectory
   {
      public static void Move(Roots roots, string source, string target, bool replaceIfExisting)
      {
         Move( roots, new DirectoryInfo(source), new DirectoryInfo(target), replaceIfExisting );
      }

      public static void Move(Roots roots, DirectoryInfo pathSource, DirectoryInfo pathTarget, bool replaceIfExisting)
      {
         if (!pathTarget.Exists)
            pathTarget.Create();

         // for every file in the current folder
         foreach (FileInfo filein in pathSource.GetFiles())
         {
            string fileSource = Path.Combine( pathSource.FullName, filein.Name);
            string fileTarget = Path.Combine( pathTarget.FullName, filein.Name);

            // test whole liquesce drive and not only one physical drive
            bool fileIsInTarget = Roots.RelativeFileExists(Roots.GetRelative(fileTarget));

            // if replace activated or file is not availabel on target
            if (replaceIfExisting || !fileIsInTarget)
            {
               File.Move(fileSource, fileTarget);
            }
         }

         // for every subfolder recurse
         foreach (DirectoryInfo dr in pathSource.GetDirectories())
         {
            Move(roots, dr, new DirectoryInfo( Path.Combine(pathTarget.FullName, dr.Name)), replaceIfExisting);
            // While we are here, remove 
            roots.RemoveTargetFromLookup(dr.FullName);
         }

         // after files are moved, check if there are new files or new subfolders
         // if so then don't remove the source directory
         if (pathSource.GetDirectories().Length == 0 && pathSource.GetFiles().Length == 0)
         {
            pathSource.Delete();
            roots.RemoveTargetFromLookup(pathSource.FullName);
         }
      }
   }
}