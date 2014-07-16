using System;
using System.Collections.Generic;
using System.Linq;
using LiquesceFacade;

namespace LiquesceSvc
{
   internal class CachedRootPathsSystemInfo : CacheHelper<string, NativeFileOps> 
   {
      public CachedRootPathsSystemInfo(uint cacheLifetimeSeconds)
         : base(cacheLifetimeSeconds, true, StringComparer.OrdinalIgnoreCase)
      {
         // StringComparer.OrdinalIgnoreCase because windows is WriteSensitive
         // BUT search ignore
      }

      public void RemoveAllTargetDirsFromLookup(string removeDirSource)
      {
         using (cacheLock.WriteLock())
         {
            List<string> toBeRemoved = Cache.Keys.Where(key => key.StartsWith(removeDirSource)).ToList();
            foreach (string s in toBeRemoved)
            {
               Cache.Remove(s);
            }
         }
      }
   }
}
