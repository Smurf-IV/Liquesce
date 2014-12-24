#include "StdAfx.h"
#include "ExtractUtils.h"


CExtractUtils::CExtractUtils(const HINSTANCE hInstance, const WORD & resourceID)
   : m_hInstance( hInstance )
   , m_wResourceID( resourceID )
//   , m_szOutputFilename()
{
   m_szOutputFilename[0] = NULL;
}

// virtual 
CExtractUtils::~CExtractUtils()
{
   // TODO: owner application will need to delete the file once it has finished with it (i.e. not when this class loses scope)
   //if ( m_szOutputFilename[0] != NULL )
   //   RemoveResource();
}


//
bool CExtractUtils::ExtractResource()
{
   bool bSuccess = CreateTempFile(); 
   if ( bSuccess )
      try
   {
      // First find and load the required resource
      HRSRC hResource = FindResource(m_hInstance, MAKEINTRESOURCE(m_wResourceID), TEXT("BIN"));
      HGLOBAL hFileResource = LoadResource(m_hInstance, hResource);

      // Now open and map this to a disk file
      LPBYTE lpFileData = (LPBYTE)LockResource(hFileResource);
      CONST DWORD cdwSize( SizeofResource(m_hInstance, hResource));

      // Open the file and filemap
      HANDLE hFile = CreateFile(m_szOutputFilename, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
      HANDLE hFilemap = CreateFileMapping(hFile, NULL, PAGE_READWRITE, 0, cdwSize, NULL);
      LPBYTE lpBaseAddress = (LPBYTE )MapViewOfFile(hFilemap, FILE_MAP_WRITE, 0, 0, 0);            

      // Write the file
      CopyMemory(lpBaseAddress, lpFileData, cdwSize);            

      // Unmap the file and close the handles
      UnmapViewOfFile(lpBaseAddress);

      CloseHandle(hFilemap);
      CloseHandle(hFile);
      bSuccess = true;
   }
   catch(...)
   {
      // Ignore all type of errors
      m_szOutputFilename[0] = NULL;
      bSuccess = false;
   } 
   return bSuccess;
}




bool CExtractUtils::CreateTempFile( )
{
   bool bSuccess = false; 
   try
   {
      TCHAR lpTempPathBuffer[MAX_PATH];
      //  Gets the temp path env string (no guarantee it's a valid path).
      DWORD dwRetVal = GetTempPath(MAX_PATH,          // length of the buffer
         lpTempPathBuffer); // buffer for path 
      if ( (dwRetVal > (MAX_PATH-14))
         || (dwRetVal == 0)
         )   
      {
      }
      else
      {
         //  Generates a temporary file name. 
         UINT uRetVal = GetTempFileName(lpTempPathBuffer, // directory for tmp files
            TEXT("Extract"),     // temp file name prefix 
            0,                // create unique name 
            m_szOutputFilename);  // buffer for name     
         bSuccess = (uRetVal != 0);
      }
   }
   catch(...)
   {
      // Ignore all type of errors
      bSuccess = false;
   } 
   return bSuccess;
}
