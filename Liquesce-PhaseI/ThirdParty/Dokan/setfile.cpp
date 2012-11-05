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


int DokanSetAllocationInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{
   PFILE_ALLOCATION_INFORMATION allocInfo = (PFILE_ALLOCATION_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);

   // A file's allocation size and end-of-file position are independent of each other,
   // with the following exception: The end-of-file position must always be less than
   // or equal to the allocation size. If the allocation size is set to a value that
   // is less than the end-of-file position, the end-of-file position is automatically
   // adjusted to match the allocation size.

   if (DokanOperations->SetAllocationSize)
   {
      return DokanOperations->SetAllocationSize( EventContext->SetFile.FileName, allocInfo->AllocationSize.QuadPart, FileInfo);
   }
   // How can we check the current end-of-file position?
   if (allocInfo->AllocationSize.QuadPart == 0)
   {
      return DokanOperations->SetEndOfFile( EventContext->SetFile.FileName, allocInfo->AllocationSize.QuadPart, FileInfo);
   } 
   else
   {
      DbgPrint(DokanOperations->DebugOutString, L"  SetAllocationInformation %I64d, can't handle this parameter.\n", allocInfo->AllocationSize.QuadPart);
   }

   return 0;
}


int DokanSetBasicInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{

   if (!DokanOperations->SetFileAttributes)
      return -1;

   if (!DokanOperations->SetFileTime)
      return -1;

   PFILE_BASIC_INFORMATION basicInfo = (PFILE_BASIC_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);

   const int status( DokanOperations->SetFileAttributes(
      EventContext->SetFile.FileName,
      basicInfo->FileAttributes,
      FileInfo) );

   if (status < 0)
      return status;

   FILETIME creation, lastAccess, lastWrite;
   creation.dwLowDateTime = basicInfo->CreationTime.LowPart;
   creation.dwHighDateTime = basicInfo->CreationTime.HighPart;
   lastAccess.dwLowDateTime = basicInfo->LastAccessTime.LowPart;
   lastAccess.dwHighDateTime = basicInfo->LastAccessTime.HighPart;
   lastWrite.dwLowDateTime = basicInfo->LastWriteTime.LowPart;
   lastWrite.dwHighDateTime = basicInfo->LastWriteTime.HighPart;

   return DokanOperations->SetFileTime( EventContext->SetFile.FileName, &creation, &lastAccess, &lastWrite, FileInfo);
}


int DokanSetDispositionInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{
   if (!DokanOperations->DeleteFile || !DokanOperations->DeleteDirectory)
      return -1;

   PFILE_DISPOSITION_INFORMATION dispositionInfo = (PFILE_DISPOSITION_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
   if (!dispositionInfo->DeleteFile)
   {
      return 0;
   }

   if (FileInfo->IsDirectory)
   {
      return DokanOperations->DeleteDirectory( EventContext->SetFile.FileName, FileInfo);
   } 
   else
   {
      return DokanOperations->DeleteFile( EventContext->SetFile.FileName, FileInfo);
   }
}


int DokanSetEndOfFileInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{
   if (!DokanOperations->SetEndOfFile)
      return -1;

   PFILE_END_OF_FILE_INFORMATION endInfo = (PFILE_END_OF_FILE_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);

   return DokanOperations->SetEndOfFile( EventContext->SetFile.FileName, endInfo->EndOfFile.QuadPart, FileInfo);
}


int DokanSetLinkInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	/*FileInfo*/,
   PDOKAN_OPERATIONS	/*DokanOperations*/)
{
   PDOKAN_LINK_INFORMATION linkInfo = (PDOKAN_LINK_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
   DBG_UNREFERENCED_LOCAL_VARIABLE( linkInfo);
   return -1;
}



int DokanSetRenameInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{
   if (!DokanOperations->MoveFile)
      return -1;

   PDOKAN_RENAME_INFORMATION renameInfo = (PDOKAN_RENAME_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
   
   // See https://code.google.com/p/dokan/issues/detail?id=238
   CONST ULONG newLength(renameInfo->FileNameLength/sizeof(WCHAR));
   DbgPrint(DokanOperations->DebugOutString, L"###DokanSetRenameInformation Source[%*s] Destination=[%*s] length[%i] IsReplacing=[%i]\n", 
      EventContext->SetFile.FileNameLength/sizeof(WCHAR), EventContext->SetFile.FileName, newLength, renameInfo->FileName, newLength, renameInfo->ReplaceIfExists);

   WCHAR newName[MAX_PATH];
   ZeroMemory(newName, sizeof(newName));

   if (renameInfo->FileName[0] != L'\\')
   {
      ULONG pos;
      if ( EventContext->ProcessId == 4)
      {
         DbgPrint(DokanOperations->DebugOutString, L"###DokanSetRenameInformation Attempt to workaround https://code.google.com/p/dokan/issues/detail?id=240\n" );
         RtlCopyMemory(newName, L"\\", 1*sizeof(WCHAR));
         pos = 0;
      }
      else
      {
         for (pos = EventContext->SetFile.FileNameLength/sizeof(WCHAR); pos != 0; --pos)
         {
            if (EventContext->SetFile.FileName[pos] == '\\')
               break;
         }
         RtlCopyMemory(newName, EventContext->SetFile.FileName, (pos+1)*sizeof(WCHAR));
      }
      RtlCopyMemory((PCHAR)newName + (pos+1)*sizeof(WCHAR), renameInfo->FileName, renameInfo->FileNameLength);
   } 
   else
      RtlCopyMemory(newName, renameInfo->FileName, renameInfo->FileNameLength);

   DbgPrint(DokanOperations->DebugOutString, L"###DokanSetRenameInformation newName is [%s]\n", newName );

   return DokanOperations->MoveFile( EventContext->SetFile.FileName, newName, renameInfo->ReplaceIfExists, FileInfo);
}


int DokanSetValidDataLengthInformation(
   PEVENT_CONTEXT		EventContext,
   PDOKAN_FILE_INFO	FileInfo,
   PDOKAN_OPERATIONS	DokanOperations)
{
   if (!DokanOperations->SetEndOfFile)
      return -1;

   PFILE_VALID_DATA_LENGTH_INFORMATION validInfo = (PFILE_VALID_DATA_LENGTH_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);

   return DokanOperations->SetEndOfFile( EventContext->SetFile.FileName, validInfo->ValidDataLength.QuadPart, FileInfo);
}


VOID DispatchSetInformation(
   HANDLE				Handle,
   PEVENT_CONTEXT		EventContext,
   PDOKAN_INSTANCE		DokanInstance)
{
   ULONG sizeOfEventInfo( sizeof(EVENT_INFORMATION) );

   if (EventContext->SetFile.FileInformationClass == FileRenameInformation)
   {
      PDOKAN_RENAME_INFORMATION renameInfo = (PDOKAN_RENAME_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
      sizeOfEventInfo += renameInfo->FileNameLength;
   }

   CheckFileName(EventContext->SetFile.FileName);

   DOKAN_FILE_INFO fileInfo;
   PDOKAN_OPEN_INFO openInfo;
   PEVENT_INFORMATION eventInfo = DispatchCommon(EventContext, sizeOfEventInfo, DokanInstance, &fileInfo, &openInfo);

   DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"###SetFileInfo %04d\n", openInfo != NULL ? openInfo->EventId : -1);

   int status(-1);
   switch (EventContext->SetFile.FileInformationClass)
   {
   case FileAllocationInformation:
      status = DokanSetAllocationInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FileBasicInformation:
      status = DokanSetBasicInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FileDispositionInformation:
      status = DokanSetDispositionInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FileEndOfFileInformation:
      status = DokanSetEndOfFileInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FileLinkInformation:
      status = DokanSetLinkInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FilePositionInformation:
      // this case is dealt with by driver
      status = -1;
      break;

   case FileRenameInformation:
      status = DokanSetRenameInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;

   case FileValidDataLengthInformation:
      status = DokanSetValidDataLengthInformation( EventContext, &fileInfo, DokanInstance->DokanOperations);
      break;
   }

   openInfo->UserContext = fileInfo.Context;

   eventInfo->BufferLength = 0;

   if (EventContext->SetFile.FileInformationClass == FileDispositionInformation)
   {
      if (status == 0)
      {
         PFILE_DISPOSITION_INFORMATION dispositionInfo = (PFILE_DISPOSITION_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
         eventInfo->Delete.DeleteOnClose = dispositionInfo->DeleteFile ? TRUE : FALSE;
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"  dispositionInfo->DeleteFile = %d\n", dispositionInfo->DeleteFile);
         eventInfo->Status = STATUS_SUCCESS;
      } 
      else if (status == -ERROR_DIR_NOT_EMPTY)
      {
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"  DispositionInfo status = STATUS_DIRECTORY_NOT_EMPTY\n");
         eventInfo->Status = STATUS_DIRECTORY_NOT_EMPTY;
      } 
      else if (status < 0)
      {
         DbgPrint(DokanInstance->DokanOperations->DebugOutString, L"  DispositionInfo status = STATUS_CANNOT_DELETE\n");
         eventInfo->Status = STATUS_CANNOT_DELETE;
      }

   } 
   else
   {
      if (status < 0)
      {
         int error = status * -1;
         eventInfo->Status = GetNTStatus(error);

      } 
      else
      {
         eventInfo->Status = STATUS_SUCCESS;

         // notice new file name to driver
         if (EventContext->SetFile.FileInformationClass == FileRenameInformation)
         {
            PDOKAN_RENAME_INFORMATION renameInfo = (PDOKAN_RENAME_INFORMATION)((PCHAR)EventContext + EventContext->SetFile.BufferOffset);
            eventInfo->BufferLength = renameInfo->FileNameLength;
            CopyMemory(eventInfo->Buffer, renameInfo->FileName, renameInfo->FileNameLength);
         }
      }
   }

   //DbgPrint("SetInfomation status = %d\n\n", status);

   SendEventInformation(Handle, eventInfo, sizeOfEventInfo, DokanInstance);
   delete(eventInfo);
}


