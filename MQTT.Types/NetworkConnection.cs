using System.Net.Sockets;

namespace MQTT.Types
{
    public class NetworkConnection
    {
        readonly TcpClient _client;

        public NetworkConnection(TcpClient client)
        {
            _client = client;
            Stream = _client.GetStream();
        }

        public NetworkStream Stream { get; private set; }

        public void Disconnect()
        {
            _client.Close();
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
