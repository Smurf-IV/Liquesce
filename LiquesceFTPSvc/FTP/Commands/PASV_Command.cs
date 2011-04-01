using System.Net;
using System.Net.Sockets;

namespace LiquesceFTPSvc.FTP
{
   partial class FTPClientCommander
   {
      /// <summary>
      /// Syntax: PASV
      /// Tells the server to enter "passive mode". 
      /// In passive mode, the server will wait for the client to establish a connection with 
      /// it rather than attempting to connect to a client-specified port. The server will 
      /// respond with the address of the port it is listening on, with a message like:
      /// 227 Entering Passive Mode (a1,a2,a3,a4,p1,p2)
      /// where a1.a2.a3.a4 is the IP address and p1*256+p2 is the port number. 
      /// </summary>
      void PASV_Command()
      {
         // Open listener within the specified port range
         int tmpPort = Settings1.Default.MinPassvPort;
      StartListener:
         if (DataListener != null) 
         { 
            DataListener.Stop(); 
            DataListener = null; 
         }
         try
         {
            DataListener = new TcpListener(IPAddress.Any, tmpPort);
            DataListener.Start();
         }
         catch
         {
            if (tmpPort < Settings1.Default.MaxPassvPort)
            {
               tmpPort++;
               goto StartListener;
            }
            else
            {
               SendMessage("500 Action Failed Retry\r\n");
               return;
            }
         }

         //string tmpEndPoint = DataListener.LocalEndpoint.ToString();
         //tmpPort = Convert.ToInt32(tmpEndPoint.Substring(tmpEndPoint.IndexOf(':') + 1));

         if (ClientSocket != null)
         {
            string SocketEndPoint = ClientSocket.LocalEndPoint.ToString();
            SocketEndPoint = SocketEndPoint.Substring(0, SocketEndPoint.IndexOf(":")).Replace(".", ",") + "," + (tmpPort >> 8) + "," + (tmpPort & 255);
            DataTransferEnabled = true;

            SendMessage("227 Entering Passive Mode (" + SocketEndPoint + ").\r\n");
         }
      }
   }
}
