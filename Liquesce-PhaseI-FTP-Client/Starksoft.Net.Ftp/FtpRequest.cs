/*
 *  Authors:  Benton Stark
 * 
 *  Copyright (c) 2007-2009 Starksoft, LLC (http://www.starksoft.com) 
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Starksoft.Net.Ftp
{
   /// <summary>
   /// FTP server commands.
   /// </summary>
   public enum FtpCmd
   {
      /// <summary>
      /// Unknown command issued.
      /// </summary>
      Unknown,
      /// <summary>
      /// The USER command.
      /// </summary>
      User,
      /// <summary>
      /// The PASS command.
      /// </summary>
      Pass,
      /// <summary>
      /// The MKD command.  Make new directory.
      /// </summary>
      Mkd,
      /// <summary>
      /// The RMD command.  Remove directory.
      /// </summary>
      Rmd,
      /// <summary>
      /// The RETR command.  Retrieve file.
      /// </summary>
      Retr,
      /// <summary>
      /// The PWD command.  Print working directory.
      /// </summary>
      Pwd,
      /// <summary>
      /// The SYST command.  System status.
      /// </summary>
      Syst,
      /// <summary>
      /// The CDUP command.  Change directory up.
      /// </summary>
      Cdup,
      /// <summary>
      /// The DELE command.  Delete file or directory.
      /// </summary>
      Dele,
      /// <summary>
      /// The TYPE command.  Transfer type.
      /// </summary>
      Type,
      /// <summary>
      /// The CWD command.  Change working directory.
      /// </summary>
      Cwd,
      /// <summary>
      /// The PORT command.  Data port.
      /// </summary>
      Port,
      /// <summary>
      /// The PASV command.  Passive port.
      /// </summary>
      Pasv,
      /// <summary>
      /// The STOR command.  Store file.
      /// </summary>
      Stor,
      /// <summary>
      /// The STOU command.  Store file unique.
      /// </summary>
      Stou,
      /// <summary>
      /// The APPE command.  Append file.
      /// </summary>
      Appe,
      /// <summary>
      /// The RNFR command.  Rename file from.
      /// </summary>
      Rnfr,
      /// <summary>
      /// The RFTO command.  Rename file to.
      /// </summary>
      Rnto,
      /// <summary>
      /// The ABOR command.  Abort current operation.
      /// </summary>
      Abor,
      /// <summary>
      /// The LIST command.  List files.
      /// </summary>
      List,
      /// <summary>
      /// The NLST command.  Namelist files.
      /// </summary>
      Nlst,
      /// <summary>
      /// The SITE command.  Site.
      /// </summary>
      Site,
      /// <summary>
      /// The STAT command.  Status.
      /// </summary>
      Stat,
      /// <summary> 
      /// The NOOP command.  No operation.
      /// </summary>
      Noop,
      /// <summary>
      /// The HELP command.  Help.
      /// </summary>
      Help,
      /// <summary>
      /// The ALLO command.  Allocate space.
      /// </summary>
      Allo,
      /// <summary>
      /// The QUIT command.  Quite session.
      /// </summary>
      Quit,
      /// <summary>
      /// The REST command.  Restart transfer.
      /// </summary>
      Rest,
      /// <summary>
      /// The AUTH command.  Initialize authentication.
      /// </summary>
      Auth,
      /// <summary>
      /// The PBSZ command.
      /// </summary>
      Pbsz,
      /// <summary>
      /// The PROT command.  Security protocol.
      /// </summary>
      Prot,
      /// <summary>
      /// The MODE command.  Data transfer mode.
      /// </summary>
      Mode,
      /// <summary>
      /// The MDTM command.  Month, date, and time.
      /// </summary>
      Mdtm,
      /// <summary>
      /// The SIZE command.  File size.
      /// </summary>
      Size,
      /// <summary>
      /// The FEAT command.  Supported features.
      /// </summary>
      Feat,
      /// <summary>
      /// The XCRC command.  CRC file integrity.
      /// </summary>
      Xcrc,
      /// <summary>
      /// The XMD5 command.  MD5 file integrity.
      /// </summary>
      Xmd5,
      /// <summary>
      /// The XSHA1 command.  SHA1 file integerity.
      /// </summary>
      Xsha1,
      /// <summary>
      /// The EPSV command.  
      /// </summary>
      Epsv,
      /// <summary>
      /// The ERPT command.
      /// </summary>
      Erpt,
      /// <summary>
      /// 
      /// </summary>
      Mfmt,
      /// <summary>
      /// 
      /// </summary>
      Mfct
   }

   /// <summary>
   /// FTP request object which contains the command, arguments and text or an FTP request.
   /// </summary>
   public class FtpRequest
   {
      private readonly byte[] _encodedCommand;
      private readonly FtpCmd _command;
      private readonly string[] _arguments;
      private FtpResponseCode[] happyCodes;

      /// <summary>
      /// Default constructor.
      /// </summary>
      public FtpRequest( FtpCmd command )
         : this(command, Encoding.ASCII)
      {
      }

      /// <summary>
      /// Override constuctor for simple commands
      /// </summary>
      /// <param name="command"></param>
      /// <param name="argument"></param>
      public FtpRequest(FtpCmd command, string argument)
         : this(command, Encoding.ASCII, argument)
      {
      }

      /// <summary>
      /// FTP request constructor.
      /// </summary>
      /// <param name="command">FTP request command.</param>
      /// <param name="encoding"></param>
      /// <param name="arguments">Parameters for the request</param>
      public FtpRequest(FtpCmd command, Encoding encoding, params string[] arguments)
      {
         _command = command;
         _arguments = arguments;
         _encodedCommand = BuildCommandText(command.ToString(), encoding, arguments);
      }


      /// <summary>
      /// FTP request constructor.
      /// </summary>
      /// <param name="preEncodedCommand">Pre-encoded command, usefule for MFCT style commands</param>
      public FtpRequest(byte[] preEncodedCommand)
      {
         _encodedCommand = preEncodedCommand;
      }

      /// <summary>
      /// Get the FTP command enumeration value.
      /// </summary>
      public FtpCmd Command
      {
         get { return _command; }
      }

      /// <summary>
      /// Get the FTP command arguments (if any).
      /// </summary>
      public List<string> Arguments
      {
         get
         {
            return new List<string>(_arguments);
         }
      }

      /// <summary>
      /// Get the FTP command text with any arguments.
      /// </summary>
      public byte[] EncodedCommand
      {
         get { return _encodedCommand; }
      }

      /// <summary>
      /// Gets a boolean value indicating if the command is a file transfer or not.
      /// </summary>
      public bool IsFileTransfer
      {
         get
         { // Check in statistical usage order for speed
            return ( (_command == FtpCmd.Retr)
               || (_command == FtpCmd.Stor)
               || (_command == FtpCmd.Stou)
               || (_command == FtpCmd.Appe)
               );
         }
      }

      ///<summary>
      /// Creates the buffer necessary to send to the Server that complies with the URF8 encoding of the Path / Filenames only.
      ///</summary>
      ///<param name="command"></param>
      ///<param name="encoding"></param>
      ///<param name="arguments"></param>
      ///<returns></returns>
      public static byte[] BuildCommandText(string command, Encoding encoding, IEnumerable<string> arguments)
      {
         string asciiCommand = command.ToUpper(CultureInfo.InvariantCulture);
         string args = string.Empty;

         if (arguments != null)
         {
            asciiCommand += ' ';
            StringBuilder builder = new StringBuilder();
            foreach (string arg in arguments)
            {
               builder.Append(arg).Append(' ');
            }
            args = builder.ToString().TrimEnd();
         }

         return Encoding.ASCII.GetBytes(asciiCommand).Concat(encoding.GetBytes(args)).ToArray();
      }

      internal bool HasHappyCodes
      {
         get { return HappyCodes.Length != 0; }
      }


      /// <summary>
      /// 
      /// </summary>
      public FtpResponseCode[] HappyCodes
      {
         get
         {
            if (happyCodes == null)
               BuildHappyCodes(_command);
            return happyCodes;
         }
         set { happyCodes = value; }
      }

      internal void BuildHappyCodes(FtpCmd command)
      {
         switch (command)
         {
            case FtpCmd.Unknown:
            case FtpCmd.Quit:
            case FtpCmd.Epsv:
            case FtpCmd.Erpt:
            case FtpCmd.Abor:
               happyCodes = BuildResponseArray();
               break;
            case FtpCmd.Allo:
               happyCodes = BuildResponseArray(FtpResponseCode.CommandOkay, FtpResponseCode.CommandNotImplementedSuperfluousAtThisSite); break;
            case FtpCmd.User:
               happyCodes = BuildResponseArray(FtpResponseCode.UserNameOkayButNeedPassword,
                  FtpResponseCode.ServiceReadyForNewUser,
                  FtpResponseCode.UserLoggedIn); break;
            case FtpCmd.Pass:
               happyCodes = BuildResponseArray(FtpResponseCode.UserLoggedIn,
                  FtpResponseCode.ServiceReadyForNewUser,
                  FtpResponseCode.NotLoggedIn);
               break;
            case FtpCmd.Cwd:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Pwd:
               happyCodes = BuildResponseArray(FtpResponseCode.PathNameCreated); break;
            case FtpCmd.Dele:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Mkd:
               happyCodes = BuildResponseArray(FtpResponseCode.PathNameCreated); break;
            case FtpCmd.Rmd:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Help:
               happyCodes = BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply,
                  FtpResponseCode.HelpMessage,
                  FtpResponseCode.FileStatus);
               break;
            case FtpCmd.Mdtm:
               happyCodes = BuildResponseArray(FtpResponseCode.FileStatus); break;
            case FtpCmd.Stat:
               happyCodes = BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply,
                  FtpResponseCode.DirectoryStatus,
                  FtpResponseCode.FileStatus);
               break;
            case FtpCmd.Cdup:
               happyCodes = BuildResponseArray(FtpResponseCode.CommandOkay, FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Size:
               happyCodes = BuildResponseArray(FtpResponseCode.FileStatus); break;
            case FtpCmd.Feat:
               happyCodes = BuildResponseArray(FtpResponseCode.SystemStatusOrHelpReply); break;
            case FtpCmd.Syst:
               happyCodes = BuildResponseArray(FtpResponseCode.NameSystemType); break;
            case FtpCmd.Rnfr:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionPendingFurtherInformation); break;
            case FtpCmd.Rnto:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Site:
               happyCodes = BuildResponseArray(FtpResponseCode.CommandOkay,
                  FtpResponseCode.CommandNotImplementedSuperfluousAtThisSite,
                  FtpResponseCode.RequestedFileActionOkayAndCompleted);
               break;
            case FtpCmd.Pasv:
               happyCodes = BuildResponseArray(FtpResponseCode.EnteringPassiveMode); break;
            case FtpCmd.Noop:
            case FtpCmd.Port:
            case FtpCmd.Type:
            case FtpCmd.Pbsz:
            case FtpCmd.Prot:
            case FtpCmd.Mode:
            case FtpCmd.Auth:
               happyCodes = BuildResponseArray(FtpResponseCode.CommandOkay); 
               break;
            case FtpCmd.Rest:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionPendingFurtherInformation); break;
            case FtpCmd.List:
            case FtpCmd.Nlst:
               happyCodes = BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting,
                           FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection,
                           FtpResponseCode.ClosingDataConnection,
                           FtpResponseCode.RequestedFileActionOkayAndCompleted);
               break;
            case FtpCmd.Appe:
            case FtpCmd.Stor:
            case FtpCmd.Stou:
            case FtpCmd.Retr:
               happyCodes = BuildResponseArray(FtpResponseCode.DataConnectionAlreadyOpenSoTransferStarting,
                           FtpResponseCode.FileStatusOkaySoAboutToOpenDataConnection,
                           FtpResponseCode.ClosingDataConnection,
                           FtpResponseCode.RequestedFileActionOkayAndCompleted);
               break;
            case FtpCmd.Xcrc:
            case FtpCmd.Xmd5:
            case FtpCmd.Xsha1:
               happyCodes = BuildResponseArray(FtpResponseCode.RequestedFileActionOkayAndCompleted); break;
            case FtpCmd.Mfmt:
            case FtpCmd.Mfct:
               happyCodes = BuildResponseArray(FtpResponseCode.FileStatus);
               break;

            default:
               throw new FtpException(String.Format("No response code(s) defined for FtpCmd {0}.", _command));
         }
      }

      ///<summary>
      ///</summary>
      ///<param name="codes"></param>
      static public FtpResponseCode[] BuildResponseArray(params FtpResponseCode[] codes)
      {
         return codes;
      }

   }
}
