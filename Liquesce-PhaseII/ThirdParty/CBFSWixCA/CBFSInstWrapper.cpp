#include "StdAfx.h"
#include "CBFSInstWrapper.h"

// explicit 
CBFSInstWrapper::CBFSInstWrapper(LPCTSTR const cpszDllName)
   : CDLLWrapper( cpszDllName )
   ,m_lpfnInstall( NULL )
   ,m_lpfnUninstall( NULL )
   ,m_lpfnGetModuleStatus( NULL )
   ,m_lpfnInstallIcon( NULL )
   ,m_lpfnUninstallIcon( NULL )
{
}

//virtual 
CBFSInstWrapper::~CBFSInstWrapper()
{
}

// The call to 'is valid' will attempt to load all the wanted function pointers
// and then check if we had an error.
//virtual 
const bool CBFSInstWrapper::IsValid(void)
{
   return( GetFunctionPointer( m_lpfnInstall, STR(Install) )
      && GetFunctionPointer( m_lpfnUninstall, STR( Uninstall ) )
      && GetFunctionPointer( m_lpfnGetModuleStatus, STR( GetModuleStatus ) )
      && GetFunctionPointer( m_lpfnInstallIcon, STR( InstallIcon ) )
      && GetFunctionPointer( m_lpfnUninstallIcon, STR( UninstallIcon ) )
      && CDLLWrapper::IsValid() 
      );
}

BOOL CBFSInstWrapper::Install( IN LPCTSTR CabPathName, IN LPCTSTR ProductName, IN LPCTSTR PathToInstall, IN BOOL SupportPnP, IN DWORD ModulesToInstall, OUT LPDWORD RebootNeeded )
{
   if ( NULL == m_lpfnInstall ) 
      return( FALSE );
   else 
      return( m_lpfnInstall( CabPathName, ProductName, PathToInstall, SupportPnP, ModulesToInstall, RebootNeeded ) );
}

BOOL CBFSInstWrapper::Uninstall( IN LPCTSTR CabPathName, IN LPCTSTR ProductName, IN LPCTSTR InstalledPath, OUT LPDWORD RebootNeeded )
{
   if ( NULL == m_lpfnUninstall ) 
      return( FALSE );
   else 
      return( m_lpfnUninstall( CabPathName, ProductName, InstalledPath, RebootNeeded ) );
}

BOOL CBFSInstWrapper::GetModuleStatus( IN LPCTSTR ProductName, IN DWORD Module, OUT LPBOOL Installed, OUT LPDWORD FileVersionHigh OPTIONAL, OUT LPDWORD FileVersionLow OPTIONAL )
{
   if ( NULL == m_lpfnGetModuleStatus ) 
      return( FALSE );
   else 
      return( m_lpfnGetModuleStatus( ProductName, Module, Installed, FileVersionHigh, FileVersionLow ) );
}

BOOL CBFSInstWrapper::InstallIcon( IN LPCTSTR IconPath, IN LPCTSTR IconId, OUT LPBOOL RebootNeeded )
{
   if ( NULL == m_lpfnInstallIcon ) 
      return( FALSE );
   else 
      return( m_lpfnInstallIcon( IconPath, IconId, RebootNeeded ) );
}

BOOL CBFSInstWrapper::UninstallIcon( IN LPCTSTR IconId, OUT LPBOOL RebootNeeded )
{
   if ( NULL == m_lpfnUninstallIcon ) 
      return( FALSE );
   else 
      return( m_lpfnUninstallIcon( IconId, RebootNeeded ) );
}
