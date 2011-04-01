using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using System.Threading;
using LiquesceSvc;
using NLog;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      #region Construction

      private DateTime ConnectedTime;
      private DateTime LastInteraction;

      // Used inside PORT_Command method
      IPEndPoint ClientEndPoint = null;
      internal string SessionID
      {
         get
         {
            return ConnectedTime.Ticks.ToString();
         }
      }
      Socket ClientSocket;
      internal FTPUser ConnectedUser;

      internal string EndPoint
      {
         get
         {
            return ClientSocket.RemoteEndPoint.ToString();
         }
      }

      internal bool IsConnected
      {
         get
         {
            if ( (ClientSocket == null) 
               || !ClientSocket.Connected 
               || (ConnectedTime.ToString("HH:mm:ss") == LastInteraction.ToString("HH:mm:ss")) 
               )
            { 
               Disconnect(); 
               return false; 
            }
            return true;
         }
      }

      protected bool UseUTF8 { get; set; }

      bool DataTransferEnabled = false;
      TcpListener DataListener = null;

      string Rename_FilePath;

      byte[] BufferData = new byte[500];
      private int startOffset;

      internal FTPClientCommander(Socket ClientSocket)
      {
         this.ClientSocket = ClientSocket;
         //SubItems[1].Text = ClientSocket.RemoteEndPoint.ToString();
         ClientSocket.NoDelay = false;
         ConnectedTime = DateTime.Now;

         ConnectedUser = new FTPUser();
         //SubItems[3].Text = SubItems[4].Text = (CurrentUser.ConnectedTime = DateTime.Now).ToString("MMM dd, yyyy hh:mm:ss");

         // Report the client that the server is ready to serve.
         SendMessage("220 FTP Ready\r\n");

         // Wait for the command to be sent by the client
         ClientSocket.BeginReceive(BufferData, 0, BufferData.Length, SocketFlags.None, new AsyncCallback(CommandReceived), null);
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
            && ClientSocket.Connected
            )
         {
            try
            {
               CommandSize = ClientSocket.EndReceive(arg);
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
            ClientSocket.BeginReceive(BufferData, 0, BufferData.Length, SocketFlags.None, new AsyncCallback(CommandReceived), null);
         }
         catch
         {
            Disconnect();
         }

         LastInteraction = DateTime.Now;
         string CommandText = Encoding.ASCII.GetString(BufferData, 0, CommandSize).TrimStart(' ');
         string CmdArguments = null, Command = null;
         int End = 0;
         if ((End = CommandText.IndexOf(' ')) == -1)
            End = (CommandText = CommandText.Trim()).Length;
         else
            CmdArguments = CommandText.Substring(End).TrimStart(' ');
         Command = CommandText.Substring(0, End).ToUpper();

         #endregion

         #region Execute Commands
         if ((CmdArguments != null)
            && CmdArguments.EndsWith("\r\n")
            )
            CmdArguments = CmdArguments.Substring(0, CmdArguments.Length - 2);
         bool CommandExecued = false;
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
                     SendMessage("500 Unknown Command.\r\n");
                     break;

               }
            }
            else 
               SendMessage("530 Access Denied! Authenticate first\r\n");
         }
         #endregion
      }


      

      
      #endregion

      #region General Methods

      internal void Disconnect()
      {
         if ( (ClientSocket != null) 
            && ClientSocket.Connected
            ) 
            ClientSocket.Close(); 
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

      void SendMessage(string Data)
      {
         if (!String.IsNullOrEmpty(Data)) 
         try
         {
            if ((ClientSocket != null)
               && ClientSocket.Connected
               )
            {
               ClientSocket.Send(UseUTF8 ? Encoding.UTF8.GetBytes(Data) : Encoding.ASCII.GetBytes(Data));
            }
         }
         catch 
         { 
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

      Socket GetDataSocket()
      {
         Socket DataSocket = null;
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
                     SendMessage("425 Data Connection Timed out\r\n");
                     return null;
                  }
               }

               DataSocket = DataListener.AcceptSocket();
               SendMessage("125 Connected, Starting Data Transfer.\r\n");
            }
            else
            {
               SendMessage("150 Connecting.\r\n");
               DataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               DataSocket.Connect(ClientEndPoint);
            }
         }
         catch
         {
            SendMessage("425 Can't open data connection.\r\n");
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

         return DataSocket;
      }

      #endregion
   }
}