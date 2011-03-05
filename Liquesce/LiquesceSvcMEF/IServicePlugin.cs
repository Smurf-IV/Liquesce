
namespace LiquesceSvcMEF
{
   public interface IDescription
   {
      /// <summary>
      /// Returns the name of the functionality provided, i.e. Priority
      /// will normally be the static string used in the menu items / logging
      /// </summary>
      string Description { get; }
   }

   public interface ICreateFactory
   {
      /// <summary>
      /// Creates a new class object that implements IServicePlugin
      /// </summary>
      /// <returns></returns>
      IServicePlugin Create();
   }

   // +=+= The Service will then import this way
   // +=+= See http://msdn.microsoft.com/en-us/library/dd460648.aspx#further_imports_and_importmany
   //[ImportMany]
   //IEnumerable<Lazy<IDescription, ICreateFactory>> operations;

   // +=+= The MEF Dll will then implement this for export and object creation when Add class object are required
   //[Export(typeof(ICreateFactory))]
   //[ExportMetadata("Description", "Add")]
   //class Add: ICreateFactory
   //{
   //    IServicePlugin Create()
   //    {
   //        return new Add();
   //    }
   //#region Implement Interfaces
   //#endregion
   //}

   /// <summary>
   /// The Main Plugin Interface that will be Implemented and then detected from the MEF DLL's
   /// and used in the Liquesce Service
   /// </summary>
   public interface IServicePlugin : IManagement, ILocations, IMoveManager, IFileEventHandlers
   {
   }
}
