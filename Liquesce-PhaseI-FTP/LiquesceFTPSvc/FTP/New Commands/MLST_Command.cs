using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Win32;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: MLST [filename]
      /// same as MLSD, but retrieves listing for an individual file rather than a directory. 
      /// For directories, no filename is passed, retrieves their own attributes rather than a listing of their members. 
      /// MLST does not require a data connection, but returns a single line containing the listing for the requested path.
      /// http://rfc-ref.org/RFC-TEXTS/3659/chapter7.html
      /// </summary>
      /// <param name="cmdArguments"></param>
      private void MLST_Command(string cmdArguments)
      {
         FileSystemInfo info;
         string Path = ConnectedUser.StartUpDirectory + GetExactPath(cmdArguments);
         if (string.IsNullOrEmpty(cmdArguments))
         {
            info = new DirectoryInfo(Path);
         }
         else
         {
            Path = Path.Substring(0, Path.Length - 1);
            info = new FileInfo(Path); 
         }
         if (!ConnectedUser.CanViewHiddenFolders
            && ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            )
         {
            SendOnControlStream("550 Invalid path specified.");
         }
         else if (!info.Exists)
            SendOnControlStream("501 " + cmdArguments + " does not exist.");
         else
         {
            SendOnControlStream("250-Details for: [" + cmdArguments + "]");
            ClientSocket.WriteInfo(" ");
            ClientSocket.WriteInfo(((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
               ? SendDirectory(info)
               : SendFile(info));
            ClientSocket.WritePathNameCRLN(UseUTF8, info.Name);
            SendOnControlStream("250 Completed.");
         }
      }

      private string SendFile(FileSystemInfo fileInfo)
      {
         StringBuilder sb = new StringBuilder(@"Type=file;");
         sb.Append(@"size=").Append(((FileInfo)fileInfo).Length).Append(';');
         sb.Append(@"Modify=").Append(GetFormattedTime(fileInfo.LastWriteTimeUtc)).Append(';');
         sb.Append(@"Create=").Append(GetFormattedTime(fileInfo.CreationTimeUtc)).Append(';');
         sb.Append(@"Perm=");
         if ((fileInfo.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
         {
            if (ConnectedUser.CanStoreFiles)
            {
               sb.Append('w'); // that the STOR command may be applied to the object named
               sb.Append('a'); // Indicates that the APPE (append) command may be applied to the file named.
            }
            if (ConnectedUser.CanRenameFiles)
               sb.Append('f'); // Allow user to rename
         }
         sb.Append("r;"); // indicates that the RETR command may be applied to that object

         sb.Append(@"Unique=").Append(fileInfo.FullName.GetHashCode()).Append(';');
         sb.Append(@"Media-Type=").Append(MimeTypeDetector(fileInfo)).Append(';');
         sb.Append(@"Win32.ea=").AppendFormat("0x{0:x8}", (uint)fileInfo.Attributes).Append("; ");
         string buffer = sb.ToString();
         Log.Trace("SendFile: " + buffer);
         return buffer;
      }

      private static void MLST_Support(FTPClientCommander thisClient)
      {
         // Servers SHOULD, if conceivably possible,support at least the type, perm, size, unique, and modify facts.
         thisClient.SendOnControlStream(" MLST Type;Size;Modify;Create;Perm;Unique;Media-Type;Win32.ea;");
      }


      private static string MimeTypeDetector(FileSystemInfo fileInfo)
      {
         //MimeTypes g_MimeTypes = new MimeTypes("mime-types.xml");
         //sbyte[] fileData = null;
         //using (System.IO.FileStream srcFile =
         //    new System.IO.FileStream(strFile, System.IO.FileMode.Open))
         //{
         //   byte[] data = new byte[srcFile.Length];
         //   srcFile.Read(data, 0, (Int32)srcFile.Length);
         //   fileData = Winista.Mime.SupportUtil.ToSByteArray(data);
         //}
         //MimeType oMimeType = g_MimeTypes.GetMimeType(fileData);

         // This solution has a problem that it is limited to the softwares you have installed on the machine. 
         // For example, if PDF is not installed, then you cannot get the MIME type for ".pdf" using this technique. 
         string mimeType = String.Empty;
         string ext = fileInfo.Extension.ToLower();
         RegistryKey regKey = Registry.ClassesRoot.OpenSubKey(ext);
         if ((regKey != null)
            && (regKey.GetValue("Content Type") != null)
            )
         {
            mimeType = regKey.GetValue("Content Type").ToString();
         }
         if (String.IsNullOrEmpty(mimeType))
         {
            // TODO: Some of these can be removed (But which? As it has to support XP / Win2k3 / Win 7 / Win2k8 etc.)
            switch (ext)
            {
               case ".3dm": mimeType = "x-world/x-3dmf"; break;
               case ".3dmf": mimeType = "x-world/x-3dmf"; break;
               case ".a": mimeType = "application/octet-stream"; break;
               case ".aab": mimeType = "application/x-authorware-bin"; break;
               case ".aam": mimeType = "application/x-authorware-map"; break;
               case ".aas": mimeType = "application/x-authorware-seg"; break;
               case ".abc": mimeType = "text/vnd.abc"; break;
               case ".acgi": mimeType = "text/html"; break;
               case ".afl": mimeType = "video/animaflex"; break;
               case ".ai": mimeType = "application/postscript"; break;
               case ".aif": mimeType = "audio/aiff"; break;
               case ".aifc": mimeType = "audio/aiff"; break;
               case ".aiff": mimeType = "audio/aiff"; break;
               case ".aim": mimeType = "application/x-aim"; break;
               case ".aip": mimeType = "text/x-audiosoft-intra"; break;
               case ".ani": mimeType = "application/x-navi-animation"; break;
               case ".aos": mimeType = "application/x-nokia-9000-communicator-add-on-software"; break;
               case ".aps": mimeType = "application/mime"; break;
               case ".arc": mimeType = "application/octet-stream"; break;
               case ".arj": mimeType = "application/arj"; break;
               case ".art": mimeType = "image/x-jg"; break;
               case ".asf": mimeType = "video/x-ms-asf"; break;
               case ".asm": mimeType = "text/x-asm"; break;
               case ".asp": mimeType = "text/asp"; break;
               case ".asx": mimeType = "video/x-ms-asf"; break;
               case ".au": mimeType = "audio/basic"; break;
               case ".avi": mimeType = "video/avi"; break;
               case ".avs": mimeType = "video/avs-video"; break;
               case ".bcpio": mimeType = "application/x-bcpio"; break;
               case ".bin": mimeType = "application/octet-stream"; break;
               case ".bm": mimeType = "image/bmp"; break;
               case ".bmp": mimeType = "image/bmp"; break;
               case ".boo": mimeType = "application/book"; break;
               case ".book": mimeType = "application/book"; break;
               case ".boz": mimeType = "application/x-bzip2"; break;
               case ".bsh": mimeType = "application/x-bsh"; break;
               case ".bz": mimeType = "application/x-bzip"; break;
               case ".bz2": mimeType = "application/x-bzip2"; break;
               case ".c": mimeType = "text/plain"; break;
               case ".c++": mimeType = "text/plain"; break;
               case ".cat": mimeType = "application/vnd.ms-pki.seccat"; break;
               case ".cc": mimeType = "text/plain"; break;
               case ".ccad": mimeType = "application/clariscad"; break;
               case ".cco": mimeType = "application/x-cocoa"; break;
               case ".cdf": mimeType = "application/cdf"; break;
               case ".cer": mimeType = "application/pkix-cert"; break;
               case ".cha": mimeType = "application/x-chat"; break;
               case ".chat": mimeType = "application/x-chat"; break;
               case ".class": mimeType = "application/java"; break;
               case ".com": mimeType = "application/octet-stream"; break;
               case ".conf": mimeType = "text/plain"; break;
               case ".cpio": mimeType = "application/x-cpio"; break;
               case ".cpp": mimeType = "text/x-c"; break;
               case ".cpt": mimeType = "application/x-cpt"; break;
               case ".crl": mimeType = "application/pkcs-crl"; break;
               case ".crt": mimeType = "application/pkix-cert"; break;
               case ".csh": mimeType = "application/x-csh"; break;
               case ".css": mimeType = "text/css"; break;
               case ".cxx": mimeType = "text/plain"; break;
               case ".dcr": mimeType = "application/x-director"; break;
               case ".deepv": mimeType = "application/x-deepv"; break;
               case ".def": mimeType = "text/plain"; break;
               case ".der": mimeType = "application/x-x509-ca-cert"; break;
               case ".dif": mimeType = "video/x-dv"; break;
               case ".dir": mimeType = "application/x-director"; break;
               case ".dl": mimeType = "video/dl"; break;
               case ".doc": mimeType = "application/msword"; break;
               case ".dot": mimeType = "application/msword"; break;
               case ".dp": mimeType = "application/commonground"; break;
               case ".drw": mimeType = "application/drafting"; break;
               case ".dump": mimeType = "application/octet-stream"; break;
               case ".dv": mimeType = "video/x-dv"; break;
               case ".dvi": mimeType = "application/x-dvi"; break;
               case ".dwf": mimeType = "model/vnd.dwf"; break;
               case ".dwg": mimeType = "image/vnd.dwg"; break;
               case ".dxf": mimeType = "image/vnd.dwg"; break;
               case ".dxr": mimeType = "application/x-director"; break;
               case ".el": mimeType = "text/x-script.elisp"; break;
               case ".elc": mimeType = "application/x-elc"; break;
               case ".env": mimeType = "application/x-envoy"; break;
               case ".eps": mimeType = "application/postscript"; break;
               case ".es": mimeType = "application/x-esrehber"; break;
               case ".etx": mimeType = "text/x-setext"; break;
               case ".evy": mimeType = "application/envoy"; break;
               case ".exe": mimeType = "application/octet-stream"; break;
               case ".f": mimeType = "text/plain"; break;
               case ".f77": mimeType = "text/x-fortran"; break;
               case ".f90": mimeType = "text/plain"; break;
               case ".fdf": mimeType = "application/vnd.fdf"; break;
               case ".fif": mimeType = "image/fif"; break;
               case ".fli": mimeType = "video/fli"; break;
               case ".flo": mimeType = "image/florian"; break;
               case ".flx": mimeType = "text/vnd.fmi.flexstor"; break;
               case ".fmf": mimeType = "video/x-atomic3d-feature"; break;
               case ".for": mimeType = "text/x-fortran"; break;
               case ".fpx": mimeType = "image/vnd.fpx"; break;
               case ".frl": mimeType = "application/freeloader"; break;
               case ".funk": mimeType = "audio/make"; break;
               case ".g": mimeType = "text/plain"; break;
               case ".g3": mimeType = "image/g3fax"; break;
               case ".gif": mimeType = "image/gif"; break;
               case ".gl": mimeType = "video/gl"; break;
               case ".gsd": mimeType = "audio/x-gsm"; break;
               case ".gsm": mimeType = "audio/x-gsm"; break;
               case ".gsp": mimeType = "application/x-gsp"; break;
               case ".gss": mimeType = "application/x-gss"; break;
               case ".gtar": mimeType = "application/x-gtar"; break;
               case ".gz": mimeType = "application/x-gzip"; break;
               case ".gzip": mimeType = "application/x-gzip"; break;
               case ".h": mimeType = "text/plain"; break;
               case ".hdf": mimeType = "application/x-hdf"; break;
               case ".help": mimeType = "application/x-helpfile"; break;
               case ".hgl": mimeType = "application/vnd.hp-hpgl"; break;
               case ".hh": mimeType = "text/plain"; break;
               case ".hlb": mimeType = "text/x-script"; break;
               case ".hlp": mimeType = "application/hlp"; break;
               case ".hpg": mimeType = "application/vnd.hp-hpgl"; break;
               case ".hpgl": mimeType = "application/vnd.hp-hpgl"; break;
               case ".hqx": mimeType = "application/binhex"; break;
               case ".hta": mimeType = "application/hta"; break;
               case ".htc": mimeType = "text/x-component"; break;
               case ".htm": mimeType = "text/html"; break;
               case ".html": mimeType = "text/html"; break;
               case ".htmls": mimeType = "text/html"; break;
               case ".htt": mimeType = "text/webviewhtml"; break;
               case ".htx": mimeType = "text/html"; break;
               case ".ice": mimeType = "x-conference/x-cooltalk"; break;
               case ".ico": mimeType = "image/x-icon"; break;
               case ".idc": mimeType = "text/plain"; break;
               case ".ief": mimeType = "image/ief"; break;
               case ".iefs": mimeType = "image/ief"; break;
               case ".iges": mimeType = "application/iges"; break;
               case ".igs": mimeType = "application/iges"; break;
               case ".ima": mimeType = "application/x-ima"; break;
               case ".imap": mimeType = "application/x-httpd-imap"; break;
               case ".inf": mimeType = "application/inf"; break;
               case ".ins": mimeType = "application/x-internett-signup"; break;
               case ".ip": mimeType = "application/x-ip2"; break;
               case ".isu": mimeType = "video/x-isvideo"; break;
               case ".it": mimeType = "audio/it"; break;
               case ".iv": mimeType = "application/x-inventor"; break;
               case ".ivr": mimeType = "i-world/i-vrml"; break;
               case ".ivy": mimeType = "application/x-livescreen"; break;
               case ".jam": mimeType = "audio/x-jam"; break;
               case ".jav": mimeType = "text/plain"; break;
               case ".java": mimeType = "text/plain"; break;
               case ".jcm": mimeType = "application/x-java-commerce"; break;
               case ".jfif": mimeType = "image/jpeg"; break;
               case ".jfif-tbnl": mimeType = "image/jpeg"; break;
               case ".jpe": mimeType = "image/jpeg"; break;
               case ".jpeg": mimeType = "image/jpeg"; break;
               case ".jpg": mimeType = "image/jpeg"; break;
               case ".jps": mimeType = "image/x-jps"; break;
               case ".js": mimeType = "application/x-javascript"; break;
               case ".jut": mimeType = "image/jutvision"; break;
               case ".kar": mimeType = "audio/midi"; break;
               case ".ksh": mimeType = "application/x-ksh"; break;
               case ".la": mimeType = "audio/nspaudio"; break;
               case ".lam": mimeType = "audio/x-liveaudio"; break;
               case ".latex": mimeType = "application/x-latex"; break;
               case ".lha": mimeType = "application/octet-stream"; break;
               case ".lhx": mimeType = "application/octet-stream"; break;
               case ".list": mimeType = "text/plain"; break;
               case ".lma": mimeType = "audio/nspaudio"; break;
               case ".log": mimeType = "text/plain"; break;
               case ".lsp": mimeType = "application/x-lisp"; break;
               case ".lst": mimeType = "text/plain"; break;
               case ".lsx": mimeType = "text/x-la-asf"; break;
               case ".ltx": mimeType = "application/x-latex"; break;
               case ".lzh": mimeType = "application/octet-stream"; break;
               case ".lzx": mimeType = "application/octet-stream"; break;
               case ".m": mimeType = "text/plain"; break;
               case ".m1v": mimeType = "video/mpeg"; break;
               case ".m2a": mimeType = "audio/mpeg"; break;
               case ".m2v": mimeType = "video/mpeg"; break;
               case ".m3u": mimeType = "audio/x-mpequrl"; break;
               case ".man": mimeType = "application/x-troff-man"; break;
               case ".map": mimeType = "application/x-navimap"; break;
               case ".mar": mimeType = "text/plain"; break;
               case ".mbd": mimeType = "application/mbedlet"; break;
               case ".mc$": mimeType = "application/x-magic-cap-package-1.0"; break;
               case ".mcd": mimeType = "application/mcad"; break;
               case ".mcf": mimeType = "text/mcf"; break;
               case ".mcp": mimeType = "application/netmc"; break;
               case ".me": mimeType = "application/x-troff-me"; break;
               case ".mht": mimeType = "message/rfc822"; break;
               case ".mhtml": mimeType = "message/rfc822"; break;
               case ".mid": mimeType = "audio/midi"; break;
               case ".midi": mimeType = "audio/midi"; break;
               case ".mif": mimeType = "application/x-mif"; break;
               case ".mime": mimeType = "message/rfc822"; break;
               case ".mjf": mimeType = "audio/x-vnd.audioexplosion.mjuicemediafile"; break;
               case ".mjpg": mimeType = "video/x-motion-jpeg"; break;
               case ".mm": mimeType = "application/base64"; break;
               case ".mme": mimeType = "application/base64"; break;
               case ".mod": mimeType = "audio/mod"; break;
               case ".moov": mimeType = "video/quicktime"; break;
               case ".mov": mimeType = "video/quicktime"; break;
               case ".movie": mimeType = "video/x-sgi-movie"; break;
               case ".mp2": mimeType = "audio/mpeg"; break;
               case ".mp3": mimeType = "audio/mpeg"; break;
               case ".mpa": mimeType = "audio/mpeg"; break;
               case ".mpc": mimeType = "application/x-project"; break;
               case ".mpe": mimeType = "video/mpeg"; break;
               case ".mpeg": mimeType = "video/mpeg"; break;
               case ".mpg": mimeType = "video/mpeg"; break;
               case ".mpga": mimeType = "audio/mpeg"; break;
               case ".mpp": mimeType = "application/vnd.ms-project"; break;
               case ".mpt": mimeType = "application/vnd.ms-project"; break;
               case ".mpv": mimeType = "application/vnd.ms-project"; break;
               case ".mpx": mimeType = "application/vnd.ms-project"; break;
               case ".mrc": mimeType = "application/marc"; break;
               case ".ms": mimeType = "application/x-troff-ms"; break;
               case ".mv": mimeType = "video/x-sgi-movie"; break;
               case ".my": mimeType = "audio/make"; break;
               case ".mzz": mimeType = "application/x-vnd.audioexplosion.mzz"; break;
               case ".nap": mimeType = "image/naplps"; break;
               case ".naplps": mimeType = "image/naplps"; break;
               case ".nc": mimeType = "application/x-netcdf"; break;
               case ".ncm": mimeType = "application/vnd.nokia.configuration-message"; break;
               case ".nif": mimeType = "image/x-niff"; break;
               case ".niff": mimeType = "image/x-niff"; break;
               case ".nix": mimeType = "application/x-mix-transfer"; break;
               case ".nsc": mimeType = "application/x-conference"; break;
               case ".nvd": mimeType = "application/x-navidoc"; break;
               case ".o": mimeType = "application/octet-stream"; break;
               case ".oda": mimeType = "application/oda"; break;
               case ".omc": mimeType = "application/x-omc"; break;
               case ".omcd": mimeType = "application/x-omcdatamaker"; break;
               case ".omcr": mimeType = "application/x-omcregerator"; break;
               case ".p": mimeType = "text/x-pascal"; break;
               case ".p10": mimeType = "application/pkcs10"; break;
               case ".p12": mimeType = "application/pkcs-12"; break;
               case ".p7a": mimeType = "application/x-pkcs7-signature"; break;
               case ".p7c": mimeType = "application/pkcs7-mime"; break;
               case ".p7m": mimeType = "application/pkcs7-mime"; break;
               case ".p7r": mimeType = "application/x-pkcs7-certreqresp"; break;
               case ".p7s": mimeType = "application/pkcs7-signature"; break;
               case ".part": mimeType = "application/pro_eng"; break;
               case ".pas": mimeType = "text/pascal"; break;
               case ".pbm": mimeType = "image/x-portable-bitmap"; break;
               case ".pcl": mimeType = "application/vnd.hp-pcl"; break;
               case ".pct": mimeType = "image/x-pict"; break;
               case ".pcx": mimeType = "image/x-pcx"; break;
               case ".pdb": mimeType = "chemical/x-pdb"; break;
               case ".pdf": mimeType = "application/pdf"; break;
               case ".pfunk": mimeType = "audio/make"; break;
               case ".pgm": mimeType = "image/x-portable-greymap"; break;
               case ".pic": mimeType = "image/pict"; break;
               case ".pict": mimeType = "image/pict"; break;
               case ".pkg": mimeType = "application/x-newton-compatible-pkg"; break;
               case ".pko": mimeType = "application/vnd.ms-pki.pko"; break;
               case ".pl": mimeType = "text/plain"; break;
               case ".plx": mimeType = "application/x-pixclscript"; break;
               case ".pm": mimeType = "image/x-xpixmap"; break;
               case ".pm4": mimeType = "application/x-pagemaker"; break;
               case ".pm5": mimeType = "application/x-pagemaker"; break;
               case ".png": mimeType = "image/png"; break;
               case ".pnm": mimeType = "application/x-portable-anymap"; break;
               case ".pot": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".pov": mimeType = "model/x-pov"; break;
               case ".ppa": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".ppm": mimeType = "image/x-portable-pixmap"; break;
               case ".pps": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".ppt": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".ppz": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".pre": mimeType = "application/x-freelance"; break;
               case ".prt": mimeType = "application/pro_eng"; break;
               case ".ps": mimeType = "application/postscript"; break;
               case ".psd": mimeType = "application/octet-stream"; break;
               case ".pvu": mimeType = "paleovu/x-pv"; break;
               case ".pwz": mimeType = "application/vnd.ms-powerpoint"; break;
               case ".py": mimeType = "text/x-script.phyton"; break;
               case ".pyc": mimeType = "applicaiton/x-bytecode.python"; break;
               case ".qcp": mimeType = "audio/vnd.qcelp"; break;
               case ".qd3": mimeType = "x-world/x-3dmf"; break;
               case ".qd3d": mimeType = "x-world/x-3dmf"; break;
               case ".qif": mimeType = "image/x-quicktime"; break;
               case ".qt": mimeType = "video/quicktime"; break;
               case ".qtc": mimeType = "video/x-qtc"; break;
               case ".qti": mimeType = "image/x-quicktime"; break;
               case ".qtif": mimeType = "image/x-quicktime"; break;
               case ".ra": mimeType = "audio/x-pn-realaudio"; break;
               case ".ram": mimeType = "audio/x-pn-realaudio"; break;
               case ".ras": mimeType = "application/x-cmu-raster"; break;
               case ".rast": mimeType = "image/cmu-raster"; break;
               case ".rexx": mimeType = "text/x-script.rexx"; break;
               case ".rf": mimeType = "image/vnd.rn-realflash"; break;
               case ".rgb": mimeType = "image/x-rgb"; break;
               case ".rm": mimeType = "application/vnd.rn-realmedia"; break;
               case ".rmi": mimeType = "audio/mid"; break;
               case ".rmm": mimeType = "audio/x-pn-realaudio"; break;
               case ".rmp": mimeType = "audio/x-pn-realaudio"; break;
               case ".rng": mimeType = "application/ringing-tones"; break;
               case ".rnx": mimeType = "application/vnd.rn-realplayer"; break;
               case ".roff": mimeType = "application/x-troff"; break;
               case ".rp": mimeType = "image/vnd.rn-realpix"; break;
               case ".rpm": mimeType = "audio/x-pn-realaudio-plugin"; break;
               case ".rt": mimeType = "text/richtext"; break;
               case ".rtf": mimeType = "text/richtext"; break;
               case ".rtx": mimeType = "text/richtext"; break;
               case ".rv": mimeType = "video/vnd.rn-realvideo"; break;
               case ".s": mimeType = "text/x-asm"; break;
               case ".s3m": mimeType = "audio/s3m"; break;
               case ".saveme": mimeType = "application/octet-stream"; break;
               case ".sbk": mimeType = "application/x-tbook"; break;
               case ".scm": mimeType = "application/x-lotusscreencam"; break;
               case ".sdml": mimeType = "text/plain"; break;
               case ".sdp": mimeType = "application/sdp"; break;
               case ".sdr": mimeType = "application/sounder"; break;
               case ".sea": mimeType = "application/sea"; break;
               case ".set": mimeType = "application/set"; break;
               case ".sgm": mimeType = "text/sgml"; break;
               case ".sgml": mimeType = "text/sgml"; break;
               case ".sh": mimeType = "application/x-sh"; break;
               case ".shar": mimeType = "application/x-shar"; break;
               case ".shtml": mimeType = "text/html"; break;
               case ".sid": mimeType = "audio/x-psid"; break;
               case ".sit": mimeType = "application/x-sit"; break;
               case ".skd": mimeType = "application/x-koan"; break;
               case ".skm": mimeType = "application/x-koan"; break;
               case ".skp": mimeType = "application/x-koan"; break;
               case ".skt": mimeType = "application/x-koan"; break;
               case ".sl": mimeType = "application/x-seelogo"; break;
               case ".smi": mimeType = "application/smil"; break;
               case ".smil": mimeType = "application/smil"; break;
               case ".snd": mimeType = "audio/basic"; break;
               case ".sol": mimeType = "application/solids"; break;
               case ".spc": mimeType = "text/x-speech"; break;
               case ".spl": mimeType = "application/futuresplash"; break;
               case ".spr": mimeType = "application/x-sprite"; break;
               case ".sprite": mimeType = "application/x-sprite"; break;
               case ".src": mimeType = "application/x-wais-source"; break;
               case ".ssi": mimeType = "text/x-server-parsed-html"; break;
               case ".ssm": mimeType = "application/streamingmedia"; break;
               case ".sst": mimeType = "application/vnd.ms-pki.certstore"; break;
               case ".step": mimeType = "application/step"; break;
               case ".stl": mimeType = "application/sla"; break;
               case ".stp": mimeType = "application/step"; break;
               case ".sv4cpio": mimeType = "application/x-sv4cpio"; break;
               case ".sv4crc": mimeType = "application/x-sv4crc"; break;
               case ".svf": mimeType = "image/vnd.dwg"; break;
               case ".svr": mimeType = "application/x-world"; break;
               case ".swf": mimeType = "application/x-shockwave-flash"; break;
               case ".t": mimeType = "application/x-troff"; break;
               case ".talk": mimeType = "text/x-speech"; break;
               case ".tar": mimeType = "application/x-tar"; break;
               case ".tbk": mimeType = "application/toolbook"; break;
               case ".tcl": mimeType = "application/x-tcl"; break;
               case ".tcsh": mimeType = "text/x-script.tcsh"; break;
               case ".tex": mimeType = "application/x-tex"; break;
               case ".texi": mimeType = "application/x-texinfo"; break;
               case ".texinfo": mimeType = "application/x-texinfo"; break;
               case ".text": mimeType = "text/plain"; break;
               case ".tgz": mimeType = "application/x-compressed"; break;
               case ".tif": mimeType = "image/tiff"; break;
               case ".tiff": mimeType = "image/tiff"; break;
               case ".tr": mimeType = "application/x-troff"; break;
               case ".tsi": mimeType = "audio/tsp-audio"; break;
               case ".tsp": mimeType = "application/dsptype"; break;
               case ".tsv": mimeType = "text/tab-separated-values"; break;
               case ".turbot": mimeType = "image/florian"; break;
               case ".txt": mimeType = "text/plain"; break;
               case ".uil": mimeType = "text/x-uil"; break;
               case ".uni": mimeType = "text/uri-list"; break;
               case ".unis": mimeType = "text/uri-list"; break;
               case ".unv": mimeType = "application/i-deas"; break;
               case ".uri": mimeType = "text/uri-list"; break;
               case ".uris": mimeType = "text/uri-list"; break;
               case ".ustar": mimeType = "application/x-ustar"; break;
               case ".uu": mimeType = "application/octet-stream"; break;
               case ".uue": mimeType = "text/x-uuencode"; break;
               case ".vcd": mimeType = "application/x-cdlink"; break;
               case ".vcs": mimeType = "text/x-vcalendar"; break;
               case ".vda": mimeType = "application/vda"; break;
               case ".vdo": mimeType = "video/vdo"; break;
               case ".vew": mimeType = "application/groupwise"; break;
               case ".viv": mimeType = "video/vivo"; break;
               case ".vivo": mimeType = "video/vivo"; break;
               case ".vmd": mimeType = "application/vocaltec-media-desc"; break;
               case ".vmf": mimeType = "application/vocaltec-media-file"; break;
               case ".voc": mimeType = "audio/voc"; break;
               case ".vos": mimeType = "video/vosaic"; break;
               case ".vox": mimeType = "audio/voxware"; break;
               case ".vqe": mimeType = "audio/x-twinvq-plugin"; break;
               case ".vqf": mimeType = "audio/x-twinvq"; break;
               case ".vql": mimeType = "audio/x-twinvq-plugin"; break;
               case ".vrml": mimeType = "application/x-vrml"; break;
               case ".vrt": mimeType = "x-world/x-vrt"; break;
               case ".vsd": mimeType = "application/x-visio"; break;
               case ".vst": mimeType = "application/x-visio"; break;
               case ".vsw": mimeType = "application/x-visio"; break;
               case ".w60": mimeType = "application/wordperfect6.0"; break;
               case ".w61": mimeType = "application/wordperfect6.1"; break;
               case ".w6w": mimeType = "application/msword"; break;
               case ".wav": mimeType = "audio/wav"; break;
               case ".wb1": mimeType = "application/x-qpro"; break;
               case ".wbmp": mimeType = "image/vnd.wap.wbmp"; break;
               case ".web": mimeType = "application/vnd.xara"; break;
               case ".wiz": mimeType = "application/msword"; break;
               case ".wk1": mimeType = "application/x-123"; break;
               case ".wmf": mimeType = "windows/metafile"; break;
               case ".wml": mimeType = "text/vnd.wap.wml"; break;
               case ".wmlc": mimeType = "application/vnd.wap.wmlc"; break;
               case ".wmls": mimeType = "text/vnd.wap.wmlscript"; break;
               case ".wmlsc": mimeType = "application/vnd.wap.wmlscriptc"; break;
               case ".word": mimeType = "application/msword"; break;
               case ".wp": mimeType = "application/wordperfect"; break;
               case ".wp5": mimeType = "application/wordperfect"; break;
               case ".wp6": mimeType = "application/wordperfect"; break;
               case ".wpd": mimeType = "application/wordperfect"; break;
               case ".wq1": mimeType = "application/x-lotus"; break;
               case ".wri": mimeType = "application/mswrite"; break;
               case ".wrl": mimeType = "application/x-world"; break;
               case ".wrz": mimeType = "x-world/x-vrml"; break;
               case ".wsc": mimeType = "text/scriplet"; break;
               case ".wsrc": mimeType = "application/x-wais-source"; break;
               case ".wtk": mimeType = "application/x-wintalk"; break;
               case ".xbm": mimeType = "image/x-xbitmap"; break;
               case ".xdr": mimeType = "video/x-amt-demorun"; break;
               case ".xgz": mimeType = "xgl/drawing"; break;
               case ".xif": mimeType = "image/vnd.xiff"; break;
               case ".xl": mimeType = "application/excel"; break;
               case ".xla": mimeType = "application/vnd.ms-excel"; break;
               case ".xlb": mimeType = "application/vnd.ms-excel"; break;
               case ".xlc": mimeType = "application/vnd.ms-excel"; break;
               case ".xld": mimeType = "application/vnd.ms-excel"; break;
               case ".xlk": mimeType = "application/vnd.ms-excel"; break;
               case ".xll": mimeType = "application/vnd.ms-excel"; break;
               case ".xlm": mimeType = "application/vnd.ms-excel"; break;
               case ".xls": mimeType = "application/vnd.ms-excel"; break;
               case ".xlt": mimeType = "application/vnd.ms-excel"; break;
               case ".xlv": mimeType = "application/vnd.ms-excel"; break;
               case ".xlw": mimeType = "application/vnd.ms-excel"; break;
               case ".xm": mimeType = "audio/xm"; break;
               case ".xml": mimeType = "application/xml"; break;
               case ".xmz": mimeType = "xgl/movie"; break;
               case ".xpix": mimeType = "application/x-vnd.ls-xpix"; break;
               case ".xpm": mimeType = "image/xpm"; break;
               case ".x-png": mimeType = "image/png"; break;
               case ".xsr": mimeType = "video/x-amt-showrun"; break;
               case ".xwd": mimeType = "image/x-xwd"; break;
               case ".xyz": mimeType = "chemical/x-pdb"; break;
               case ".z": mimeType = "application/x-compressed"; break;
               case ".zip": mimeType = "application/zip"; break;
               case ".zoo": mimeType = "application/octet-stream"; break;
               case ".zsh": mimeType = "text/x-script.zsh"; break;
               default: mimeType = "application/octet-stream"; break;
            }
         }
         return mimeType;
      }
   }
}
