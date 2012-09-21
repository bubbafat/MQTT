using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace MQTT.Broker.Network
{
    internal class NetworkConnection
    {
        TcpClient _client;

        public NetworkConnection(TcpClient client)
        {
            _client = client;
            Stream = _client.GetStream();
        }

        public Stream Stream { get; private set; }

        internal void Disconnect()
        {
            _client.Close();
        }
    }
}
