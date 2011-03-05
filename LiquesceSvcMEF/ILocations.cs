using System.Collections.Generic;
using System.IO;

namespace LiquesceSvcMEF
{
   /// <summary>
   /// Used to get the next / this file location(s), i.e. Create / Open calls
   /// </summary>
   public interface ILocations
   {
      /// <summary>
      /// Return possible Physical Location of the new file, does not create it
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>New Physical Location to create the new object</returns>
      string CreateLocation(string dokanPath);

      /// <summary>
      /// Return Physical Location of an existing file
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>Physical Location</returns>
      string OpenLocation(string dokanPath);

      /// <summary>
      /// Return location(s) of the directories that make up the merged directory object.
      /// Return them in priority order (i.e. the order they are defined in the GUI)
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <returns>Physical Location</returns>
      List<string> OpenDirectoryLocations(string dokanPath);

      /// <summary>
      /// Called when the actual file / directory has been deleted.
      /// This allows the location cache's to be updated
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="isDirectory"></param>
      void DeleteLocation(string dokanPath, bool isDirectory);

      /// <summary>
      /// Populate the base class with the appropriate object
      /// </summary>
      /// <param name="dokanPath">DokanPath passed in</param>
      /// <param name="refreshCache">If Liquesce knows it has the file open then it will send true</param>
      FileSystemInfo GetInfo(string dokanPath, bool refreshCache);

      /// <summary>
      /// Return the found elements from the dokanPath
      /// </summary>
      /// <param name="dokanPath"></param>
      /// <param name="pattern">search pattern</param>
      FileSystemInfo[] FindFiles(string dokanPath, string pattern);
   }
}
