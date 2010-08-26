using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Liquesce
{
   [Serializable]
   internal class DragDropItem
   {
      public DragDropItem()
      {
      }

      public DragDropItem(string name)
      {
         Name = name;
      }

      public string Name { get; private set; }
   }

}
