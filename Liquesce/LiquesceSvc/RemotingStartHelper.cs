using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Threading;
using System.Xml;
using NLog;

namespace LiquesceSvc
{
   /// <summary>
   /// 
   /// </summary>
   public static class RemotingStartHelper
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      /// <summary>
      /// Finds the next available port higher than the requested, iff requested in use
      /// </summary>
      /// <param name="requestedPort">.</param>
      /// <returns>Requested or next highest available</returns>
      private static int GetNextUnusedPort(int requestedPort)
      {
         List<int> usedPorts = new List<int>();
         bool portFound = false;
         foreach (TcpRow tcpRow in ManagedIpHelper.GetExtendedTcpTable(true))
         {
            usedPorts.Add(tcpRow.LocalEndPoint.Port);
            if (tcpRow.LocalEndPoint.Port == requestedPort)
            {
               portFound = true;
               Process p = Process.GetProcessById(tcpRow.ProcessId);
               Log.Info(p);
            }
         }
         if (portFound)
         {
            usedPorts.Sort();
            foreach (int usedPort in usedPorts)
            {
               if (usedPort == requestedPort)
               {
                  // Add 1 so that it can be checked on the next iteration
                  requestedPort++;
               }
               else if (usedPort > requestedPort)
               {
                  // Must have found a hole so stop
                  break;
               }
            }
         }
         return requestedPort;
      }

      /// <summary>
      /// Reads the configuration file and configures the remoting infrastructure.
      /// </summary>
      /// <param name="filename">The name of the remoting configuration file. Can be null</param>
      /// <param name="ensureSecurity">true to enable security; otherwise, false</param>
      /// <param name="instance">In order to inform SCM of delay to the start</param>
      public static void Configure(string filename, bool ensureSecurity, ServiceBase instance)
      {
         XmlDocument xmlDocument = new XmlDocument();
         xmlDocument.Load(filename);
         XmlNode xmlNode = xmlDocument.SelectSingleNode("//configuration/system.runtime.remoting/application/channels/channel[@ref='tcp']");
         int port = 0;
         if (xmlNode != null)
         {
            if (xmlNode.Attributes != null) 
               int.TryParse(xmlNode.Attributes["port"].Value, out port);
            else
            {
               port = 0;
            }
         }
         if (port > 0)
         {
            Log.Debug("===== IPPorts currently in use, Target Port required = {0}", port);
            try
            {
               foreach (TcpRow tcpRow in ManagedIpHelper.GetExtendedTcpTable(true))
               {
                  Log.Debug(tcpRow.LocalEndPoint.Port);
               }
            }
            catch (System.Exception e)
            {
               Log.WarnException("Cannot get the process information for the port in use", e);
            }

            bool portFound = true;
            const int sleep = 2000;
            int count = 40;   // * sleep sec is greater than short_max millisec for Default TCP timeout + plus some as 40 Secs is not enough !
            while (portFound
               && (--count > 0)
               )
            {
               try
               {
                  portFound = (port != GetNextUnusedPort(port));
                  if (portFound)
                     Thread.Sleep(sleep);
               }
               catch (Exception ex)
               {
                  portFound = true;
                  Log.WarnException("Failed to get port usage waiting", ex);
                  instance.RequestAdditionalTime(sleep * 5);
                  Thread.Sleep(sleep);
               }
            }

            if (count <= 0)
            {
               throw new InvalidProgramException("Port expected for remoting is still in use: " + port);
            }
            else
            {
               Log.Info(" * * * * 8 * * * 8 * * * 8: Had to wait for {0} seconds", (count - 40) * sleep / -1000);
               RemotingConfiguration.Configure(filename, ensureSecurity);
            }
         }
      }
   }
   #region Managed IP Helper API
   /// <summary>
   /// 
   /// </summary>
   public class TcpTable : IEnumerable<TcpRow>
   {
      #region Private Fields

      private IEnumerable<TcpRow> tcpRows;

      #endregion

      #region Constructors
      /// <summary>
      /// 
      /// </summary>
      /// <param name="tcpRows"></param>
      public TcpTable(IEnumerable<TcpRow> tcpRows)
      {
         this.tcpRows = tcpRows;
      }

      #endregion

      #region Public Properties
      /// <summary>
      /// 
      /// </summary>
      public IEnumerable<TcpRow> Rows
      {
         get { return this.tcpRows; }
      }

      #endregion

      #region IEnumerable<TcpRow> Members
      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      public IEnumerator<TcpRow> GetEnumerator()
      {
         return this.tcpRows.GetEnumerator();
      }

      #endregion

      #region IEnumerable Members
      /// <summary>
      /// 
      /// </summary>
      /// <returns></returns>
      IEnumerator IEnumerable.GetEnumerator()
      {
         return this.tcpRows.GetEnumerator();
      }

      #endregion

   }

   /// <summary>
   /// 
   /// </summary>
   public class TcpRow
   {
      #region Private Fields

      private readonly IPEndPoint localEndPoint;
      private readonly IPEndPoint remoteEndPoint;
      private readonly TcpState state;
      private readonly int processId;

      #endregion

      #region Constructors
      /// <summary>
      /// 
      /// </summary>
      /// <param name="tcpRow"></param>
      public TcpRow(IpHelper.TcpRow tcpRow)
      {
         state = tcpRow.state;
         processId = tcpRow.owningPid;

         int localPort = (tcpRow.localPort1 << 8) + (tcpRow.localPort2) + (tcpRow.localPort3 << 24) + (tcpRow.localPort4 << 16);
         localPort &= IPEndPoint.MaxPort;
         long localAddress = tcpRow.localAddr;
         localEndPoint = new IPEndPoint(localAddress, localPort);

         int remotePort = (tcpRow.remotePort1 << 8) + (tcpRow.remotePort2) + (tcpRow.remotePort3 << 24) + (tcpRow.remotePort4 << 16);
         remotePort &= IPEndPoint.MaxPort;
         long remoteAddress = tcpRow.remoteAddr;
         remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
      }

      #endregion

      #region Public Properties
      /// <summary>
      /// 
      /// </summary>
      public IPEndPoint LocalEndPoint
      {
         get { return localEndPoint; }
      }
      /// <summary>
      /// 
      /// </summary>
      public IPEndPoint RemoteEndPoint
      {
         get { return remoteEndPoint; }
      }
      /// <summary>
      /// 
      /// </summary>
      public TcpState State
      {
         get { return state; }
      }
      /// <summary>
      /// 
      /// </summary>
      public int ProcessId
      {
         get { return processId; }
      }

      #endregion
   }

   /// <summary>
   /// 
   /// </summary>
   public static class ManagedIpHelper
   {
      #region Public Methods
      /// <summary>
      /// 
      /// </summary>
      /// <param name="sorted"></param>
      /// <returns></returns>
      public static TcpTable GetExtendedTcpTable(bool sorted)
      {
         List<TcpRow> tcpRows = new List<TcpRow>();

         IntPtr tcpTable = IntPtr.Zero;
         int tcpTableLength = 0;

         if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) != 0)
         {
            try
            {
               tcpTable = Marshal.AllocHGlobal(tcpTableLength);
               if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, IpHelper.AfInet, IpHelper.TcpTableType.OwnerPidAll, 0) == 0)
               {
                  IpHelper.TcpTable table = (IpHelper.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(IpHelper.TcpTable));

                  IntPtr rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.Length));
                  for (int i = 0; i < table.Length; ++i)
                  {
                     tcpRows.Add(new TcpRow((IpHelper.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(IpHelper.TcpRow))));
                     rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(IpHelper.TcpRow)));
                  }
               }
            }
            finally
            {
               if (tcpTable != IntPtr.Zero)
               {
                  Marshal.FreeHGlobal(tcpTable);
               }
            }
         }

         return new TcpTable(tcpRows);
      }

      #endregion
   }

   #endregion

   #region P/Invoke IP Helper API

   /// <summary>
   /// For more details see: "http://msdn2.microsoft.com/en-us/library/aa366073.aspx"
   /// </summary>
   public static class IpHelper
   {
      #region Public Fields
      /// <summary>
      /// 
      /// </summary>
      private const string DllName = "iphlpapi.dll";
      /// <summary>
      /// 
      /// </summary>
      public const int AfInet = 2;  // #define AF_INET         2               /* internetwork: UDP, TCP, etc. */
      // AF_INET6


      #endregion

      #region Public Methods

      /// <summary>
      /// For more details see: "http://msdn2.microsoft.com/en-us/library/aa365928.aspx"
      /// </summary>
      [DllImport(DllName, SetLastError = true)]
      public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

      #endregion

      #region Public Enums

      /// <summary>
      /// For more details see: "http://msdn2.microsoft.com/en-us/library/aa366386.aspx"
      /// </summary>
      public enum TcpTableType
      {
         /// <summary>
         /// BasicListener member of TcpTableType.
         /// </summary>
         BasicListener,
         /// <summary>
         /// BasicConnections member of TcpTableType.
         /// </summary>
         BasicConnections,
         /// <summary>
         /// BasicAll member of TcpTableType.
         /// </summary>
         BasicAll,
         /// <summary>
         /// OwnerPidListener member of TcpTableType.
         /// </summary>
         OwnerPidListener,
         /// <summary>
         /// OwnerPidConnections member of TcpTableType.
         /// </summary>
         OwnerPidConnections,
         /// <summary>
         /// OwnerPidAll member of TcpTableType.
         /// </summary>
         OwnerPidAll,
         /// <summary>
         /// OwnerModuleListener member of TcpTableType.
         /// </summary>
         OwnerModuleListener,
         /// <summary>
         /// OwnerModuleConnections member of TcpTableType.
         /// </summary>
         OwnerModuleConnections,
         /// <summary>
         /// OwnerModuleAll member of TcpTableType.
         /// </summary>
         OwnerModuleAll,
      }

      #endregion

      #region Public Structs

      /// <summary>
      /// For more details see: "http://msdn2.microsoft.com/en-us/library/aa366921.aspx"
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct TcpTable
      {
         /// <summary>
         /// Length of TcpTable.
         /// </summary>
         public readonly uint Length;
         /// <summary>
         /// Row within TcpTable.
         /// </summary>
         private TcpRow row;
      }

      /// <summary>
      /// For more details see: "http://msdn2.microsoft.com/en-us/library/aa366913.aspx"
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct TcpRow
      {
         public readonly TcpState state;
         public readonly uint localAddr;
         public readonly byte localPort1;
         public readonly byte localPort2;
         public readonly byte localPort3;
         public readonly byte localPort4;
         public readonly uint remoteAddr;
         public readonly byte remotePort1;
         public readonly byte remotePort2;
         public readonly byte remotePort3;
         public readonly byte remotePort4;
         public readonly int owningPid;
      }

      #endregion
   }

   #endregion
}