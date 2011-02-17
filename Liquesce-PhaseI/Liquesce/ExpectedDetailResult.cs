using System;

namespace Liquesce
{
   class ExpectedDetailResult : IComparable
   {
      public string DisplayName { get; set; }
      public string ActualFileLocation { get; set; }
      public int CompareTo(object obj)
      {
         ExpectedDetailResult other = obj as ExpectedDetailResult;
         return (other!= null)?DisplayName.CompareTo(other.DisplayName):0;
      }
   }
}
