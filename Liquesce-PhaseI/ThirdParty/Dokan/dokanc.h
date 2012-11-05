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

#ifndef __DOKANC_H_
#define __DOKANC_H_

#ifndef __DOKAN_H_
#include "dokan.h"
#endif

#define DOKAN_MOUNT_POINT_SUPPORTED_VERSION 600
#define DOKAN_SECURITY_SUPPORTED_VERSION	600

#define DOKAN_GLOBAL_DEVICE_NAME	L"\\\\.\\Dokan"
#define DOKAN_CONTROL_PIPE			L"\\\\.\\pipe\\DokanMounter"

#define DOKAN_MOUNTER_SERVICE L"DokanMounter"
#define DOKAN_DRIVER_SERVICE L"Dokan"

#define DOKAN_CONTROL_MOUNT		1
#define DOKAN_CONTROL_UNMOUNT	2
#define DOKAN_CONTROL_CHECK		3
#define DOKAN_CONTROL_FIND		4
#define DOKAN_CONTROL_LIST		5

#define DOKAN_CONTROL_OPTION_FORCE_UNMOUNT 1

#define DOKAN_CONTROL_SUCCESS	1
#define DOKAN_CONTROL_FAIL		0

#define DOKAN_SERVICE_START		1
#define DOKAN_SERVICE_STOP		2
#define DOKAN_SERVICE_DELETE	3

#define DOKAN_KEEPALIVE_TIME	3000 // in milliseconds

#define DOKAN_MAX_THREAD		32


typedef struct _DOKAN_CONTROL 
{
   ULONG	Type;
   WCHAR	MountPoint[MAX_PATH];
   WCHAR	DeviceName[64];
   ULONG	Option;
   ULONG	Status;

} DOKAN_CONTROL, *PDOKAN_CONTROL;


STDDLLEXAPI_(BOOL ) DokanServiceInstall( LPCWSTR ServiceName, DWORD ServiceType, LPCWSTR ServiceFullPath);

STDDLLEXAPI_(BOOL ) DokanServiceDelete( LPCWSTR	ServiceName);

STDDLLEXAPI_(BOOL ) DokanNetworkProviderInstall();

STDDLLEXAPI_(BOOL ) DokanNetworkProviderUninstall();

STDDLLEXAPI_(BOOL ) DokanSetDebugMode(ULONG Mode);

STDDLLEXAPI_(BOOL ) DokanMountControl(PDOKAN_CONTROL Control);


#endif