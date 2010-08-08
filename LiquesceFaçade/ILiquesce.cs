using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiquesceFaçade
{
   [Serializable]
   public enum LiquesceSvcState
   {
      Unknown,
      Running, // Used to indicate that all is well
      InError,
      InWarning
   }

   public interface ILiquesce
   {
      void Stop();

      bool Start();

      LiquesceSvcState State { get; }

      ConfigDetails ConfigDetails { get; set; }
   }
}
