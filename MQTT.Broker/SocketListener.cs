using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using MQTT.Commands;

namespace MQTT.Broker
{
    internal sealed class SocketListener
    {
        readonly Dictionary<Socket, ActiveConnection> _connections = new Dictionary<Socket, ActiveConnection>();
        readonly ReaderWriterLockSlim _connectionsLock = new ReaderWriterLockSlim();
        readonly Thread _pollingThread;

        public SocketListener()
        {
            _pollingThread = new Thread(PollingThread);
            _pollingThread.Start();
        }

        public void Register(ActiveConnection connection)
        {
            _connectionsLock.EnterWriteLock();
            try
            {
                _connections.Add(connection.Socket, connection);
            }
            finally
            {
                _connectionsLock.ExitWriteLock();
            }
        }

        public void Unregister(ActiveConnection connection)
        {
            _connectionsLock.EnterWriteLock();
            try
            {
                if (!_connections.Remove(connection.Socket))
                {
                    throw new InvalidOperationException(string.Format("Connection not found with client ID: {0}", connection.ClientId));
                }                
            }
            finally
            {
                _connectionsLock.ExitWriteLock();
            }
        }

        private void ProcessExpiredConnections()
        {
            throw new NotImplementedException();
        }

        private void PollingThread()
        {
            while (true)
            {
                _connectionsLock.EnterReadLock();
                try
                {
                    // remove and LWT expired connections
                    ProcessExpiredConnections();

                    IList dataReady = _connections.Values.Select(c => c.Socket).ToList();

                    bool pollManually = false;

                    if (dataReady.Count > 0)
                    {
                        try
                        {
                            Socket.Select(dataReady, null, null, 0);
                        }
                        catch (SocketException se)
                        {
                            pollManually = true;
                        }

                        if (pollManually)
                        {
                            dataReady = _connections.Values.Select(c => c.Socket).Where(s => s.Poll(0, SelectMode.SelectRead)).ToList();
                        }
                     
                        ProcessIncomingData(dataReady);
                    }
                }
                finally
                {
                    _connectionsLock.ExitReadLock();
                }
            }
        }

        private void ProcessIncomingData(IList dataReady)
        {
            foreach (Socket socket in dataReady)
            {
                ActiveConnection connection;
                if (_connections.TryGetValue(socket, out connection))
                {
                }
            }
        }
    }
}
