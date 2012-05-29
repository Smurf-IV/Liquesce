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

//////////////////////////////////////////////////////////////////////////////

#ifdef __ATLDBGMEM_H__ 
#undef THIS_FILE
#undef new
static char THIS_FILE[] = __FILE__;
#define new(nSize) ATL::AtlAllocMemoryDebug(nSize, __FILE__, __LINE__) 
#define delete(pbData) ATL::AtlFreeMemoryDebug(pbData) 
#endif


//////////////////////////////////////////////////////////////////////////////

STDDLLEXAPI_(BOOL ) DokanResetTimeout(ULONG Timeout, PDOKAN_FILE_INFO FileInfo)
{
   PDOKAN_OPEN_INFO openInfo = (PDOKAN_OPEN_INFO)FileInfo->DokanContext;
   if (openInfo == NULL)
   {
      return FALSE;
   }

   PEVENT_CONTEXT eventContext = openInfo->EventContext;
   if (eventContext == NULL)
   {
      return FALSE;
   }

   PDOKAN_INSTANCE instance = openInfo->DokanInstance;
   if (instance == NULL)
   {
      return FALSE;
   }

   CONST ULONG	eventInfoSize( sizeof(EVENT_INFORMATION) );
   PEVENT_INFORMATION eventInfo = (PEVENT_INFORMATION)new(eventInfoSize);
   ZeroMemory(eventInfo, eventInfoSize);

   eventInfo->SerialNumber = eventContext->SerialNumber;
   eventInfo->ResetTimeout.Timeout = Timeout;

   ULONG	returnedLength;
   const bool status( SendToDevice(
            GetRawDeviceName(instance->DeviceName),
            IOCTL_RESET_TIMEOUT,
            eventInfo,
            eventInfoSize,
            NULL,
            0,
            &returnedLength));
   delete(eventInfo);
   return (status?TRUE:FALSE);
}


UINT WINAPI DokanKeepAlive( PVOID/*PDOKAN_INSTANCE*/ DokanInstance)
{
   ULONG	ReturnedLength;

   HANDLE device = CreateFile(
            GetRawDeviceName( ((PDOKAN_INSTANCE)DokanInstance)->DeviceName),
            GENERIC_READ | GENERIC_WRITE,       // dwDesiredAccess
                FILE_SHARE_READ | FILE_SHARE_WRITE, // dwShareMode
                NULL,                               // lpSecurityAttributes
                OPEN_EXISTING,                      // dwCreationDistribution
                0,                                  // dwFlagsAndAttributes
                NULL                                // hTemplateFile
         );

    while(device != INVALID_HANDLE_VALUE)
    {

      CONST BOOL status( DeviceIoControl(
               device,                 // Handle to device
               IOCTL_KEEPALIVE,			// IO Control code
               NULL,		    // Input Buffer to driver.
               0,			// Length of input buffer in bytes.
               NULL,           // Output Buffer from driver.
               0,			// Length of output buffer in bytes.
               &ReturnedLength,		    // Bytes placed in buffer.
               NULL                    // synchronous call
            ));
      if (status == FALSE)
      {
         break;
      }
      Sleep(DOKAN_KEEPALIVE_TIME);
   }

   CloseHandle(device);

   _endthreadex(0);
   return 0;
}
