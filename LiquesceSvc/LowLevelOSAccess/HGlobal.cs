using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LiquesceSvc.LowLevelOSAccess
{
   /// <summary>
   /// Stoeln from https://stackoverflow.com/questions/3525932/should-marshal-freehglobal-be-placed-in-a-finally-block-to-ensure-resources-are
   /// </summary>
   /// <example>
   /// <code>
   /// using (var h = new HGlobal(buffer.Length))
   /// {
   ///   h.WriteArray(0, buffer, 0, buffer.Length);
   /// }
   /// </code>
   /// </example>
   internal class HGlobal : SafeHandleZeroOrMinusOneIsInvalid
   {
      public HGlobal(int cb)
         : base(true)
      {
         SetHandle(Marshal.AllocHGlobal(cb));
      }

      protected override bool ReleaseHandle()
      {
         Marshal.FreeHGlobal(handle);
         return true;
      }

      public static implicit operator IntPtr(HGlobal w)
      {
         return w.DangerousGetHandle();
      }
   }
}