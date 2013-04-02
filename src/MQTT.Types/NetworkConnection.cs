using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQTT.Types
{
    public class NetworkConnection
    {
        readonly TcpClient _client;
        
        public NetworkConnection()
            : this(new TcpClient())
        {
        }

        public NetworkConnection(TcpClient client)
        {
            _client = client;
        }


        public Task Connect(IPEndPoint endpoint)
        {
            return _client.ConnectAsync(endpoint.Address, endpoint.Port);
        }

        public Task Write(byte[] bytes)
        {
            return Write(bytes, 0, bytes.Length);
        }

        public Task Write(byte[] bytes, int offset, int length)
        {
            return Stream.WriteAsync(bytes, offset, length);
        }

        public void Flush()
        {
            Stream.Flush();
        }

        public Task<byte[]> ReadBytesOrFailAsync(int length)
        {
            return Stream.ReadBytesOrFailAsync(length);
        }

        public void Disconnect()
        {
            _client.Close();
        }

        public bool Connected
        {
            get
            {
                return _client.Connected;
            }
        }

        private NetworkStream Stream
        {
            get { return _client.GetStream(); }
        }
    }
}
