using System.ComponentModel.Composition;
using LiquesceSvcMEF;

namespace FolderMEF
{
   [Export(typeof(ICreateFactory))]
   [ExportMetadata("Description", "Folder")]
   public class FolderMEF : ICreateFactory
   {
      #region Implementation of ICreateFactory

      /// <summary>
      /// Creates a new class object that implements IServicePlugin
      /// </summary>
      /// <returns></returns>
      public IServicePlugin Create()
      {
         return new FolderMEFImpl();
      }

      #endregion
   }
}
