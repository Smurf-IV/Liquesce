using System;
using System.Diagnostics;
using CallbackFS;
using NLog;

namespace CBFS
{
   /// <summary>
   /// These events are optional, i.e. you should not attach an event handler if you don't plan to handle it. 
   /// </summary>
   internal interface ICBFSFuncsAdvancedStreams
   {
      /// <summary>
      /// The application must report information about the entry in the file specified by FileInfo.
      /// If the entry is present, NamedStreamFound must be set to true and the information about the entry must be included.
      /// If the entry is not present, NamedStreamFound must be set to false. 
      /// If this is the first call to enumerate the streams, Context in EnumerationInfo can be used to store information,
      /// which speeds up subsequent enumeration calls. The application can use Context to store the reference to some 
      /// information, identifying the search (such as stream or file handle or database record ID etc). 
      /// The value, set in the event handler, is later passed to all operations, related to this enumeration, 
      /// i.e. subsequent calls to OnEnumerateNamedStreams event handler. 
      /// </summary>
      /// <param name="fileInfo"></param>
      /// <param name="userContextInfo"></param>
      /// <param name="namedStreamsEnumerationInfo"></param>
      /// <param name="streamName"></param>
      /// <param name="streamSize"></param>
      /// <param name="streamAllocationSize"></param>
      /// <param name="aNamedStreamFound"></param>
      void EnumerateNamedStreams(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo, ref string streamName, ref long streamSize, ref long streamAllocationSize, out bool aNamedStreamFound);

      /// <summary>
      /// This event is fired when the OS has finished enumerating named streams of the file and requests the resources, allocated for enumeration, to be released. 
      /// </summary>
      /// <param name="fileInfo"></param>
      /// <param name="namedStreamsEnumerationInfo"></param>
      void CloseNamedStreamsEnumeration(CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo);

   }


   internal interface ICBFSFuncsAdvancedSecurity
   {
      /// <summary>
      /// This event is fired when the OS wants to change file security attributes. Use SecurityInformation parameter to
      /// find out, what exactly security information must be set. Detailed information about SECURITY_INFORMATION and
      /// SECURITY_DESCRIPTOR can be found in MSDN Library or Windows NT Platform SDK. The passed security descriptor 
      /// is in the self-relative format, where all security information is stored in a contiguous block of memory. 
      /// For details see MSDN for self-relative and absolute security descriptor formats. 
      /// </summary>
      /// <param name="fileInfo"></param>
      /// <param name="userContextInfo"></param>
      /// <param name="securityInformation"></param>
      /// <param name="SecurityDescriptor"></param>
      /// <param name="length"></param>
      void SetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint securityInformation, IntPtr SecurityDescriptor, uint length);

      /// <summary>
      /// This event is fired when the OS wants to obtain file security attributes. 
      /// Use SecurityInformation parameter to find out, what exactly security information must be provided. 
      /// Detailed information about SECURITY_INFORMATION and SECURITY_DESCRIPTOR can be found in MSDN Library or 
      /// Windows NT Platform SDK. The returned security descriptor must be in the self-relative format, where all
      /// security information is stored in a contiguous block of memory. For details see MSDN for self-relative and
      /// absolute security descriptor formats. 
      /// </summary>
      /// <remarks>
      /// NOTE: the system calls the callback twice. 
      /// First it passes 0 in Length parameter and the application must provide the necessary length for the data
      /// in LengthNeeded parameter. Then the OS will call the callback for the second time, 
      /// passing the actual buffer for the data. 
      /// </remarks>
      /// <param name="fileInfo"></param>
      /// <param name="userContextInfo"></param>
      /// <param name="RequestedInformation"></param>
      /// <param name="SecurityDescriptor"></param>
      /// <param name="Length"></param>
      /// <param name="lengthNeeded"></param>
      void GetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint RequestedInformation, IntPtr SecurityDescriptor, uint Length, out uint lengthNeeded);
   }

   interface ICBFSFuncsAdvancedFile
   {
      
   /// <summary>
      /// Use this event to provide file path by it's unique ID.
      /// </summary>
      /// <param name="FileId"></param>
      /// <returns>FilePath</returns>
      string GetFileNameByFileId(long FileId);

      /// <summary>
      /// This event is fired when the OS tells the file system, that file buffers (incuding all possible metadata)
      /// must be flushed and written to the backend storage. FileInfo contains information about the file to be flushed.
      /// If FileInfo is empty, your code should attempt to flush everything, related to the disk.
      /// </summary>
      /// <param name="FileInfo"></param>
      void FlushFile(CbFsFileInfo FileInfo);

      /// <summary>
      /// This event is fired when the storage is removed by the user using Eject command in Explorer.
      /// When the event is fired, the storage has been completely destroyed.
      /// You don't need to call UnmountMedia() or DeleteStorage() methods. 
      /// </summary>
      void StorageEjected();
   }

   // ReSharper disable RedundantAssignment
   /// <summary>
   /// Class that forces the abstraction from the CBFS, and handles the error exception conversion.
   /// </summary>
   public abstract class CBFSHandlersAdvancedStreams : CBFSHandlers, ICBFSFuncsAdvancedStreams
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      public CBFSHandlersAdvancedStreams()
      {
      }

      private void EnumerateNamedStreams(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsHandleInfo handleinfo,
                                         CbFsNamedStreamsEnumerationInfo namedstreamsenumerationinfo,
                                         ref string streamname, ref long streamsize, ref long streamallocationsize,
                                         ref bool namedstreamfound)
      {
         Log.Trace("EnumerateNamedStreams IN");
         try
         {
            EnumerateNamedStreams(fileInfo, handleinfo, namedstreamsenumerationinfo,
               ref streamname, ref streamsize, ref streamallocationsize, out namedstreamfound);
         }
         catch (Exception ex)
         {
            CBFSWinUtil.BestAttemptToECBFSError(ex);
         }
         finally
         {
            Log.Trace("EnumerateNamedStreams OUT");
         }
      }
      public abstract void EnumerateNamedStreams(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo, ref string streamName, ref long streamSize, ref long streamAllocationSize, out bool aNamedStreamFound);

      private void CloseNamedStreamsEnumeration(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo)
      {
         CBFSWinUtil.Invoke("CloseNamedStreamsEnumeration", () => CloseNamedStreamsEnumeration(fileInfo, namedStreamsEnumerationInfo));
      }
      public abstract void CloseNamedStreamsEnumeration(CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo);
   }

   public abstract class CBFSHandlersAdvanced : CBFSHandlers, ICBFSFuncsAdvancedSecurity, ICBFSFuncsAdvancedStreams
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();

      public CBFSHandlersAdvanced()
      {
         CbFs.OnSetFileSecurity = SetFileSecurity;
         CbFs.OnGetFileSecurity = GetFileSecurity;
         CbFs.OnEnumerateNamedStreams = EnumerateNamedStreams;
         CbFs.OnCloseNamedStreamsEnumeration = CloseNamedStreamsEnumeration;

      }


      private void SetFileSecurity(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsHandleInfo handleinfo,
                                   uint securityinformation, IntPtr securitydescriptor, uint length)
      {
         CBFSWinUtil.Invoke("SetFileSecurity", () =>
                                          SetFileSecurity(fileInfo, handleinfo, securityinformation, securitydescriptor, length));
      }

      public abstract void SetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint securityInformation, IntPtr SecurityDescriptor, uint length);

      [DebuggerHidden] // Stop firing for the "Too small buffer" error
      private void GetFileSecurity(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsHandleInfo handleinfo,
                                   uint securityinformation, IntPtr securitydescriptor, uint length,
                                   ref uint lengthNeeded)
      {
         Log.Trace("GetFileSecurity IN");
         try
         {
            GetFileSecurity(fileInfo, handleinfo, securityinformation, securitydescriptor, length, out lengthNeeded);
         }
         catch (Exception ex)
         {
            CBFSWinUtil.BestAttemptToECBFSError(ex);
         }
         finally
         {
            Log.Trace("GetFileSecurity OUT");
         }
      }

      public abstract void GetFileSecurity(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, uint RequestedInformation, IntPtr SecurityDescriptor, uint Length, out uint lengthNeeded);


      private void EnumerateNamedStreams(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsHandleInfo handleinfo,
                                   CbFsNamedStreamsEnumerationInfo namedstreamsenumerationinfo,
                                   ref string streamname, ref long streamsize, ref long streamallocationsize,
                                   ref bool namedstreamfound)
      {
         Log.Trace("EnumerateNamedStreams IN");
         try
         {
            EnumerateNamedStreams(fileInfo, handleinfo, namedstreamsenumerationinfo,
               ref streamname, ref streamsize, ref streamallocationsize, out namedstreamfound);
         }
         catch (Exception ex)
         {
            CBFSWinUtil.BestAttemptToECBFSError(ex);
         }
         finally
         {
            Log.Trace("EnumerateNamedStreams OUT");
         }
      }

      public abstract void EnumerateNamedStreams(CbFsFileInfo fileInfo, CbFsHandleInfo userContextInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo, ref string streamName, ref long streamSize, ref long streamAllocationSize, out bool aNamedStreamFound);

      private void CloseNamedStreamsEnumeration(CallbackFileSystem sender, CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo)
      {
         CBFSWinUtil.Invoke("CloseNamedStreamsEnumeration", () => CloseNamedStreamsEnumeration(fileInfo, namedStreamsEnumerationInfo));
      }
      public abstract void CloseNamedStreamsEnumeration(CbFsFileInfo fileInfo, CbFsNamedStreamsEnumerationInfo namedStreamsEnumerationInfo);

   }
      
   
   public abstract class CBFSHandlersAdvancedFile : CBFSHandlers, ICBFSFuncsAdvancedFile
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public CBFSHandlersAdvancedFile()
      {
         CbFs.OnGetFileNameByFileId = GetFileNameByFileId;
         CbFs.OnStorageEjected = StorageEjected;
      }


   private void GetFileNameByFileId(CallbackFileSystem sender, long fileId, ref string FilePath, ref ushort filePathLength)
      {
         string filePath = string.Empty;
         CBFSWinUtil.Invoke("GetFileNameByFileId", () => filePath = GetFileNameByFileId(fileId));
         FilePath = filePath;
         filePathLength = (ushort)FilePath.Length;
      }
      public abstract string GetFileNameByFileId(long FileId);

      private void StorageEjected(CallbackFileSystem sender)
      {
         CBFSWinUtil.Invoke("StorageEjected", StorageEjected);
      }
      public abstract void StorageEjected();
   }
}