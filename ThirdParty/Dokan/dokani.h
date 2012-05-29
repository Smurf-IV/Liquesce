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

#pragma once

#ifndef _DOKANI_H_
#define _DOKANI_H_

#ifndef _WINDOWS_
#include <windows.h>
#endif
#ifndef _INC_STDIO
#include <stdio.h>
#endif
#ifndef _INC_STDLIB
#include <stdlib.h>
#endif

#ifndef _PUBLIC_H_
#include "public.h"
#endif
#ifndef _DOKAN_H_
#include "dokan.h"
#endif
#ifndef _DOKANC_H_
#include "dokanc.h"
#endif
#ifndef _LIST_H_
#include "list.h"
#endif

//#ifdef __cplusplus
//extern "C" {
//#endif



class DOKAN_INSTANCE 
{
public:
   DOKAN_INSTANCE()
   {
      ZeroMemory( DeviceName, 64 * sizeof(WCHAR) );
      ZeroMemory( MountPoint, MAX_PATH * sizeof(WCHAR) );
      DeviceNumber = 0UL;
      MountId = 0UL;
      DokanOptions = NULL;
      DokanOperations = NULL;
      ZeroMemory( &ListEntry, sizeof(LIST_ENTRY) );
   }
   // to ensure that unmount dispatch is called at once
   CComAutoCriticalSection	CriticalSection;

   // store CurrentDeviceName
   // (when there are many mounts, each mount use 
   // other DeviceName)
   WCHAR	DeviceName[64];
   WCHAR	MountPoint[MAX_PATH];

   ULONG	DeviceNumber;
   ULONG	MountId;

   PDOKAN_OPTIONS		DokanOptions;
   PDOKAN_OPERATIONS	DokanOperations;

   LIST_ENTRY	ListEntry;
} 
typedef DOKAN_INSTANCE, *PDOKAN_INSTANCE;


typedef struct _DOKAN_OPEN_INFO 
{
   BOOL			IsDirectory;
   volatile ULONG			OpenCount;
   PEVENT_CONTEXT	EventContext;
   PDOKAN_INSTANCE	DokanInstance;
   ULONG64			UserContext;
   ULONG			EventId;
   PLIST_ENTRY		DirListHead;
} DOKAN_OPEN_INFO, *PDOKAN_OPEN_INFO;


const bool DokanStart( PDOKAN_INSTANCE	Instance);

const bool SendToDevice( LPCWSTR	DeviceName,
   DWORD	IoControlCode,
   PVOID	InputBuffer,
   ULONG	InputLength,
   PVOID	OutputBuffer,
   ULONG	OutputLength,
   PULONG	ReturnedLength);

LPCWSTR GetRawDeviceName(LPCWSTR	DeviceName);

BOOL DokanMount( LPCWSTR	MountPoint, LPCWSTR	DeviceName);

VOID SendEventInformation( HANDLE				Handle,
   PEVENT_INFORMATION	EventInfo,
   ULONG				EventLength,
   PDOKAN_INSTANCE		DokanInstance);


PEVENT_INFORMATION DispatchCommon( PEVENT_CONTEXT		EventContext,
   ULONG				SizeOfEventInfo,
   PDOKAN_INSTANCE		DokanInstance,
   PDOKAN_FILE_INFO	DokanFileInfo,
   PDOKAN_OPEN_INFO*	DokanOpenInfo);


VOID DispatchDirectoryInformation( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchQueryInformation( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchQueryVolumeInformation( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchSetInformation( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchRead( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchWrite( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchCreate( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchClose( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchCleanup( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchFlush( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchUnmount( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchLock( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchQuerySecurity( HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance);


VOID DispatchSetSecurity( HANDLE			Handle,
   PEVENT_CONTEXT	EventContext,
   PDOKAN_INSTANCE	DokanInstance);


//BOOLEAN InstallDriver( SC_HANDLE  SchSCManager,
//   LPCWSTR    DriverName,
//   LPCWSTR    ServiceExe);
//
//
//BOOLEAN RemoveDriver( SC_HANDLE  SchSCManager,
//    LPCWSTR    DriverName);
//
//
//BOOLEAN StartDriver( SC_HANDLE  SchSCManager,
//    LPCWSTR    DriverName);
//
//
//BOOLEAN StopDriver( SC_HANDLE  SchSCManager,
//    LPCWSTR    DriverName);
//
//
//BOOLEAN ManageDriver( LPCWSTR  DriverName,
//    LPCWSTR  ServiceName,
//    USHORT   Function);
//

BOOL SendReleaseIRP( LPCWSTR DeviceName);

VOID CheckFileName( LPWSTR	FileName);

VOID ClearFindData( PLIST_ENTRY	ListHead);

UINT WINAPI DokanLoop( PVOID /*PDOKAN_INSTANCE*/ Param);

UINT WINAPI DokanKeepAlive( PVOID /*PDOKAN_INSTANCE*/ Param);


ULONG GetNTStatus(DWORD ErrorCode);

PDOKAN_OPEN_INFO GetDokanOpenInfo( PEVENT_CONTEXT EventInfomation, PDOKAN_INSTANCE DokanInstance);

VOID ReleaseDokanOpenInfo( PEVENT_INFORMATION EventInfomation, PDOKAN_INSTANCE DokanInstance);


//#ifdef __cplusplus
//}
//#endif


#endif
