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


VOID DispatchCreate( HANDLE Handle, PEVENT_CONTEXT EventContext, PDOKAN_INSTANCE DokanInstance)
{
   static long eventId = 0;
   CheckFileName(EventContext->Create.FileName);

   CONST ULONG length( sizeof(EVENT_INFORMATION) );
   EVENT_INFORMATION _eventInfo;
   ZeroMemory( &_eventInfo, length );
   PEVENT_INFORMATION eventInfo = &_eventInfo;

   eventInfo->BufferLength = 0;
   eventInfo->SerialNumber = EventContext->SerialNumber;

   // DOKAN_OPEN_INFO is structure for a opened file
   // this will be freed by Close
   PDOKAN_OPEN_INFO openInfo = (PDOKAN_OPEN_INFO)new(sizeof(DOKAN_OPEN_INFO));
   ZeroMemory(openInfo, sizeof(DOKAN_OPEN_INFO));
   openInfo->OpenCount = 2;
   openInfo->EventContext = EventContext;
   openInfo->DokanInstance = DokanInstance;

   DOKAN_FILE_INFO fileInfo;
   ZeroMemory(&fileInfo, sizeof(DOKAN_FILE_INFO));
   fileInfo.ProcessId = EventContext->ProcessId;
   fileInfo.DokanOptions = DokanInstance->DokanOptions;
   fileInfo.DokanContext = (ULONG64)openInfo;

   // pass it to driver and when the same handle is used get it back
   eventInfo->Context = (ULONG64)openInfo;

   // The high 8 bits of this parameter correspond to the Disposition parameter
   CONST DWORD disposition( (EventContext->Create.CreateOptions >> 24) & 0x000000ff);

   int status( -1 ); // in case being not dispatched

   // The low 24 bits of this member correspond to the CreateOptions parameter
   CONST DWORD options(EventContext->Create.CreateOptions & FILE_VALID_OPTION_FLAGS );
   DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"Create.CreateOptions 0x%x\n", options);

   // to open directory
   // even if this flag is not specified, 
   // there is a case to open a directory
   fileInfo.IsDirectory = ((options & FILE_DIRECTORY_FILE) == FILE_DIRECTORY_FILE);

   // to open no directory file
   // event if this flag is not specified,
   // there is a case to open non directory file
   if ((options & FILE_NON_DIRECTORY_FILE) == FILE_NON_DIRECTORY_FILE) 
   {
      DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"FILE_NON_DIRECTORY_FILE\n");
   }

   if ((options & FILE_DELETE_ON_CLOSE) == FILE_DELETE_ON_CLOSE)
   {
      EventContext->Create.FileAttributes |= FILE_FLAG_DELETE_ON_CLOSE;
   }

   DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"###Create %04d\n", eventId);
   //DbgPrint("### OpenInfo %X\n", openInfo);
   openInfo->EventId = eventId++;

   DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"   CreateDisposition %X\n", disposition);
   // make a directory or open
   if (fileInfo.IsDirectory != FALSE)
   {
      switch(disposition)
      {
      case FILE_CREATE:
      case FILE_OPEN_IF:
         if (DokanInstance->DokanOperations->CreateDirectory) 
         {
            status = DokanInstance->DokanOperations->CreateDirectory( EventContext->Create.FileName, &fileInfo);
         }
         break;
      case FILE_OPEN:
         if (DokanInstance->DokanOperations->OpenDirectory) 
         {
            status = DokanInstance->DokanOperations->OpenDirectory( EventContext->Create.FileName, &fileInfo);
         }
         break;
      default:
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"### Create other disposition : %d\n", disposition);
         break;
      }

   } 
   else // open a file 
   {
      DWORD creationDisposition( OPEN_EXISTING );
      switch(disposition)
      {
      case FILE_CREATE:
         creationDisposition = CREATE_NEW;
         break;
      case FILE_OPEN:
         creationDisposition = OPEN_EXISTING;
         break;
      case FILE_OPEN_IF:
         creationDisposition = OPEN_ALWAYS;
         break;
      case FILE_OVERWRITE:
         creationDisposition = TRUNCATE_EXISTING;
         break;
      case FILE_OVERWRITE_IF:
         creationDisposition = CREATE_ALWAYS;
         break;
      default:
         // TODO: should support FILE_SUPERSEDE ?
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"### Create other disposition : %X\n", disposition);
         break;
      }

      if(DokanInstance->DokanOperations->CreateFile) 
      {
         status = DokanInstance->DokanOperations->CreateFile(
            EventContext->Create.FileName,
            EventContext->Create.DesiredAccess,
            EventContext->Create.ShareAccess,
            creationDisposition,
            EventContext->Create.FileAttributes,
            &fileInfo);
      }
   }

   // save the information about this access in DOKAN_OPEN_INFO
   openInfo->IsDirectory = fileInfo.IsDirectory;
   openInfo->UserContext = fileInfo.Context;

   // FILE_CREATED
   // FILE_DOES_NOT_EXIST
   // FILE_EXISTS
   // FILE_OPENED
   // FILE_OVERWRITTEN
   // FILE_SUPERSEDED


   if (status < 0) 
   {

      const int error(status * -1);

      DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"CreateFile status = %d\n", status);
      if ((EventContext->Flags & SL_OPEN_TARGET_DIRECTORY) == SL_OPEN_TARGET_DIRECTORY)
      {
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"SL_OPEN_TARGET_DIRECTORY specified\n");
      }
      eventInfo->Create.Information = FILE_DOES_NOT_EXIST;

      switch(error)
      {
      case ERROR_FILE_NOT_FOUND:
         if ((EventContext->Flags & SL_OPEN_TARGET_DIRECTORY) == SL_OPEN_TARGET_DIRECTORY)
            eventInfo->Status = STATUS_SUCCESS;
         else
            eventInfo->Status = STATUS_OBJECT_NAME_NOT_FOUND;
         break;
      case ERROR_PATH_NOT_FOUND:
         //if (EventContext->Flags & SL_OPEN_TARGET_DIRECTORY)
         //	eventInfo->Status = STATUS_SUCCESS;
         //else
         eventInfo->Status = STATUS_OBJECT_PATH_NOT_FOUND;
         break;
      case ERROR_ACCESS_DENIED:
         eventInfo->Status = STATUS_ACCESS_DENIED;
         break;
      case ERROR_SHARING_VIOLATION:
         eventInfo->Status = STATUS_SHARING_VIOLATION;
         break;
      case ERROR_INVALID_NAME:
         eventInfo->Status = STATUS_OBJECT_NAME_NOT_FOUND;
         break;
      case ERROR_FILE_EXISTS:
      case ERROR_ALREADY_EXISTS:		
         eventInfo->Status = STATUS_OBJECT_NAME_COLLISION;
         eventInfo->Create.Information = FILE_EXISTS;
         break;
      case ERROR_PRIVILEGE_NOT_HELD:
         eventInfo->Status = STATUS_PRIVILEGE_NOT_HELD;
         break;
      case ERROR_NOT_READY:
         eventInfo->Status = STATUS_DEVICE_NOT_READY;
         break;
      default:
         eventInfo->Status = STATUS_INVALID_PARAMETER;
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"Create got unknown error code %d\n", error);
         break;
      }


      if (eventInfo->Status != STATUS_SUCCESS) 
      {
         // Needs to free openInfo because Close is never called.
         delete(openInfo);
         eventInfo->Context = 0;
      }

   } 
   else
   {

      //DbgPrint("status = %d\n", status);

      eventInfo->Status = STATUS_SUCCESS;
      eventInfo->Create.Information = FILE_OPENED;

      if (disposition == FILE_CREATE ||
         disposition == FILE_OPEN_IF ||
         disposition == FILE_OVERWRITE_IF) 
      {

         if (status != ERROR_ALREADY_EXISTS)
         {
            eventInfo->Create.Information = FILE_CREATED;
         }
      }

      if ((disposition == FILE_OVERWRITE_IF || disposition == FILE_OVERWRITE) &&
         eventInfo->Create.Information != FILE_CREATED)
      {

         eventInfo->Create.Information = FILE_OVERWRITTEN;
      }

      if (fileInfo.IsDirectory)
         eventInfo->Create.Flags |= DOKAN_FILE_DIRECTORY;
   }

   SendEventInformation(Handle, eventInfo, length, DokanInstance);
   //delete( eventInfo );
}
