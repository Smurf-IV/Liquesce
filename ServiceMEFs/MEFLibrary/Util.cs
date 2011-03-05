using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MEFLibrary
{
   public class Util
   {
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
   }
}
