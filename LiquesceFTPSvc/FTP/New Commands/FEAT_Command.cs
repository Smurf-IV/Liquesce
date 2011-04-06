namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      private delegate void FeaturePtr(FTPClientCommander thisClient);

      /// <summary>
      /// ftp://ftp.iana.org/assignments/ftp-commands-extensions
      /// </summary>
      private static readonly FeaturePtr[] features =
         {
            // TODO: Add in the Auth keys in here as well
            EPRT_Support
           ,EPSV_Support
           // ,FEAT_Support
           ,HASH_Support
           ,LANG_Support
           ,LPRT_Support
           ,LPSV_Support
           ,MDTM_Support
           ,MFCT_Support
           ,MFF_Support
           ,MFMT_Support
           ,MLSD_Support
           ,MLST_Support
           ,OPTS_Support
           ,REST_Support
           ,SIZE_Support
           ,XCRC_Support
           ,XMD5_Support
           ,XSHA1_Support
           ,XSHA256_Support
           ,XSHA512_Support
         };

      /// <summary>
      /// Syntax: 
      /// retrieves a listing of optional features supported by FTP server
      /// http://tools.ietf.org/html/rfc2389#page-4
      /// </summary>
      private void FEAT_Command()
      {
         SendOnControlStream("211-Extensions supported:");
         foreach (var feature in features)
         {
            feature(this);
         }
         // SendOnControlStream(" TVFS");  // Enable the Trivial Virtual File Structures for path traversal http://tools.ietf.org/html/rfc3659#page-18
         SendOnControlStream("211 END");
      }

   }
}
