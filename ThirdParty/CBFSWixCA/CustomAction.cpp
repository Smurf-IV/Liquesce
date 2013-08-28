#include "stdafx.h"
#include "resource.h"
#include "ExtractUtils.h"
#include "CBFSInstWrapper.h"

// Follow the steps in here
// https://blogs.technet.com/b/alexshev/archive/2009/05/15/from-msi-to-wix-part-22-dll-custom-actions-introduction.aspx?Redirected=true
//

// initialised fromthe DllMain
extern HINSTANCE g_hInst;


UINT __stdcall InstallCBFS_CA(MSIHANDLE hInstall)
{
   UINT er = ERROR_SUCCESS;
   int iModulesToInstall;
   LPWSTR pszwProductName = NULL;
   LPWSTR pwzData = NULL;

   HRESULT hr = WcaInitialize(hInstall, "InstallCBFS_CA");
   ExitOnFailure(hr, "Failed to initialize");
   {
      USES_CONVERSION;
      WcaLog(LOGMSG_STANDARD, "Initialized.");
      hr = WcaGetProperty( __TEXT("CustomActionData"), &pwzData);
      ExitOnFailure(hr, "failed to get CustomActionData");
      WcaLog(LOGMSG_STANDARD, "CustomActionData: %ls", pwzData); 

      pszwProductName = wcstok( pwzData, L";" );
      iModulesToInstall = _wtoi(wcstok( NULL, L"," ));
 
      if ( wcslen(pszwProductName) == 0 )
         WcaLogError(SCHED_E_NAMESPACE, "ProductName value is not set");
      else
         WcaLog(LOGMSG_STANDARD, "ProductName = %ls", pszwProductName);

      WcaLog(LOGMSG_STANDARD, "CBFSModulesToInstall = %i", iModulesToInstall);
      if ( iModulesToInstall == 0 )
         WcaLogError(SCHED_E_INVALIDVALUE, "CBFSModulesToInstall value is not set");

      // TODO: Add your custom action code here.
      WcaLog(LOGMSG_STANDARD, "CBFSCab(g_hInst,IDR_CBFS_CAB1)");
      CExtractUtils CBFSCab(g_hInst,IDR_CBFS_CAB1);
      if ( !CBFSCab.ExtractResource() )
      {
         ExitWithLastError(hr, "Failed CBFSCab.ExtractResource");
      }
      WcaLog(LOGMSG_STANDARD, "CBFSCab(g_hInst,IDR_CBFSINST_X32_DLL1)");
      {
         WcaLog(LOGMSG_STANDARD, "CBFSCab.m_szOutputFilename");
         WcaLog(LOGMSG_STANDARD, T2CA(CBFSCab.m_szOutputFilename) );
         CExtractUtils CBFSInstaller(g_hInst,IDR_CBFSINST_X32_DLL1);
         if ( !CBFSInstaller.ExtractResource() )
         {
            ExitWithLastError(hr, "Failed CBFSInstaller.ExtractResource");
         }
         {
            WcaLog(LOGMSG_STANDARD, "InstWrapper(CBFSInstaller.m_szOutputFilename)-> [%s]", T2CA(CBFSInstaller.m_szOutputFilename));
            CBFSInstWrapper InstWrapper(CBFSInstaller.m_szOutputFilename);
            if ( !InstWrapper.IsValid() )
            {
               WcaLog(LOGMSG_STANDARD, "Following list shows the functins that failed to be found:");
               if ( !InstWrapper.GetFunctionPointer( InstWrapper.m_lpfnInstall, STR(Install) ) )
                  WcaLog(LOGMSG_STANDARD, STR(Install));
               if ( !InstWrapper.GetFunctionPointer( InstWrapper.m_lpfnUninstall, STR( Uninstall ) ) )
                  WcaLog(LOGMSG_STANDARD, STR(Uninstall));
               if ( !InstWrapper.GetFunctionPointer( InstWrapper.m_lpfnGetModuleStatus, STR( GetModuleStatus ) ) )
                  WcaLog(LOGMSG_STANDARD, STR(GetModuleStatus));
               if ( !InstWrapper.GetFunctionPointer( InstWrapper.m_lpfnInstallIcon, STR( InstallIcon ) ) )
                  WcaLog(LOGMSG_STANDARD, STR(InstallIcon));
               if ( !InstWrapper.GetFunctionPointer( InstWrapper.m_lpfnUninstallIcon, STR( UninstallIcon ) ) )
                  WcaLog(LOGMSG_STANDARD, STR(UninstallIcon));
               if ( !InstWrapper.CDLLWrapper::IsValid()  )
                  WcaLog(LOGMSG_STANDARD, "CDLLWrapper::IsValid()");

               ExitOnFailure(hr=InstWrapper.GetErrorCode(), "Failed InstWrapper.IsValid");
            }
            {
               WcaLog(LOGMSG_STANDARD, "Call InstWrapper.Install(%s, %ls, TRUE, %i, out)", CBFSCab.m_szOutputFilename, pszwProductName,  iModulesToInstall);
               DWORD dwRebootNeeded(0UL);
               if ( FALSE == InstWrapper.Install(CBFSCab.m_szOutputFilename, W2CT(pszwProductName), NULL /* here you can put custom location if needed*/, TRUE, iModulesToInstall, &dwRebootNeeded) )
               {
                  ExitWithLastError(hr, "Failed InstWrapper.Install");
               }
               else if ( dwRebootNeeded != 0 )
                  WcaDeferredActionRequiresReboot();
               WcaLog(LOGMSG_STANDARD, "Success InstWrapper.Install");

               // If doing icons then possibly use
               // HRESULT WIXAPI WcaGetTargetPath( __in_z LPCWSTR wzFolder, __out LPWSTR* ppwzData );

               // Following brakets due to the macros and
               // http://msdn.microsoft.com/en-us/library/s6s80d9f%28v=vs.71%29.aspx
            }
         }
      }
   }
LExit:
   WcaLog(LOGMSG_STANDARD, "Out hr[%i]", hr);

   er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
   return WcaFinalize(er);
}


UINT __stdcall UninstallCBFS_CA(MSIHANDLE hInstall)
{
   UINT er = ERROR_SUCCESS;
   LPWSTR pszwProductName = NULL;

   HRESULT hr = WcaInitialize(hInstall, "UninstallCBFS_CA");
   ExitOnFailure(hr, "Failed to initialize");
   {
      USES_CONVERSION;
      WcaLog(LOGMSG_STANDARD, "Initialized.");
      hr = WcaGetProperty( __TEXT("CustomActionData"), &pszwProductName);
      ExitOnFailure(hr, "failed to get CustomActionData");
      WcaLog(LOGMSG_STANDARD, "CustomActionData: %ls", pszwProductName); 

      WcaLog(LOGMSG_STANDARD, "ProductName = %ls", pszwProductName);

      WcaLog(LOGMSG_STANDARD, "CBFSCab(g_hInst,IDR_CBFS_CAB1)");
      CExtractUtils CBFSCab(g_hInst,IDR_CBFS_CAB1);
      if ( !CBFSCab.ExtractResource() )
      {
         ExitWithLastError(hr, "Failed CBFSCab.ExtractResource");
      }
      {
         WcaLog(LOGMSG_STANDARD, "CBFSInstaller(g_hInst,IDR_CBFSINST_X32_DLL1)");
         CExtractUtils CBFSInstaller(g_hInst,IDR_CBFSINST_X32_DLL1);
         if ( !CBFSInstaller.ExtractResource() )
         {
            ExitWithLastError(hr, "Failed CBFSInstaller.ExtractResource");
         }
         {
            WcaLog(LOGMSG_STANDARD, "InstWrapper(CBFSInstaller.m_szOutputFilename)");
            CBFSInstWrapper InstWrapper(CBFSInstaller.m_szOutputFilename);
            if ( !InstWrapper.IsValid() )
            {
               ExitOnFailure(hr=InstWrapper.GetErrorCode(), "Failed InstWrapper.IsValid");
            }
            {
               WcaLog(LOGMSG_STANDARD, "CBFSCab(g_hInst,IDR_CBFS_CAB1)");
               DWORD dwRebootNeeded(0UL);

               if ( FALSE == InstWrapper.Uninstall(CBFSCab.m_szOutputFilename, W2CT(pszwProductName), NULL /* here you can put custom location if needed*/, &dwRebootNeeded) )
               {
                  ExitWithLastError(hr, "Failed InstWrapper.Install");
               }
               else if ( dwRebootNeeded != 0 )
                  WcaDeferredActionRequiresReboot();
               // Following brakets due to the macros and
               // http://msdn.microsoft.com/en-us/library/s6s80d9f%28v=vs.71%29.aspx
            }
         }
      }
   }
LExit:
   er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
   return WcaFinalize(er);
}