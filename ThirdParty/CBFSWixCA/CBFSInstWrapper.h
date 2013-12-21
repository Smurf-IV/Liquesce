#pragma once
//////////////////////////////////////////////////////////////////////////////

#ifndef STDDLLAPIPTR_
#define STDDLLAPIPTR_( ret_type, PTRName ) typedef ret_type (* PTRName)
#endif //

//////////////////////////////////////////////////////////////////////////////

STDDLLAPIPTR_( BOOL, InstallAFN)( IN LPCSTR CabPathName, IN LPCSTR ProductName, IN LPCWSTR PathToInstall, IN BOOL SupportPnP, IN DWORD ModulesToInstall, OUT LPDWORD RebootNeeded );
STDDLLAPIPTR_( BOOL, InstallWFN)( IN LPCWSTR CabPathName, IN LPCWSTR ProductName, IN LPCWSTR PathToInstall, IN BOOL SupportPnP, IN DWORD ModulesToInstall, OUT LPDWORD RebootNeeded );
 
STDDLLAPIPTR_( BOOL, UninstallAFN)( IN LPCSTR CabPathName, IN LPCSTR ProductName, IN LPCWSTR InstalledPath, OUT LPDWORD RebootNeeded );
STDDLLAPIPTR_( BOOL, UninstallWFN)( IN LPCWSTR CabPathName, IN LPCWSTR ProductName, IN LPCWSTR InstalledPath, OUT LPDWORD RebootNeeded );

STDDLLAPIPTR_( BOOL, GetModuleStatusAFN)( IN LPCSTR ProductName, IN DWORD Module, OUT LPBOOL Installed, OUT LPDWORD FileVersionHigh OPTIONAL, OUT LPDWORD FileVersionLow OPTIONAL );
STDDLLAPIPTR_( BOOL, GetModuleStatusWFN)( IN LPCWSTR ProductName, IN DWORD Module, OUT LPBOOL Installed, OUT LPDWORD FileVersionHigh OPTIONAL, OUT LPDWORD FileVersionLow OPTIONAL );

STDDLLAPIPTR_( BOOL, InstallIconAFN)( IN LPCSTR ProductName, IN LPCSTR IconPath, IN LPCSTR IconId, OUT LPBOOL RebootNeeded );
STDDLLAPIPTR_( BOOL, InstallIconWFN)( IN LPCWSTR ProductName, IN LPCWSTR IconPath, IN LPCWSTR IconId, OUT LPBOOL RebootNeeded );

STDDLLAPIPTR_( BOOL, UninstallIconAFN)( IN LPCSTR ProductName, IN LPCSTR IconId, OUT LPBOOL RebootNeeded );
STDDLLAPIPTR_( BOOL, UninstallIconWFN)( IN LPCWSTR ProductName, IN LPCWSTR IconId, OUT LPBOOL RebootNeeded );

#ifdef _UNICODE
#define Install         InstallW
#define Uninstall       UninstallW
#define GetModuleStatus GetModuleStatusW
#define InstallIcon     InstallIconW
#define UninstallIcon   UninstallIconW
#else
#define Install         InstallA
#define Uninstall       UninstallA
#define GetModuleStatus GetModuleStatusA
#define InstallIcon     InstallIconA
#define UninstallIcon   UninstallIconA
#endif 

// will need to use "double expansion" trick:
#define STR1(x)  #x
#define STR(x)  STR1(x)


//////////////////////////////////////////////////////////////////////////////
// Base class to handle the loading and unloading of dlls when
// necessary e.g. guaranteed to free the loaded library.
//
// also defines a template member function which generates appropriate
// instances of any wanted function pointers.
//
//*****************************************************************************
// IsValid() :	Call this function to ensure that the dll has successfully loaded
//*****************************************************************************
// GetErrorCode() : Call this function to find out why the dll is NOT valid
//*****************************************************************************
// asHinstance() : Operator overloading to allow you to use a 'CDll' as a HINSTANCE wherever
//   needed.
//*****************************************************************************

class CDLLWrapper
{
public:
   explicit CDLLWrapper(LPCTSTR const cpszName)
      : m_hrErrorNum(NO_ERROR)
      ,m_hInstance(NULL) 
   { 
      m_hInstance = LoadLibrary(cpszName);
      if ( NULL == m_hInstance )
         SetErrorCode( GetLastError() );
   }

   //   Virtual destructor - ensures the loaded dll is freed
   virtual ~CDLLWrapper(void)
   {
      if( NULL != m_hInstance )
      {
         FreeLibrary(m_hInstance);
         m_hInstance=NULL;
      }
   }
   virtual const bool IsValid( void )       { return( (m_hrErrorNum == NO_ERROR) ); };
   virtual const HRESULT GetErrorCode( void ) const { return( m_hrErrorNum ); };
   const HINSTANCE asHinstance( void )      { return( m_hInstance ); };

   // Allowed to perform cast on ordinal because this is 32 bit
   template <class T> bool GetFunctionPointer( T &rlpfnTFN, INT ciProcNameOrOrdinal )
   { return( GetFunctionPointer( rlpfnTFN, reinterpret_cast<LPCSTR>(cpszProcNameOrOrdinal) ) ); };
   // This template function generates an appropriate function pointer accessor which
   // provides an appropriate function pointer as needed.
   template <class T> bool GetFunctionPointer( T &rlpfnTFN, LPCSTR cpszProcNameOrOrdinal )
   {	
      bool bGetFunctionPointer_OK( false );
      // have we successfully loaded the dll
      if ( NULL != asHinstance() )
      {	// try and get the function pointer (Allowed to perform cast on ordinal because this is 32 bit
         rlpfnTFN = reinterpret_cast<T>( GetProcAddress( m_hInstance, cpszProcNameOrOrdinal ) );
         // did we fail
         if ( NULL == rlpfnTFN )
            SetErrorCode( GetLastError() );
         else
            bGetFunctionPointer_OK = true;
      }
      else  // haven't managed to load the dll
         SetErrorCode(ERROR_INVALID_HANDLE);
      return( bGetFunctionPointer_OK );
   };

protected:
   // Convert Win32 error code into HResult wqe can use
   void SetErrorCode(CONST DWORD cdwError) { m_hrErrorNum = HRESULT_FROM_WIN32(static_cast<long>(cdwError)); };
   CDLLWrapper(void){/*Don't use constructor." );*/}	// force CDll to load a 'named' library (or fail trying)

private:
   HRESULT m_hrErrorNum;    // last error number encountered
   HINSTANCE m_hInstance;
};

//////////////////////////////////////////////////////////////////////////////

class CBFSInstWrapper : public CDLLWrapper
{
public:
   // Just provide the name of the dll to load
   explicit CBFSInstWrapper(LPCTSTR const cpszDllName);
   virtual ~CBFSInstWrapper();
    // The call to 'is valid' will attempt to load all the wanted function pointers
    // and then check if we had an error.
   virtual const bool IsValid(void);

   BOOL Install( IN LPCTSTR CabPathName, IN LPCTSTR ProductName, IN LPCTSTR PathToInstall, IN BOOL SupportPnP, IN DWORD ModulesToInstall, OUT LPDWORD RebootNeeded );
   BOOL Uninstall( IN LPCTSTR CabPathName, IN LPCTSTR ProductName, IN LPCTSTR InstalledPath, OUT LPDWORD RebootNeeded );
   BOOL GetModuleStatus( IN LPCTSTR ProductName, IN DWORD Module, OUT LPBOOL Installed, OUT LPDWORD FileVersionHigh OPTIONAL, OUT LPDWORD FileVersionLow OPTIONAL );
   BOOL InstallIcon( IN LPCTSTR ProductName, IN LPCTSTR IconPath, IN LPCTSTR IconId, OUT LPBOOL RebootNeeded );
   BOOL UninstallIcon( IN LPCTSTR ProductName, IN LPCTSTR IconId, OUT LPBOOL RebootNeeded );

private:
   public:
#ifdef _UNICODE
    InstallWFN         m_lpfnInstall;
    UninstallWFN       m_lpfnUninstall;
    GetModuleStatusWFN m_lpfnGetModuleStatus;
   InstallIconWFN     m_lpfnInstallIcon;
   UninstallIconWFN   m_lpfnUninstallIcon;
#else
   InstallAFN         m_lpfnInstall;
   UninstallAFN       m_lpfnUninstall;
   GetModuleStatusAFN m_lpfnGetModuleStatus;
   InstallIconAFN     m_lpfnInstallIcon;
   UninstallIconAFN   m_lpfnUninstallIcon;
#endif 

   // Disable Copy Constructor
   CBFSInstWrapper( const CBFSInstWrapper&  )
   {};
   // Disable assignment operator
   CBFSInstWrapper& operator=(const CBFSInstWrapper& rs_)
   {   if ( this != &rs_) {} return( *this ); };
};


//////////////////////////////////////////////////////////////////////////////