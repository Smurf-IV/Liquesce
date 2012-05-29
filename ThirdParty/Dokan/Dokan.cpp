/*
Dokan : user-mode file system library for Windows

Copyright (C) 2008 Hiroki Asakawa info@dokan-dev.net

http://dokan-dev.net/en

This program is free software; you can redistribute it and/or modify it under
the terms of the GNU Lesser General Public License as published by the Free
Software Foundation; either version 3 of the License, or (at your option) any
later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY
WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along
with this program. If not, see <http://www.gnu.org/licenses/>.
*/


#include "stdafx.h"

#include "fileinfo.h"
#include "dokani.h"
#include "list.h"

//////////////////////////////////////////////////////////////////////////////

#ifdef __ATLDBGMEM_H__ 
#undef THIS_FILE
#undef new
static char THIS_FILE[] = __FILE__;
#define new(nSize) ATL::AtlAllocMemoryDebug(nSize, __FILE__, __LINE__) 
#define delete(pbData) ATL::AtlFreeMemoryDebug(pbData) 
#endif


//////////////////////////////////////////////////////////////////////////////


// DokanOptions->DebugMode is ON?
bool g_DebugMode( true );

// DokanOptions->UseStdErr is ON?
bool g_UseStdErr( false );


CRITICAL_SECTION	g_InstanceCriticalSection;
LIST_ENTRY			g_InstanceList;	

PDOKAN_INSTANCE  NewDokanInstance()
{
//   PDOKAN_INSTANCE instance = (PDOKAN_INSTANCE)new(sizeof(DOKAN_INSTANCE));
   PDOKAN_INSTANCE instance = new DOKAN_INSTANCE;
   //ZeroMemory(instance, sizeof(DOKAN_INSTANCE));

//#if _MSC_VER < 1300
//   InitializeCriticalSection(&instance->CriticalSection);
//#else
//   InitializeCriticalSectionAndSpinCount( &instance->CriticalSection, 0x80000400);
//#endif
//
   InitializeListHead(&instance->ListEntry);

   EnterCriticalSection(&g_InstanceCriticalSection);
   InsertTailList(&g_InstanceList, &instance->ListEntry);
   LeaveCriticalSection(&g_InstanceCriticalSection);

   return instance;
}

VOID DeleteDokanInstance(PDOKAN_INSTANCE Instance)
{
   //DeleteCriticalSection(&Instance->CriticalSection);

   EnterCriticalSection(&g_InstanceCriticalSection);
   RemoveEntryList(&Instance->ListEntry);
   LeaveCriticalSection(&g_InstanceCriticalSection);

   delete Instance;
   Instance = NULL;
}

BOOL IsValidDriveLetter(WCHAR DriveLetter)
{
   return (L'd' <= DriveLetter && DriveLetter <= L'z') ||
      (L'D' <= DriveLetter && DriveLetter <= L'Z');
}

VOID DokanDbgPrintW(LPCWSTR format, ...)
{
   WCHAR buffer[512];
   va_list argp;
   va_start(argp, format);
   vswprintf_s(buffer, sizeof(buffer)/sizeof(WCHAR), format, argp);
   va_end(argp);
   if (g_UseStdErr)
      fwprintf(stderr, buffer);
   else
      OutputDebugStringW(buffer);
}

int CheckMountPoint(LPCWSTR	MountPoint)
{
   ULONG	length = wcslen(MountPoint);

   if ((length == 1) ||
      (length == 2 && MountPoint[1] == L':') ||
      (length == 3 && MountPoint[1] == L':' && MountPoint[2] == L'\\'))
   {
      WCHAR driveLetter = MountPoint[0];

      if (IsValidDriveLetter(driveLetter))
      {
         return DOKAN_SUCCESS;
      } 
      else
      {
         DokanDbgPrintW(L"Dokan Error: bad drive letter %s\n", MountPoint);
         return DOKAN_DRIVE_LETTER_ERROR;
      }
   } 
   else if (length > 3)
   {
      HANDLE handle = CreateFile(
         MountPoint, GENERIC_WRITE, 0, NULL, OPEN_EXISTING,
         FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS, NULL);
      if (handle == INVALID_HANDLE_VALUE)
      {
         DokanDbgPrintW(L"Dokan Error: bad mount point %s\n", MountPoint);
         return DOKAN_MOUNT_POINT_ERROR;
      }
      CloseHandle(handle);
      return DOKAN_SUCCESS;
   }
   return DOKAN_MOUNT_POINT_ERROR;
}

int DokaMainInt( PDOKAN_OPTIONS DokanOptions, PDOKAN_OPERATIONS DokanOperations ) 
{
   int error; 
   BOOL	useMountPoint = FALSE;
   ULONG	threadNum = 0;
   ULONG	i;
   HANDLE	device;
   HANDLE	threadIds[DOKAN_MAX_THREAD];
   PDOKAN_INSTANCE instance;

   if (DokanOptions->ThreadCount == 0)
   {
      DokanOptions->ThreadCount = 5;

   } 
   else if (DOKAN_MAX_THREAD-1 < DokanOptions->ThreadCount)
   {
      // DOKAN_MAX_THREAD includes DokanKeepAlive thread, so 
      // available thread is DOKAN_MAX_THREAD -1
      DokanDbgPrintW(L"Dokan Error: too many thread count %d\n", DokanOptions->ThreadCount);
      DokanOptions->ThreadCount = DOKAN_MAX_THREAD-1;
   }

   if (DOKAN_MOUNT_POINT_SUPPORTED_VERSION <= DokanOptions->Version 
      && DokanOptions->MountPoint
      )
   {
      error = CheckMountPoint(DokanOptions->MountPoint);
      if (error != DOKAN_SUCCESS)
      {
         return error;
      }
      useMountPoint = TRUE;
   } 
   else if (!IsValidDriveLetter((WCHAR)DokanOptions->Version))
   {
      // Older versions use the first 2 bytes of DokanOptions struct as DriveLetter.
      DokanDbgPrintW(L"Dokan Error: bad drive letter %wc\n", (WCHAR)DokanOptions->Version);
      return DOKAN_DRIVE_LETTER_ERROR;
   }

   device = CreateFile(
      DOKAN_GLOBAL_DEVICE_NAME,			// lpFileName
      GENERIC_READ|GENERIC_WRITE,			// dwDesiredAccess
      FILE_SHARE_READ|FILE_SHARE_WRITE,	// dwShareMode
      NULL,								// lpSecurityAttributes
      OPEN_EXISTING,						// dwCreationDistribution
      0,									// dwFlagsAndAttributes
      NULL								// hTemplateFile
      );

   if (device == INVALID_HANDLE_VALUE)
   {
      DokanDbgPrintW(L"Dokan Error: CreatFile Failed %s: %d\n", DOKAN_GLOBAL_DEVICE_NAME, GetLastError());
      return DOKAN_DRIVER_INSTALL_ERROR;
   }

   DbgPrint(L"device opened\n");

   instance = NewDokanInstance();
   instance->DokanOptions = DokanOptions;
   instance->DokanOperations = DokanOperations;
   if (useMountPoint) 
   {
      wcscpy_s(instance->MountPoint, sizeof(instance->MountPoint) / sizeof(WCHAR), DokanOptions->MountPoint);
   } 
   else
   {
      // Older versions use the first 2 bytes of DokanOptions struct as DriveLetter.
      instance->MountPoint[0] = (WCHAR)DokanOptions->Version;
      instance->MountPoint[1] = L':';
      instance->MountPoint[2] = L'\\';
   }

   if (!DokanStart(instance))
   {
      return DOKAN_START_ERROR;
   }

   if (!DokanMount(instance->MountPoint, instance->DeviceName))
   {
      SendReleaseIRP(instance->DeviceName);
      DokanDbgPrintW(L"Dokan Error: DefineDosDevice Failed\n");
      return DOKAN_MOUNT_ERROR;
   }

   DbgPrint(L"mounted: %s -> %s\n", instance->MountPoint, instance->DeviceName);

   if (DokanOptions->Options & DOKAN_OPTION_KEEP_ALIVE)
   {
      threadIds[threadNum++] = (HANDLE)_beginthreadex(
         NULL, // Security Atributes
         0, //stack size
         DokanKeepAlive,
         instance, // param
         0, // create flag
         NULL);
   }

   for (i = 0; i < DokanOptions->ThreadCount; ++i)
   {
      threadIds[threadNum++] = (HANDLE)_beginthreadex(
         NULL, // Security Atributes
         0, //stack size
         DokanLoop,
         (PVOID)instance, // param
         0, // create flag
         NULL);
   }


   // wait for thread terminations
   WaitForMultipleObjects(threadNum, threadIds, TRUE, INFINITE);

   for (i = 0; i < threadNum; ++i)
   {
      CloseHandle(threadIds[i]);
   }

   CloseHandle(device);

   Sleep(1000);

   DbgPrint(L"\nunload\n");

   DeleteDokanInstance(instance);

   return DOKAN_SUCCESS;
}

//int DOKANAPI 
STDDLLEXAPI_(int) DokanMain(PDOKAN_OPTIONS DokanOptions, PDOKAN_OPERATIONS DokanOperations)
{
   g_DebugMode = (DokanOptions->Options & DOKAN_OPTION_DEBUG) == DOKAN_OPTION_DEBUG;
   g_UseStdErr = (DokanOptions->Options & DOKAN_OPTION_STDERR) == DOKAN_OPTION_STDERR;

   if (g_UseStdErr)
   {
      DbgPrint(L"Dokan: use stderr\n");
      g_DebugMode = true;
   }

   if (g_DebugMode)
   {
      DbgPrint(L"Dokan: debug mode on\n");
      ATL::AtlEnableMemoryTracking( TRUE );
   }

   const int error( DokaMainInt(DokanOptions, DokanOperations) );
   DokanDbgPrintW(L"DokanMain: Closing with%s\n", AtlGetErrorDescription(AtlHresultFromLastError()));
   if (g_DebugMode)
   {
      ATL::AtlDumpMemoryLeaks();
   }
   return error;
}

LPCWSTR GetRawDeviceName(LPCWSTR	DeviceName)
{
   static WCHAR rawDeviceName[MAX_PATH];
   wcscpy_s(rawDeviceName, MAX_PATH, L"\\\\.");
   wcscat_s(rawDeviceName, MAX_PATH, DeviceName);
   return rawDeviceName;
}

UINT WINAPI DokanLoop( PVOID/*PDOKAN_INSTANCE*/ param )
{
   PDOKAN_INSTANCE DokanInstance = (PDOKAN_INSTANCE)param;
   HANDLE	device;
   char	buffer[EVENT_CONTEXT_MAX_SIZE];
   ULONG	count = 0;
   ULONG	returnedLength;
   DWORD	result = 0;

   RtlZeroMemory(buffer, sizeof(buffer));

   device = CreateFile(
      GetRawDeviceName( DokanInstance->DeviceName), // lpFileName
      GENERIC_READ | GENERIC_WRITE,       // dwDesiredAccess
      FILE_SHARE_READ | FILE_SHARE_WRITE, // dwShareMode
      NULL,                               // lpSecurityAttributes
      OPEN_EXISTING,                      // dwCreationDistribution
      0,                                  // dwFlagsAndAttributes
      NULL                                // hTemplateFile
      );

   if (device == INVALID_HANDLE_VALUE)
   {
      if (g_DebugMode)
      {
         CONST DWORD errorCode(GetLastError());
         DbgPrint(L"DokanLoop: CreateFile failed %ws: %d\n", GetRawDeviceName(DokanInstance->DeviceName), errorCode);
      }
      result = -1;
      _endthreadex(result);
      return result;
   }

   while(1)
   {

      CONST BOOL status( DeviceIoControl(
         device,				// Handle to device
         IOCTL_EVENT_WAIT,	// IO Control code
         NULL,				// Input Buffer to driver.
         0,					// Length of input buffer in bytes.
         buffer,             // Output Buffer from driver.
         sizeof(buffer),		// Length of output buffer in bytes.
         &returnedLength,	// Bytes placed in buffer.
         NULL                // synchronous call
         ));

      if (status == FALSE)
      {
         if (g_DebugMode)
         {
            CONST DWORD errorCode(GetLastError());
            DbgPrint(L"DokanLoop: Ioctl failed with code %d\n", errorCode);
         }
         result = -1;
         break;
      }

      //printf("#%d got notification %d\n", (ULONG)Param, count++);

      if(returnedLength > 0) 
      {
         PEVENT_CONTEXT context = (PEVENT_CONTEXT)buffer;
         if (context->MountId != DokanInstance->MountId)
         {
            DbgPrint(L"DokanLoop: Invalid MountId (expected:%d, acctual:%d)\n", DokanInstance->MountId, context->MountId);
            continue;
         }

         switch (context->MajorFunction)
         {
         case IRP_MJ_CREATE:
            DispatchCreate(device, context, DokanInstance);
            break;
         case IRP_MJ_CLEANUP:
            DispatchCleanup(device, context, DokanInstance);
            break;
         case IRP_MJ_CLOSE:
            DispatchClose(device, context, DokanInstance);
            break;
         case IRP_MJ_DIRECTORY_CONTROL:
            DispatchDirectoryInformation(device, context, DokanInstance);
            break;
         case IRP_MJ_READ:
            DispatchRead(device, context, DokanInstance);
            break;
         case IRP_MJ_WRITE:
            DispatchWrite(device, context, DokanInstance);
            break;
         case IRP_MJ_QUERY_INFORMATION:
            DispatchQueryInformation(device, context, DokanInstance);
            break;
         case IRP_MJ_QUERY_VOLUME_INFORMATION:
            DispatchQueryVolumeInformation(device ,context, DokanInstance);
            break;
         case IRP_MJ_LOCK_CONTROL:
            DispatchLock(device, context, DokanInstance);
            break;
         case IRP_MJ_SET_INFORMATION:
            DispatchSetInformation(device, context, DokanInstance);
            break;
         case IRP_MJ_FLUSH_BUFFERS:
            DispatchFlush(device, context, DokanInstance);
            break;
         case IRP_MJ_QUERY_SECURITY:
            DispatchQuerySecurity(device, context, DokanInstance);
            break;
         case IRP_MJ_SET_SECURITY:
            DispatchSetSecurity(device, context, DokanInstance);
            break;
         case IRP_MJ_SHUTDOWN:
            // this cass is used before unmount not shutdown
            DispatchUnmount(device, context, DokanInstance);
            break;
         default:
            break;
         }

      } 
      else
      {
         DbgPrint(L"DokanLoop: ReturnedLength %d\n", returnedLength);
      }
   }

   CloseHandle(device);
   _endthreadex(result);
   return result;
}



VOID SendEventInformation(
   HANDLE				Handle,
   PEVENT_INFORMATION	EventInfo,
   ULONG				EventLength,
   PDOKAN_INSTANCE		DokanInstance)
{
   ULONG	returnedLength;

   //DbgPrint("###EventInfo->Context %X\n", EventInfo->Context);
   if (DokanInstance != NULL)
   {
      ReleaseDokanOpenInfo(EventInfo, DokanInstance);
   }

   // send event info to driver
   CONST BOOL status( DeviceIoControl(
      Handle,				// Handle to device
      IOCTL_EVENT_INFO,	// IO Control code
      EventInfo,			// Input Buffer to driver.
      EventLength,		// Length of input buffer in bytes.
      NULL,				// Output Buffer from driver.
      0,					// Length of output buffer in bytes.
      &returnedLength,	// Bytes placed in buffer.
      NULL				// synchronous call
      ) );

   if (status == FALSE)
   {
      if (g_DebugMode)
      {
         CONST DWORD errorCode( GetLastError() );
         DbgPrint(L"SendEventInformation: Ioctl failed with code %d\n", errorCode );
      }
   }
}


VOID CheckFileName( LPWSTR	FileName)
{
   // if the begining of file name is "\\",
   // replace it with "\"
   if (FileName[0] == L'\\' && FileName[1] == L'\\')
   {
      int i;
      for (i = 0; FileName[i+1] != L'\0'; ++i)
      {
         FileName[i] = FileName[i+1];
      }
      FileName[i] = L'\0';
   }
}



PEVENT_INFORMATION DispatchCommon(
   PEVENT_CONTEXT		EventContext,
   ULONG				SizeOfEventInfo,
   PDOKAN_INSTANCE		DokanInstance,
   PDOKAN_FILE_INFO	DokanFileInfo,
   PDOKAN_OPEN_INFO*	DokanOpenInfo)
{
   PEVENT_INFORMATION	eventInfo = (PEVENT_INFORMATION)new(SizeOfEventInfo);

   RtlZeroMemory(eventInfo, SizeOfEventInfo);
   RtlZeroMemory(DokanFileInfo, sizeof(DOKAN_FILE_INFO));

   eventInfo->BufferLength = 0;
   eventInfo->SerialNumber = EventContext->SerialNumber;

   DokanFileInfo->ProcessId	= EventContext->ProcessId;
   DokanFileInfo->DokanOptions = DokanInstance->DokanOptions;
   if (EventContext->FileFlags & DOKAN_DELETE_ON_CLOSE)
   {
      DokanFileInfo->DeleteOnClose = 1;
   }
   if (EventContext->FileFlags & DOKAN_PAGING_IO)
   {
      DokanFileInfo->PagingIo = 1;
   }
   if (EventContext->FileFlags & DOKAN_WRITE_TO_END_OF_FILE)
   {
      DokanFileInfo->WriteToEndOfFile = 1;
   }
   if (EventContext->FileFlags & DOKAN_SYNCHRONOUS_IO)
   {
      DokanFileInfo->SynchronousIo = 1;
   }
   if (EventContext->FileFlags & DOKAN_NOCACHE)
   {
      DokanFileInfo->Nocache = 1;
   }

   *DokanOpenInfo = GetDokanOpenInfo(EventContext, DokanInstance);
   if (*DokanOpenInfo == NULL)
   {
      DbgPrint(L"error openInfo is NULL\n");
      return eventInfo;
   }

   DokanFileInfo->Context		= (ULONG64)(*DokanOpenInfo)->UserContext;
   DokanFileInfo->IsDirectory	= (UCHAR)(*DokanOpenInfo)->IsDirectory;
   DokanFileInfo->DokanContext = (ULONG64)(*DokanOpenInfo);

   eventInfo->Context = (ULONG64)(*DokanOpenInfo);

   return eventInfo;
}


PDOKAN_OPEN_INFO GetDokanOpenInfo(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance)
{
   PDOKAN_OPEN_INFO openInfo;
   CComCritSecLock<CComAutoCriticalSection> lock(DokanInstance->CriticalSection, true);

   openInfo = (PDOKAN_OPEN_INFO)EventContext->Context;
   if (openInfo != NULL)
   {
      openInfo->OpenCount++;
      openInfo->EventContext = EventContext;
      openInfo->DokanInstance = DokanInstance;
   }
   return openInfo;
}


VOID ReleaseDokanOpenInfo(
   PEVENT_INFORMATION	EventInformation,
   PDOKAN_INSTANCE		DokanInstance)
{
   PDOKAN_OPEN_INFO openInfo;
   CComCritSecLock<CComAutoCriticalSection> lock(DokanInstance->CriticalSection, true);

   openInfo = (PDOKAN_OPEN_INFO)EventInformation->Context;
   if (openInfo != NULL)
   {
      openInfo->OpenCount--;
      if (openInfo->OpenCount < 1)
      {
         if (openInfo->DirListHead != NULL)
         {
            ClearFindData(openInfo->DirListHead);
            delete(openInfo->DirListHead);
            openInfo->DirListHead = NULL;
         }
         delete(openInfo);
         EventInformation->Context = 0;
      }
   }
}


VOID DispatchUnmount(
   HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance)
{
   DOKAN_FILE_INFO			fileInfo;
   static int count = 0;

   // Unmount is called only once
   CComCritSecLock<CComAutoCriticalSection> lock(DokanInstance->CriticalSection, true);

   if (count > 0)
   {
      return;
   }
   count++;

   RtlZeroMemory(&fileInfo, sizeof(DOKAN_FILE_INFO));

   fileInfo.ProcessId = EventContext->ProcessId;

   if (DokanInstance->DokanOperations->Unmount)
   {
      // ignore return value
      DokanInstance->DokanOperations->Unmount(&fileInfo);
   }
}


// ask driver to release all pending IRP to prepare for Unmount.
BOOL SendReleaseIRP( LPCWSTR	DeviceName)
{
   ULONG	returnedLength;

   DbgPrint(L"SendReleaseIRP\n");

   if (!SendToDevice(
      GetRawDeviceName(DeviceName),
      IOCTL_EVENT_RELEASE,
      NULL,
      0,
      NULL,
      0,
      &returnedLength) )
   {

      DbgPrint(L"SendReleaseIRP: Failed to unmount device:%s\n", DeviceName);
      return FALSE;
   }

   return TRUE;
}


const bool DokanStart(PDOKAN_INSTANCE Instance)
{
   EVENT_START			eventStart;
   EVENT_DRIVER_INFO	driverInfo;
   ULONG		returnedLength = 0;

   ZeroMemory(&eventStart, sizeof(EVENT_START));
   ZeroMemory(&driverInfo, sizeof(EVENT_DRIVER_INFO));

   eventStart.UserVersion = DOKAN_DRIVER_VERSION;
   if (Instance->DokanOptions->Options & DOKAN_OPTION_ALT_STREAM)
   {
      eventStart.Flags |= DOKAN_EVENT_ALTERNATIVE_STREAM_ON;
   }
   if (Instance->DokanOptions->Options & DOKAN_OPTION_KEEP_ALIVE)
   {
      eventStart.Flags |= DOKAN_EVENT_KEEP_ALIVE_ON;
   }
   if (Instance->DokanOptions->Options & DOKAN_OPTION_NETWORK ) 
   {
      eventStart.DeviceType = DOKAN_NETWORK_FILE_SYSTEM;
   }
   if (Instance->DokanOptions->Options & DOKAN_OPTION_REMOVABLE)
   {
      eventStart.Flags |= DOKAN_EVENT_REMOVABLE;
   }

   SendToDevice(
      DOKAN_GLOBAL_DEVICE_NAME,
      IOCTL_EVENT_START,
      &eventStart,
      sizeof(EVENT_START),
      &driverInfo,
      sizeof(EVENT_DRIVER_INFO),
      &returnedLength);

   if (driverInfo.Status == DOKAN_START_FAILED)
   {
      if (driverInfo.DriverVersion != eventStart.UserVersion)
      {
         DokanDbgPrintW( L"Dokan Error: driver version mismatch, driver %X, dll %X\n",
            driverInfo.DriverVersion, eventStart.UserVersion);
      } 
      else
      {
         DokanDbgPrintW(L"Dokan Error: driver start error\n");
      }
      return false;
   } 
   else if (driverInfo.Status == DOKAN_MOUNTED)
   {
      Instance->MountId = driverInfo.MountId;
      Instance->DeviceNumber = driverInfo.DeviceNumber;
      wcscpy_s(Instance->DeviceName, sizeof(Instance->DeviceName) / sizeof(WCHAR), driverInfo.DeviceName);
      return true;
   }
   return false;
}


STDDLLEXAPI_(BOOL ) DokanSetDebugMode( ULONG	Mode)
{
   ULONG returnedLength;
   return (SendToDevice(
      DOKAN_GLOBAL_DEVICE_NAME,
      IOCTL_SET_DEBUG_MODE,
      &Mode,
      sizeof(ULONG),
      NULL,
      0,
      &returnedLength)?TRUE:FALSE);
}


const bool SendToDevice(
   LPCWSTR	DeviceName,
   DWORD	IoControlCode,
   PVOID	InputBuffer,
   ULONG	InputLength,
   PVOID	OutputBuffer,
   ULONG	OutputLength,
   PULONG	ReturnedLength)
{

   HANDLE device = CreateFile(
      DeviceName,							// lpFileName
      GENERIC_READ | GENERIC_WRITE,       // dwDesiredAccess
      FILE_SHARE_READ | FILE_SHARE_WRITE, // dwShareMode
      NULL,                               // lpSecurityAttributes
      OPEN_EXISTING,                      // dwCreationDistribution
      0,                                  // dwFlagsAndAttributes
      NULL                                // hTemplateFile
      );

   if (device == INVALID_HANDLE_VALUE)
   {
      if (g_DebugMode) 
      {
         CONST DWORD dwErrorCode( GetLastError() );
         DbgPrint(L"SendToDevice: Failed to open %s with code %d\n", DeviceName, dwErrorCode);
      }
      return FALSE;
   }

   // see http://msdn.microsoft.com/en-us/library/windows/desktop/aa363147%28v=vs.85%29.aspx
   CONST BOOL status( DeviceIoControl(
      device,                 // Handle to device
      IoControlCode,			// IO Control code
      InputBuffer,		    // Input Buffer to driver.
      InputLength,			// Length of input buffer in bytes.
      OutputBuffer,           // Output Buffer from driver.
      OutputLength,			// Length of output buffer in bytes.
      ReturnedLength,		    // Bytes placed in buffer.
      (LPOVERLAPPED)NULL                    // synchronous call
      ));

   CloseHandle(device);

   // If the operation completes successfully, the return value is nonzero.
   if ( status == FALSE )
   {
      if (g_DebugMode) 
      {
         CONST DWORD dwErrorCode( GetLastError() );
         DbgPrint(L"SendToDevice: Ioctl failed with code %d\n", dwErrorCode);
      }
      return false;
   }

   return true;
}


//BOOL WINAPI 
   extern "C" int APIENTRY DllMain( HINSTANCE Instance, DWORD Reason, LPVOID Reserved)
{
   switch(Reason)
   {
   case DLL_PROCESS_ATTACH:
#if _MSC_VER < 1300
         InitializeCriticalSection(&g_InstanceCriticalSection);
#else
         InitializeCriticalSectionAndSpinCount( &g_InstanceCriticalSection, 0x80000400);
#endif
         InitializeListHead(&g_InstanceList);
      break;			

   case DLL_PROCESS_DETACH:
         EnterCriticalSection(&g_InstanceCriticalSection);

         while(!IsListEmpty(&g_InstanceList)) 
         {
            PLIST_ENTRY entry = RemoveHeadList(&g_InstanceList);
            PDOKAN_INSTANCE instance = CONTAINING_RECORD(entry, DOKAN_INSTANCE, ListEntry);

            DokanRemoveMountPoint(instance->MountPoint);
            DeleteDokanInstance(instance);
         }

         LeaveCriticalSection(&g_InstanceCriticalSection);
         DeleteCriticalSection(&g_InstanceCriticalSection);
      break;
   }
   return TRUE;
}

