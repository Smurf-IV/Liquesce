#pragma once

// Scoping class to ensure that the extracted resource is tidied up
class CExtractUtils
{
public:
   CExtractUtils(const HINSTANCE hInstance, const WORD & resourceID);
   virtual ~CExtractUtils();

   bool ExtractResource();

   TCHAR m_szOutputFilename[MAX_PATH+1];

private:
   bool CreateTempFile();
   const HINSTANCE m_hInstance;
   const WORD m_wResourceID;
};

