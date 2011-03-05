using System;
using System.Threading;

namespace LiquesceSvcMEF
{
   // Stolen from http://stackoverflow.com/questions/407238/readerwriterlockslim-vs-monitor
   //
   internal static class RWLSExtension
   {
      public static ReadLockHelper ReadLock(this ReaderWriterLockSlim readerWriterLock)
      {
         return new ReadLockHelper(readerWriterLock);
      }

      public static UpgradeableReadLockHelper UpgradableReadLock(this ReaderWriterLockSlim readerWriterLock)
      {
         return new UpgradeableReadLockHelper(readerWriterLock);
      }

      public static WriteLockHelper WriteLock(this ReaderWriterLockSlim readerWriterLock)
      {
         return new WriteLockHelper(readerWriterLock);
      }

      public struct ReadLockHelper : IDisposable
      {
         private readonly ReaderWriterLockSlim readerWriterLock;

         public ReadLockHelper(ReaderWriterLockSlim readerWriterLock)
         {
            readerWriterLock.EnterReadLock();
            this.readerWriterLock = readerWriterLock;
         }

         public void Dispose()
         {
            this.readerWriterLock.ExitReadLock();
         }
      }

      public struct UpgradeableReadLockHelper : IDisposable
      {
         private readonly ReaderWriterLockSlim readerWriterLock;

         public UpgradeableReadLockHelper(ReaderWriterLockSlim readerWriterLock)
         {
            readerWriterLock.EnterUpgradeableReadLock();
            this.readerWriterLock = readerWriterLock;
         }

         public void Dispose()
         {
            this.readerWriterLock.ExitUpgradeableReadLock();
         }
      }

      public struct WriteLockHelper : IDisposable
      {
         private readonly ReaderWriterLockSlim readerWriterLock;

         public WriteLockHelper(ReaderWriterLockSlim readerWriterLock)
         {
            readerWriterLock.EnterWriteLock();
            this.readerWriterLock = readerWriterLock;
         }

         public void Dispose()
         {
            this.readerWriterLock.ExitWriteLock();
         }
      }
   }

   // Example usage

   //class ReaderWriterLockedList<T> : SlowList<T>
   //{

   //   ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim();

   //   public override T this[int index]
   //   {
   //      get
   //      {
   //         using (slimLock.ReadLock())
   //         {
   //            return base[index];
   //         }
   //      }
   //      set
   //      {
   //         using (slimLock.WriteLock())
   //         {
   //            base[index] = value;
   //         }
   //      }
   //   }
   //}

}
