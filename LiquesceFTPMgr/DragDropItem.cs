using System;

namespace Liquesce
{

   [Serializable]
   internal class DragDropItem
   {
      public enum SourceType
      {
         Drive,
         Merge
      }
      public DragDropItem(string name, SourceType source)
      {
         Name = name;
         Source = source;
      }

      public string Name { get; private set; }
      public SourceType Source { get; private set; }
   }

}
