using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using NLog;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      #region Construction

      private DateTime ConnectedTime;

      // Used inside PORT_Command method
      IPEndPoint ClientEndPoint;
      internal string SessionID
      {
         get
         {
            return ConnectedTime.Ticks.ToString();
         }
      }
      NetworkStream ClientSocket;
      internal FTPUser ConnectedUser;

      protected bool UseUTF8 { get; set; }

      protected EndPoint LocalEndPoint { get; set; }
      protected bool abortReceived { get; set; }

      bool DataTransferEnabled;
      TcpListener DataListener;

      string Rename_FilePath;

      byte[] BufferData = new byte[1024];
      private int startOffset;

      internal FTPClientCommander(TcpClient newClientSocket)
      {
         // Disable Nagle algorithm. Most of the time single short strings get transferred over the control connection. 
         newClientSocket.NoDelay = false;
         LocalEndPoint = newClientSocket.Client.LocalEndPoint;
         ClientSocket = newClientSocket.GetStream();
         //SubItems[1].Text = ClientSocket.RemoteEndPoint.ToString();
         ConnectedTime = DateTime.Now;

         ConnectedUser = new FTPUser();
         //SubItems[3].Text = SubItems[4].Text = (CurrentUser.ConnectedTime = DateTime.Now).ToString("MMM dd, yyyy hh:mm:ss");

         // Report the client that the server is ready to serve.
         SendOnControlStream("220 FTP Ready");

         
         // Wait for the command to be sent by the client
         ClientSocket.BeginRead(BufferData, 0, BufferData.Length, CommandReceived, null);
         //ClientStream.BeginRead(BufferData, 0, BufferData.Length, new AsyncCallback(CommandReceived), null);
      }


      /// <summary>
      /// https://secure.wikimedia.org/wikipedia/en/wiki/List_of_FTP_server_return_codes
      /// 
      /// </summary>
      /// <param name="arg"></param>
      void CommandReceived(IAsyncResult arg)
      {
         #region Read and Parse commands
         int CommandSize = 0;
         if ((ClientSocket != null)
            && ClientSocket.CanRead
            )
         {
            try
            {
               CommandSize = ClientSocket.EndRead(arg);
            }
            catch
            {
            }
         }
         if (CommandSize == 0)
         {
            Disconnect();
            return;
         }


         // Wait for the next command to be sent by the client
         try
         {
            ClientSocket.BeginRead(BufferData, 0, BufferData.Length, CommandReceived, null);
         }
         catch
         {
            Disconnect();
         }

         string CommandText = Encoding.ASCII.GetString(BufferData, 0, CommandSize).TrimStart(' ');
         string CmdArguments = String.Empty, Command;
         int End;
         if ((End = CommandText.IndexOf(' ')) == -1)
            End = (CommandText = CommandText.Trim()).Length;
         else if (UseUTF8)
         {
            CmdArguments = Encoding.UTF8.GetString(BufferData, End, CommandSize - End);
         }
         else
         {
            CmdArguments = CommandText.Substring(End);
         }
         Command = CommandText.Substring(0, End).ToUpper();

         #endregion

         #region Execute Commands
         if (!String.IsNullOrEmpty(CmdArguments))
         {
            CmdArguments = CmdArguments.TrimStart(' ').Replace("\r\n", String.Empty);
         }
         bool CommandExecued = false;
         Log.Info(string.Format("Received Command: [{0}] with args: [{1}]", Command, CmdArguments));
         switch (Command)
         {
            case "USER":
               USER_Command(CmdArguments);
               CommandExecued = true;
               break;
            case "PASS":
               if (!PASS_Command(CmdArguments))
                  return;
               CommandExecued = true;
               break;
         }
         if (!CommandExecued)
         {
            if (ConnectedUser.IsAuthenticated)
            {
               // I looked into making this into a (static) dictionary fuction call thing
               // But it turns out that the IL code of the switch wil do this of more than five non contiguos items are in the list.
               // As these are strings, then they non contiguos and will not be "jmp tabled"
               switch (Command)
               {
                  case "CWD":
                     CWD_Command(CmdArguments);
                     break;
                  case "CDUP_Command":
                     CDUP_Command();
                     break;
                  case "QUIT":
                     QUIT_Command();
                     break;
                  case "PORT":
                     PORT_Command(CmdArguments);
                     break;
                  case "PASV":
                     PASV_Command();
                     break;
                  case "TYPE":
                     TYPE_Command(CmdArguments);
                     break;
                  case "RETR":
                     RETR_Command(CmdArguments);
                     break;
                  case "STOR":
                     STOR_Command(CmdArguments);
                     break;
                  case "APPE":
                     APPE_Command(CmdArguments);
                     break;
                  case "ACCT":
                     ACCT_Command(CmdArguments);
                     break;
                  case "ALLO":
                     ALLO_Command(CmdArguments);
                     break;
                  case "RNFR":
                     RNFR_Command(CmdArguments);
                     break;
                  case "RNTO":
                     RNTO_Command(CmdArguments);
                     break;
                  case "DELE":
                     DELE_Command(CmdArguments);
                     break;
                  case "RMD":
                     RMD_Command(CmdArguments);
                     break;
                  case "MKD":
                     MKD_Command(CmdArguments);
                     break;
                  case "MODE":
                     MODE_Command(CmdArguments);
                     break;
                  case "PWD":
                     PWD_Command();
                     break;
                  case "LIST":
                     LIST_Command(CmdArguments);
                     break;
                  case "NLST":
                     NLST_Command(CmdArguments);
                     break;
                  case "SYST":
                     SYST_Command();
                     break;
                  case "NOOP":
                     NOOP_Command();
                     break;
                  case "SITE":
                     SITE_Command(CmdArguments);
                     break;
                  case "SMNT":
                     SMNT_Command(CmdArguments);
                     break;
                  case "STAT":
                     STAT_Command(CmdArguments);
                     break;
                  case "STRU":
                     STRU_Command(CmdArguments);
                     break;
                  case "HELP":
                     HELP_Command(CmdArguments);
                     break;
                  case "REIN":
                     REIN_Command();
                     break;
                  case "STOU":
                     STOU_Command(CmdArguments);
                     break;
                  case "REST":
                     REST_Command(CmdArguments);
                     break;
                  case "ABOR":
                     ABOR_Command();
                     break;
#region Authentication
                  case "ADAT":
                     ADAT_Command(CmdArguments);
                     break;
                  case "AUTH":
                     AUTH_Command(CmdArguments);
                     break;
                  case "CCC":
                     CCC_Command(CmdArguments);
                     break;
                  case "CONF":
                     CONF_Command(CmdArguments);
                     break;
                  case "ENC":
                     ENC_Command(CmdArguments);
                     break;
                  case "MIC":
                     MIC_Command(CmdArguments);
                     break;
                  case "PBSZ":
                     PBSZ_Command(CmdArguments);
                     break;
                  case "PROT":
                     PROT_Command(CmdArguments);
                     break;
#endregion
#region New Commands
                  case "EPRT":
                     EPRT_Command(CmdArguments);
                     break;
                  case "EPSV":
                     EPSV_Command(CmdArguments);
                     break;
                  case "FEAT":
                     FEAT_Command();
                     break;
                  case "HASH":
                     HASH_Command(CmdArguments);
                     break;
                  case "LANG":
                     LANG_Command(CmdArguments);
                     break;
                  case "LPRT":
                     LPRT_Command(CmdArguments);
                     break;
                  case "LPSV":
                     LPSV_Command(CmdArguments);
                     break;
                  case "MDTM":
                     MDTM_Command(CmdArguments);
                     break;
                  case "MFCT":
                     MFCT_Command(CmdArguments);
                     break;
                  case "MFF":
                     MFF_Command(CmdArguments);
                     break;
                  case "MFMT":
                     MFMT_Command(CmdArguments);
                     break;
                  case "MLSD":
                     MLSD_Command(CmdArguments);
                     break;
                  case "MLST":
                     MLST_Command(CmdArguments);
                     break;
                  case "OPTS":
                     OPTS_Command(CmdArguments);
                     break;
                  case "SIZE":
                     SIZE_Command(CmdArguments);
                     break;
                  case "XCRC":
                     XCRC_Command(CmdArguments);
                     break;
                  case "XMD5":
                     XMD5_Command(CmdArguments);
                     break;
                  case "XSHA1":
                     XSHA1_Command(CmdArguments);
                     break;
                  case "XSHA256":
                     XSHA256_Command(CmdArguments);
                     break;
                  case "XSHA512":
                     XSHA512_Command(CmdArguments);
                     break;
#endregion
                  default:
                     SendOnControlStream("500 Unknown Command.");
                     break;

               }
            }
            else 
               SendOnControlStream("530 Access Denied! Authenticate first!");
         }
         #endregion
      }


      

      
      #endregion

      #region General Methods

      internal void Disconnect()
      {
         Log.Warn("Disconnect called");
         if ( ClientSocket != null)
         {
            ClientSocket.Flush();
            ClientSocket.Close(15);
         }
         ClientSocket = null;
         if (DataListener != null) 
            DataListener.Stop(); 
         DataListener = null;
         ClientEndPoint = null;
         ConnectedUser = null;

         BufferData = null;
         Rename_FilePath = null;
         ManagementLayer.FtpServer.FTPClients.Remove(this);
         GC.Collect();
      }

      void SendOnControlStream(string Data)
      {
         if (!String.IsNullOrEmpty(Data)) 
         try
         {
            Log.Info(Data);
            if ((ClientSocket != null)
               && ClientSocket.CanWrite
               )
            {
               ClientSocket.WriteInfoCRLN(Data);
            }
         }
         catch ( Exception ex )
         {
            Log.ErrorException("SendOnControlStream ", ex);
            Disconnect(); 
         }
      }

      string GetExactPath(string Path)
      {
         if (Path == null) 
            Path = String.Empty;

         string dir = Path.Replace("/", "\\");

         if (!dir.EndsWith("\\")) 
            dir += "\\";

         if (!Path.StartsWith("/")) 
            dir = ConnectedUser.CurrentWorkingDirectory + dir;

         ArrayList pathParts = new ArrayList();
         dir = dir.Replace("\\\\", "\\");
         string[] p = dir.Split('\\');
         pathParts.AddRange(p);

         for (int i = 0; i < pathParts.Count; i++)
         {
            if (pathParts[i].ToString() == "..")
            {
               if (i > 0)
               {
                  pathParts.RemoveAt(i - 1);
                  i--;
               }

               pathParts.RemoveAt(i);
               i--;
            }
         }

         return dir.Replace("\\\\", "\\");
      }

      NetworkStream GetDataSocket()
      {
         TcpClient DataSocket;
         try
         {
            if (DataTransferEnabled)
            {
               int Count = 0;
               while (!DataListener.Pending())
               {
                  Thread.Sleep(1000);
                  Count++;
                  // Time out after 30 seconds
                  if (Count > 29)
                  {
                     SendOnControlStream("425 Data Connection Timed out.");
                     return null;
                  }
               }

               DataSocket = DataListener.AcceptTcpClient();
               SendOnControlStream("125 Connected, Starting Data Transfer.");
            }
            else
            {
               SendOnControlStream("150 Connecting.");
               DataSocket = new TcpClient(ClientEndPoint);
            }
         }
         catch
         {
            SendOnControlStream("425 Can't open data connection.");
            return null;
         }
         finally
         {
            if (DataListener != null)
            {
               DataListener.Stop();
               DataListener = null;
               GC.Collect();
            }
         }

         DataTransferEnabled = false;
         DataSocket.LingerState = new LingerOption(true, 15);
         DataSocket.Client.DontFragment = false;
         return DataSocket.GetStream();
      }

      #endregion
   }
}