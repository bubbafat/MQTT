using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace MQTT.Types
{
    public class NetworkConnection
    {
        TcpClient _client;

        public NetworkConnection(TcpClient client)
        {
            _client = client;
            Stream = _client.GetStream();
        }

        public NetworkStream Stream { get; private set; }

        public void Disconnect()
        {
            _client.Dispose();
        }

        public int Available
        {
            get
            {
                return _client.Available;
            }
        }

        public bool Connected
        {
            get
            {
                return _client.Connected;
            }
        }
    }
}
