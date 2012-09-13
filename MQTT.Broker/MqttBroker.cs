using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Domain;
using System.Timers;
using MQTT.Domain.StateMachines;
using MQTT.Types;
using System.Net;
using MQTT.Commands;
using System.Net.Sockets;
using System.Threading;

namespace MQTT.Broker
{
    public sealed class MqttBroker : IDisposable
    {
        Socket _listenSocket;
        Thread _listenThread;

        public MqttBroker()
        {
        }

        public void Listen(IPEndPoint endpoint)
        {
            _listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(1024);

            _listenThread = new Thread(ListenLoop);
            _listenThread.Start();            
        }

        public void Dispose()
        {
            if (_listenThread != null)
            {
                _listenThread.Abort();
            }
        }

        private void ListenLoop()
        {
            while (true)
            {
                Socket connection = _listenSocket.Accept();
                if (connection.Connected)
                {
                    NetworkInterface net = new NetworkInterface(connection);
                    _activeConnections.Add(new ActiveConnection(net));
                }
            }
        }

        List<ActiveConnection> _activeConnections = new List<ActiveConnection>();
    }
}
