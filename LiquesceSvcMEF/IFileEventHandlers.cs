using System.Collections.Generic;
using System.IO;

namespace LiquesceSvcMEF
{
   /// <summary>
   /// Will respond to deletes, and moves that have already occurred
   /// The order of firing (i.e after a directory move) will be:
   /// FileDeleted
   /// DirectoryDeleted
   /// FileClosed
   /// </summary>
   public interface IFileEventHandlers
   {
      /// <summary>
      /// To be used after a file has possibly been updated and closed.
      /// </summary>
      /// <param name="actualLocations"></param>
      void FileClosed(List<string> actualLocations);

      /// <summary>
      /// To be used after a file has possibly been updated and closed.
      /// </summary>
      /// <param name="actualLocation"></param>
      void FileClosed(string actualLocation);

      /// <summary>
      /// A file has been removed from the system
      /// </summary>
      /// <param name="actualLocations"></param>
      void FileDeleted(List<string> actualLocations);

      /// <summary>
      /// When a directory is deleted (i.e. is empty), this will be called
      /// </summary>
      /// <param name="actualLocations"></param>
      void DirectoryDeleted(List<string> actualLocations);

   }
}
