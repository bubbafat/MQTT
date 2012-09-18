using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.Diagnostics;

namespace MQTT.Broker
{
    internal class ConnectionManager
    {
        private List<Socket> _listeningList = new List<Socket>();

        private List<ActiveConnection> _connectionsInWaiting = new List<ActiveConnection>();
        private List<ActiveConnection> _connectionsInAction = new List<ActiveConnection>();
        private List<ActiveConnection> _nonValidatedConnections = new List<ActiveConnection>();

        public List<ActiveConnection> Select()
        {
            List<ActiveConnection> result = new List<ActiveConnection>();

            IList<Socket> newConnections = GetIncomingConnections();

            foreach(Socket s in newConnections)
            {
                ActiveConnection conn = new ActiveConnection(this, new Domain.NetworkInterface(s));
                _nonValidatedConnections.Add(conn);
                result.Add(conn);
            }

            IList<ActiveConnection> haveData = GetDataReadyConnections();

            foreach (ActiveConnection c in haveData)
            {
                _connectionsInAction.Add(c);
                result.Add(c);
            }

            return result;
        }

        public int ConnectionCount
        {
            get
            {
                return _connectionsInWaiting.Count + _connectionsInAction.Count;
            }
        }

        private IList<ActiveConnection> GetDataReadyConnections()
        {
            List<ActiveConnection> result = new List<ActiveConnection>();

            IList toCheck = _connectionsInWaiting.Select(a => a.Socket).ToList();

            if (toCheck.Count > 0)
            {
                Socket.Select(toCheck, null, null, 0);

                foreach (Socket ready in toCheck)
                {
                    ActiveConnection conn = _connectionsInWaiting.Where(a => a.Socket == ready).FirstOrDefault();
                    Debug.Assert(conn != null, "Why is there no connection when we just found it?");

                    if (conn != null)
                    {
                        result.Add(conn);
                        _connectionsInWaiting.Remove(conn);
                        _connectionsInAction.Add(conn);
                    }
                }
            }

            return result;
        }

        private IList<Socket> GetIncomingConnections()
        {
            List<Socket> result = new List<Socket>();

            while (result.Count < 100)
            {
                IList listeners = _listeningList.ToList();
                Socket.Select(listeners, null, null, 0);
                if (listeners.Count == 0)
                {
                    break;
                }

                foreach (Socket s in listeners)
                {
                    try
                    {
                        result.Add(s.Accept());
                    }
                    catch (SocketException)
                    {
                        // skip it
                    }
                }
            }

            return result;
        }

        internal void Start()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.IPv6Any, 1883);

            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

            // accept both ipv4 and ipv6 connections
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            _listeningList.Add(socket);

            foreach (Socket s in _listeningList)
            {
                s.Bind(endpoint);
                s.Listen(1024);
            }
        }

        internal void Stop()
        {
            foreach (Socket s in _listeningList)
            {
                try
                {
                    s.Close();
                    using (s) { }
                }
                catch (SocketException)
                {
                    // eat it
                }
            }

            _listeningList.Clear();
        }

        internal void Connected(ActiveConnection activeConnection)
        {
            _nonValidatedConnections.Remove(activeConnection);
            _connectionsInWaiting.Add(activeConnection);
        }

        internal void Disconnect(ActiveConnection activeConnection)
        {
            _connectionsInAction.Remove(activeConnection);
            _nonValidatedConnections.Remove(activeConnection);
            _connectionsInWaiting.Remove(activeConnection);
        }

        internal void Done(ActiveConnection activeConnection)
        {
            _connectionsInAction.Remove(activeConnection);
            _connectionsInWaiting.Add(activeConnection);
        }
    }
}
