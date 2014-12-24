
#include "stdafx.h"

/*
hInst : A handle to the DLL module. The value is the base address of the DLL. 
The HINSTANCE of a DLL is the same as the HMODULE of the DLL, so hinstDLL 
can be used in calls to functions that require a module handle.
*/
HINSTANCE g_hInst = NULL;

// DllMain - Initialize and cleanup WiX custom action utils.
extern "C" BOOL WINAPI DllMain(
   __in HINSTANCE hInst,
   __in ULONG ulReason,
   __in LPVOID
   )
{
   switch(ulReason)
   {
   case DLL_PROCESS_ATTACH:
      g_hInst = hInst;
      WcaGlobalInitialize(hInst);
      break;

   case DLL_PROCESS_DETACH:
      g_hInst = NULL;
      WcaGlobalFinalize();
      break;
   }

   return TRUE;
}
