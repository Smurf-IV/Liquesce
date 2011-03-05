using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;

namespace LiquesceSvcMEF
{
   public abstract class CommonStorage : IManagement
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      protected string root;
      protected UInt64 HoldOffBufferBytes
      {
         get;
         private set;
      }
      protected List<string> sourceLocations;
      protected List<string> knownSharePaths;

      static protected readonly string PathDirectorySeparatorChar = Path.DirectorySeparatorChar.ToString();

      protected readonly Dictionary<string, List<string>> foundDirectories = new Dictionary<string, List<string>>();
      protected readonly ReaderWriterLockSlim foundDirectoriesSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

      protected readonly ReaderWriterLockSlim rootPathsSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      protected readonly Dictionary<string, string> rootPaths = new Dictionary<string, string>();

      protected readonly ReaderWriterLockSlim fileInfoSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
      protected readonly Dictionary<string, FileSystemInfo> fileInfoCache = new Dictionary<string, FileSystemInfo>();

      #region Implementation of IManagement

      /// <summary>
      /// Used to force this Extension to read it's config information
      /// </summary>
      /// <param name="mountPoint">Ofset into the Config information as there may be more than one mount</param>
      /// <param name="holdOffBufferBytes">When creating new objects, this is used to move to the next calculated location</param>
      virtual public void Initialise(string mountPoint, UInt64 holdOffBufferBytes)
      {
         Log.Debug("Initialise([{0}])", mountPoint);
         root = mountPoint;
         if (mountPoint.Length == 1)
            root += ":" + PathDirectorySeparatorChar;
         HoldOffBufferBytes = holdOffBufferBytes;
      }

      /// <summary>
      /// Called to get this thing going
      /// </summary>
      virtual public void Start()
      {
         Log.Debug("Start()");
      }

      /// <summary>
      /// Called to exit the functions / threads, so be nice because this is normally 
      /// the service closing and will have limited time before the rug is pulled
      /// </summary>
      virtual public void Stop()
      {
         Log.Debug("Start()");
      }

      /// <summary>
      /// Details to be passed in of the base mountPoint information
      /// </summary>
      public List<string> SourceLocations
      {
         set { sourceLocations = value; }
      }

      /// <summary>
      /// Details to be passed in as the shares are "discovered" for the mountPoint
      /// </summary>
      public List<string> KnownSharePaths
      {
         set { knownSharePaths = value; }
      }

      #endregion

#region ILocations helpers
      public FileSystemInfo GetInfo(string dokanPath, bool refreshCache)
      {
         FileSystemInfo fsi = null;
         string path = OpenLocation(dokanPath);
         if (!String.IsNullOrEmpty(path))
         {
            if (!refreshCache)
            {
               using (fileInfoSync.ReadLock())
               {
                  if (fileInfoCache.TryGetValue(path, out fsi))
                     return fsi;
               }
            }
            // USe internal objects to prevent getting the information twice (See internal usage of FileInfo.Exists(path))
            fsi = new FileInfo(path);
            if (!fsi.Exists)
            {
               fsi = new DirectoryInfo(path);
               if (!fsi.Exists)
                  fsi = null;
            }
            if (fsi != null)
               using (fileInfoSync.WriteLock())
               {
                  fileInfoCache[path] = fsi;
               }
         }
         return fsi;
      }

      public abstract string OpenLocation(string dokanPath);

      /// <summary>
      /// Return Physical Location of an existing file
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>Physical Location</returns>
      public List<string> OpenDirectoryLocations(string dokanPath)
      {
         List<string> found = null;
         using (foundDirectoriesSync.UpgradableReadLock())
         {
            if (String.IsNullOrWhiteSpace(dokanPath))
               throw new ArgumentNullException(dokanPath, "Not allowed to pass this length 2");
            if (dokanPath[0] != Path.DirectorySeparatorChar)
               dokanPath = PathDirectorySeparatorChar + dokanPath;
            if (!foundDirectories.TryGetValue(dokanPath, out found))
            {
               found = sourceLocations.Select(sourceLocation => sourceLocation + dokanPath).Where(Directory.Exists).ToList();
               if (found.Count > 0)
               {
                  if ((GetInfo(dokanPath, false).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                  {
                     found.RemoveRange(1, found.Count - 1);
                  }
                  using (foundDirectoriesSync.WriteLock())
                  {
                     foundDirectories[dokanPath] = found;
                  }
               }
            }
         }
         return found;
      }

      /// <summary>
      /// Called when the actual file / directory has been deleted.
      /// This allows the location cache's to be updated
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="isDirectory"></param>
      public void DeleteLocation(string dokanPath, bool isDirectory)
      {
         if (isDirectory)
         {
            using (foundDirectoriesSync.WriteLock())
            {
               foundDirectories.Remove(dokanPath);
            }
         }
         else
         {
            // TODO: Do the File cache removal
         }
      }

      /// <summary>
      /// Return the found elements from the dokanPath
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="pattern">search pattern</param>
      public FileSystemInfo[] FindFiles(string dokanPath, string pattern)
      {
         string path = OpenLocation(dokanPath);
         Dictionary<string, FileSystemInfo> uniqueFiles = new Dictionary<string, FileSystemInfo>();
         if (!Directory.Exists(path))
         {
            // TODO: Check if this is ever called
            AddFiles(path, uniqueFiles, pattern);
         }
         else
         {
            List<string> currentMatchingDirs = OpenDirectoryLocations(dokanPath);
            // Do this in reverse, so that the preferred references overwrite the older files
            for (int i = currentMatchingDirs.Count - 1; i >= 0; i--)
            {
               AddFiles(currentMatchingDirs[i], uniqueFiles, pattern);
            }
         }
         // TODO: Should this return "." and ".." info as well ?
         FileSystemInfo[] foundFiles = new FileSystemInfo[uniqueFiles.Count];
         int index = 0;
         using (fileInfoSync.WriteLock())
         {
            // Update the cache and perform the return build up
            foreach (FileSystemInfo info in uniqueFiles.Select(kvp => kvp.Value))
            {
               fileInfoCache[info.FullName] = info;
               foundFiles[index++] = info;
            }
         }
         return foundFiles;
      }

#endregion

      private string TrimToMount(string fullFilePath)
      {
         int index = sourceLocations.FindIndex(fullFilePath.StartsWith);
         return index >= 0 ? fullFilePath.Remove(0, sourceLocations[index].Length) : String.Empty;
      }

      // adds the root path to rootPaths dicionary for a specific file
      protected string TrimAndAddUnique(string fullFilePath)
      {
         string key = TrimToMount(fullFilePath);
         if (!String.IsNullOrEmpty(key))
         {
            Log.Trace("Adding [{0}] to [{1}]", key, fullFilePath);
            using (rootPathsSync.WriteLock())
            {
               rootPaths[key] = fullFilePath;
            }
            return key;
         }
         throw new ArgumentException("Unable to find BelongTo Path: " + fullFilePath, fullFilePath);
      }

      private void AddFiles(string path, Dictionary<string, FileSystemInfo> files, string pattern)
      {
         Log.Trace("AddFiles IN path[{0}] pattern[{1}]", path, pattern);
         try
         {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
               FileSystemInfo[] fileSystemInfos = dirInfo.GetFileSystemInfos(pattern, SearchOption.TopDirectoryOnly);
               //                  bool isDirectoy = (info2.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
               foreach (FileSystemInfo info2 in fileSystemInfos)
               {
                  // Prevent the system from timing out due to slow access through the driver == FileAttributes.Offline
                  if (Log.IsTraceEnabled
                     && ((info2.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)   // Not even XP allows this!
                     )
                  {
                     info2.Attributes |= FileAttributes.Offline;
                  }
                  files[TrimAndAddUnique(info2.FullName)] = info2;
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("AddFiles threw: ", ex);
         }
      }
   }
}
