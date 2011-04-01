using System;
using System.Net;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: PORT a1,a2,a3,a4,p1,p2
      /// Specifies the host and port to which the server should connect for the next file transfer. 
      /// This is interpreted as IP address a1.a2.a3.a4, port p1*256+p2. 
      /// </summary>
      /// <param name="CmdArguments"></param>
      void PORT_Command(string CmdArguments)
      {
         string[] IP_Parts = CmdArguments.Split(',');
         if (IP_Parts.Length != 6)
         {
            SendMessage("550 Invalid arguments.\r\n");
            return;
         }

         string ClientIP = IP_Parts[0] + "." + IP_Parts[1] + "." + IP_Parts[2] + "." + IP_Parts[3];
         int tmpPort = (Convert.ToInt32(IP_Parts[4]) << 8) | Convert.ToInt32(IP_Parts[5]);

         ClientEndPoint = new IPEndPoint(Dns.GetHostEntry(ClientIP).AddressList[0], tmpPort);

         DataTransferEnabled = false;

         SendMessage("200 Ready to connect to " + ClientIP + "\r\n");
      }
   }
}
