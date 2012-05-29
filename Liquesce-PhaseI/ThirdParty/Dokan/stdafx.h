// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#pragma warning( push, 3 )

#define VC_EXTRALEAN            // Exclude rarely-used stuff from Windows headers

#define WINVER 0x0502	      // Allow use of features specific to Windows Server 2003 with SP1, Windows XP with SP2 and later.
#define _WIN32_WINNT 0x0502	// Allow use of features specific to Windows Server 2003 with SP1, Windows XP with SP2 and later.
#define _WIN32_WINDOWS 0x0502 // Allow use of features specific to Windows Server 2003 with SP1, Windows XP with SP2 and later.
#define _WIN32_IE 0x0800

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS      // some CString constructors will be explicit

// turns off MFC's hiding of some common and often safely ignored warning messages
#define _AFX_ALL_WARNINGS

#ifdef VC_EXTRALEAN
#define NOSERVICE
#define NOMCX
#define NOIME
#define NOSOUND
#define NOCOMM
#define NORPC

#ifndef NO_ANSIUNI_ONLY
#ifdef _UNICODE
#define UNICODE_ONLY
#else
#define ANSI_ONLY
#endif
#endif //!NO_ANSIUNI_ONLY
#endif //VC_EXTRALEAN

// Don't include winsock.h
#define _WINSOCKAPI_
#define _AFX_NO_CTL3D_SUPPORT

#include <atldbgmem.h>

// SDKDDKVer.h will set any of WINVER, NTDDI_VERSION and _WIN32_IE that are yet unset.
#include <sdkddkver.h>

// Windows Header Files:
#include <windows.h>
#include <atltypes.h>
#include <tchar.h>
#include <WinSvc.h>
#include <winioctl.h>
#include <process.h>
#include <atlutil.h>
#include <atlbase.h>

#pragma warning( pop )
//////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////
//	Export helpers
#define EXPORT32 _declspec( dllexport )
#define IMPORT32 _declspec( dllimport )
#ifndef AFX_EXPORT
 #define AFX_EXPORT
#endif //

#ifdef __cplusplus
 extern "C" {
#endif	// be compatible with the definition in OLENLS.h

#ifndef EXTERN_C
  #ifdef __cplusplus     
   #define EXTERN_C    extern "C" 
  #else 
    #define EXTERN_C    extern 
 #endif
#endif 
#ifdef __cplusplus
 }
#endif
// Make the functions defined for export look a lot neater
#define STDDLLAPI_( ret_type ) EXTERN_C IMPORT32 ret_type AFX_EXPORT
#define STDDLLEXAPI_( ret_type ) EXTERN_C EXPORT32 ret_type AFX_EXPORT
#define STDDLLAPIPTR_( ret_type, PTRName ) typedef ret_type (AFX_EXPORT * PTRName)
#define STDAPIPTR_( ret_type, PTRName ) typedef ret_type (CALLBACK * PTRName)

VOID DokanDbgPrintW(LPCWSTR format, ...);
// DokanOptions->DebugMode is ON?
extern bool g_DebugMode;
// DokanOptions->UseStdErr is ON?
extern bool	g_UseStdErr;

#define DbgPrint(format, ... )  if (g_DebugMode) { DokanDbgPrintW(format, __VA_ARGS__); }


#ifndef _STRSAFE_H_INCLUDED_
// The MSDN Suggests that this file go after all the other #includes
// ms-help://MS.MSDNQTR.2004APR.1033/winui/winui/windowsuserinterface/resources/strings/usingstrsafefunctions.htm#cch_functions
#define STRSAFE_NO_CB_FUNCTIONS
#define STRSAFE_LIB
#include <StrSafe.h>
#endif
