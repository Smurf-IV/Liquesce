/*
Dokan : user-mode file system library for Windows

Copyright (C) 2010 Hiroki Asakawa info@dokan-dev.net

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

#include "dokani.h"
#include "fileinfo.h"

//////////////////////////////////////////////////////////////////////////////

#ifdef __ATLDBGMEM_H__ 
#undef THIS_FILE
#undef new
static char THIS_FILE[] = __FILE__;
#define new(nSize) ATL::AtlAllocMemoryDebug(nSize, __FILE__, __LINE__) 
#define delete(pbData) ATL::AtlFreeMemoryDebug(pbData) 
#endif


//////////////////////////////////////////////////////////////////////////////

STDDLLEXAPI_(HANDLE ) DokanOpenRequestorToken(PDOKAN_FILE_INFO FileInfo)
{
   PDOKAN_OPEN_INFO openInfo = (PDOKAN_OPEN_INFO)FileInfo->DokanContext;
   if (openInfo == NULL)
   {
      return INVALID_HANDLE_VALUE;
   }

   PEVENT_CONTEXT eventContext = openInfo->EventContext;
   if (eventContext == NULL)
   {
      return INVALID_HANDLE_VALUE;
   }

   PDOKAN_INSTANCE instance = openInfo->DokanInstance;
   if (instance == NULL)
   {
      return INVALID_HANDLE_VALUE;
   }

   if (eventContext->MajorFunction != IRP_MJ_CREATE)
   {
      return INVALID_HANDLE_VALUE;
   }

   CONST ULONG eventInfoSize( sizeof(EVENT_INFORMATION) );
   EVENT_INFORMATION eventInfo;
   ZeroMemory(&eventInfo, eventInfoSize);

   eventInfo.SerialNumber = eventContext->SerialNumber;

   ULONG	returnedLength;
   HANDLE handle = INVALID_HANDLE_VALUE;
   const bool status( SendToDevice( NULL, GetRawDeviceName(instance->DeviceName),
      IOCTL_GET_ACCESS_TOKEN,
      &eventInfo,
      eventInfoSize,
      &eventInfo,
      eventInfoSize,
      &returnedLength));

   if (status)
   {
      handle = eventInfo.AccessToken.Handle;
   } 
   else
   {
      DbgPrint(instance->DokanOperations->DebugOutString, L"IOCTL_GET_ACCESS_TOKEN failed\n");
   }
   return handle;
}