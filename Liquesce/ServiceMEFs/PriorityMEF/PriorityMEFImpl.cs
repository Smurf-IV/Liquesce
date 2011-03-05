using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using LiquesceFacade;
using LiquesceSvcMEF;
using NLog;

namespace PriorityMEF
{
   /// <summary>
   /// This will the default if nothing has been recognised by the mode variable
   /// </summary>
   public class PriorityMEFImpl : CommonStorage, IServicePlugin
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();



      #region Implementation of ILocations

      /// <summary>
      /// Return possible Physical Location of the new file, does not create it
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>New Physical Location to create the new object</returns>
      public string CreateLocation(string dokanPath)
      {
         Log.Debug("CreateLocation([{0}])", dokanPath);
         string pathToCheck = Path.GetDirectoryName(dokanPath);
         if (String.IsNullOrEmpty(pathToCheck))
            throw new DirectoryNotFoundException("Lookup directory not found");
         ulong num = 0, num2, num3;
         foreach (string t in from t in sourceLocations
                              where !t.Contains(pathToCheck)
                              where GetDiskFreeSpaceEx(t, out num, out num2, out num3)
                              where num > HoldOffBufferBytes
                              select t)
         {
            return (pathToCheck == dokanPath) ? t : Path.Combine(t, Path.GetFileName(dokanPath));
         }

         throw new DirectoryNotFoundException(string.Format("No more individual space usages allowed; holdOffBufferBytes=[{0}]", HoldOffBufferBytes));
      }

      /// <summary>
      /// Return Physical Location of an existing file
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>Physical Location</returns>
      public override string OpenLocation(string dokanPath)
      {
         string foundPath = String.Empty;
         try
         {
            Log.Info("OpenLocation([{0}])", dokanPath);
            //if (dokanPath != PathDirectorySeparatorChar)
            {
               //dokanPath = dokanPath.TrimEnd(Path.DirectorySeparatorChar);
               if (String.IsNullOrWhiteSpace(dokanPath))
                  throw new ArgumentNullException(dokanPath, "Not allowed to pass this length 2");
               if (dokanPath[0] != Path.DirectorySeparatorChar)
                  dokanPath = PathDirectorySeparatorChar + dokanPath;
               using (rootPathsSync.UpgradableReadLock())
               {
                  if (!rootPaths.TryGetValue(dokanPath, out foundPath))
                  {
                     foreach (string newTarget in sourceLocations.Select(sourceLocation => sourceLocation + dokanPath))
                     {
                        Log.Trace("Try and GetPath from [{0}]", newTarget);
                        //Now here's a kicker.. The User might have copied a file directly onto one of the drives while
                        // this has been running, So this ought to try and find if it exists that way.
                        if (File.Exists(newTarget)
                           || Directory.Exists(newTarget))
                        {
                           TrimAndAddUnique(newTarget);
                           rootPaths.TryGetValue(dokanPath, out foundPath);
                           break;
                        }
                     }
                  }
               }
            }
         }
         finally
         {
            Log.Debug("OpenLocation Out(foundPath=[{0}])", foundPath);
         }
         return foundPath;
      }


      #endregion

      #region Implementation of IFileEventHandlers

      /// <summary>
      /// To be used after a file has been updated and closed.
      /// Can be used to create the directory tree as well
      /// </summary>
      /// <param name="actualLocations"></param>
      public void FileClosed(List<string> actualLocations)
      {
      }

      /// <summary>
      /// To be used after a file has been updated and closed.
      /// </summary>
      /// <param name="actualLocation"></param>
      public void FileClosed(string actualLocation)
      {
      }

      /// <summary>
      /// A file has been removed from the system
      /// </summary>
      /// <param name="dokanPath"></param>
      public void FileDeleted(List<string> dokanPath)
      {
      }

      /// <summary>
      /// When a directory is deleted (i.e. is empty), this will be called
      /// </summary>
      /// <param name="actualLocations"></param>
      public void DirectoryDeleted(List<string> actualLocations)
      {
      }

      #endregion


      #region DLL Imports

      [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
      private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
         out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

      #endregion

      #region Code to be shared
      // adds the root path to rootPaths dicionary for a specific file
      private string TrimAndAddUnique(string fullFilePath)
      {
         int index = sourceLocations.FindIndex(fullFilePath.StartsWith);
         if (index >= 0)
         {
            string key = fullFilePath.Remove(0, sourceLocations[index].Length);
            Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
            using (rootPathsSync.WriteLock())
            {
               rootPaths[key] = fullFilePath;
            }
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }

      #endregion

      #region Implementation of IMoveManager

      /// <summary>
      /// Move directories depends on the scatter pattern beig used by the plugin.
      /// Therefore if a priority is implemented, then it could be that some files from a remote part are
      /// being collasced into a single location, but that location may already exist
      /// There are other difficult scenrios that each of the plugins will need to solve.
      /// When they have done, they must inform the other plugin's of their actions.
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="dokanTarget"></param>
      /// <param name="replaceIfExisting"></param>
      /// <param name="actualFileNewLocations"></param>
      /// <param name="actualFileDeleteLocations"></param>
      /// <param name="actualDirectoryDeleteLocations"></param>
      public void MoveDirectory(string dokanPath, string dokanTarget, bool replaceIfExisting, out List<string> actualFileNewLocations, out List<string> actualFileDeleteLocations, out List<string> actualDirectoryDeleteLocations)
      {
         DeleteLocation(dokanPath, true);
         throw new NotImplementedException();
      }

      #endregion
   }
}