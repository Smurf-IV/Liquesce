// The code in this file has been built up from lot's of trawling of the internet.
// So I cannot claim any Copyright or usage on it.
// Mostly taken from http://msdn.microsoft.com/en-us/library/cc303707.aspx
// It's your risk !

using System;
using System.Runtime.InteropServices;
using System.Transactions;
using Microsoft.Win32.SafeHandles;

namespace LiquesceSvc
{
   [Guid("79427A2B-F895-40e0-BE79-B57DC82ED231"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
   public interface IKernelTransaction
   {
      int GetHandle(out IntPtr pHandle);
   }

   [System.Security.SuppressUnmanagedCodeSecurity]
   public class KtmTransactionHandle : SafeHandleZeroOrMinusOneIsInvalid
   {
      private KtmTransactionHandle(IntPtr handle)
         : base(true)
      {
         this.handle = handle;
      }

      public static bool IsAvailable
      {
         get { return Transaction.Current != null; }
      }

      public static KtmTransactionHandle CreateKtmTransactionHandle()
      {
         if (Transaction.Current == null)
         {
            throw new InvalidOperationException("Cannot create a KTM handle without Transaction.Current");
         }

         return CreateKtmTransactionHandle(Transaction.Current);
      }

      public static KtmTransactionHandle CreateKtmTransactionHandle(Transaction managedTransaction)
      {
         IDtcTransaction dtcTransaction = TransactionInterop.GetDtcTransaction(managedTransaction);

         IKernelTransaction ktmInterface = (IKernelTransaction)dtcTransaction;

         IntPtr ktmTxHandle;
         int hr = ktmInterface.GetHandle(out ktmTxHandle);
         HandleError(hr);

         return new KtmTransactionHandle(ktmTxHandle);
      }

      protected override bool ReleaseHandle()
      {
         return CloseHandle(handle);
      }

      private static void HandleError(int error)
      {
         if (error != ERROR_SUCCESS)
         {
            throw new System.ComponentModel.Win32Exception(error);
         }
      }

      private const int ERROR_SUCCESS = 0;

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      [return: MarshalAs(UnmanagedType.Bool)]
      private static extern bool CloseHandle( [In] IntPtr handle);

   }
}
