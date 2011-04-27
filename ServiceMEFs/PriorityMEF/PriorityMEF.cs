using System.ComponentModel.Composition;
using LiquesceSvcMEF;

namespace PriorityMEF
{
   [Export(typeof(ICreateFactory))]
   [ExportMetadata("Description", "Priority")]
   public class PriorityMEF : ICreateFactory
   {
      #region Implementation of ICreateFactory

      /// <summary>
      /// Creates a new class object that implements IServicePlugin
      /// </summary>
      /// <returns></returns>
      public IServicePlugin Create()
      {
         return new PriorityMEFImpl();
      }

      #endregion
   }
}
