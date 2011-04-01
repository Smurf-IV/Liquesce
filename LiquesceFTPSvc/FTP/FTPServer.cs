using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using LiquesceFacade;
using NLog;

namespace LiquesceFTPSvc.FTP
{
    internal class FTPServer
    {
       static private readonly Logger Log = LogManager.GetCurrentClassLogger();
       private readonly ConfigDetails currentConfigDetails;
       TcpListener FTPListener;

       internal readonly List<FTPClientCommander> FTPClients = new List<FTPClientCommander>();

       public FTPServer(ConfigDetails currentConfigDetails)
       {
          this.currentConfigDetails = currentConfigDetails;
       }

       internal bool IsRunning
        {
            get { return FTPListener != null; }
        }

        internal bool Start()
        {
            try
            {
                Stop();

                FTPListener = new TcpListener(IPAddress.Any, Settings1.Default.FtpPort);
                FTPListener.Start(20);

                // Start accepting the incoming clients.
                FTPListener.BeginAcceptSocket(NewFTPClientArrived, null);
                return true;
            }
            catch (Exception Ex)
            {
                Log.ErrorException("Start Failed", Ex);
            }
            return false;
        }

        internal void Stop()
        {
            if (FTPListener != null) 
               FTPListener.Stop(); 
           FTPListener = null;
        }

        void NewFTPClientArrived(IAsyncResult arg)
        {
            try
            {
                FTPClients.Add(new FTPClientCommander(FTPListener.EndAcceptSocket(arg)));
            }
            catch (Exception Ex)
            {
                Log.ErrorException("NewFTPClientArrived1", Ex);
            }

            try
            {
                // Start accepting the incoming clients.
                FTPListener.BeginAcceptSocket(NewFTPClientArrived, null);
            }
            catch (Exception Ex)
            {
                Log.ErrorException("NewFTPClientArrived2", Ex);
            }
        }
    }
}