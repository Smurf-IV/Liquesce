using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiquesceSvcMEF
{
   public interface IMoveManager
   {
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
      void MoveDirectory(string dokanPath, string dokanTarget, bool replaceIfExisting, out List<string> actualFileNewLocations, out List<string> actualFileDeleteLocations, out List<string> actualDirectoryDeleteLocations);
   }
}
