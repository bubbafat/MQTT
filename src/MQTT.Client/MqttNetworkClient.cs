using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Domain;

namespace MQTT.Client
{
    public sealed class MqttNetworkClient : IMqttClient
    {
        readonly INetworkInterface _network;

        public MqttNetworkClient(INetworkInterface network)
        {
            _network = network;
        }

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            var client = new TcpClient();
            client.Connect(endpoint);
            _network.Start(client, cmd =>
                {
                    var recv = OnMessageReceived;
                    if (recv != null)
                    {
                        recv(this, new ClientCommandEventArgs(cmd));
                    }
                });
        }

        public void Disconnect()
        {
            _network.Disconnect();
        }

        public Task Send(MqttCommand command)
        {
            return _network.Send(command);
        }

        public bool IsConnected
        {
            get
            {
                return _network.IsConnected;
            }
        }

        public event MessageReceivedCallback OnMessageReceived;

        public void Dispose()
        {
            using (_network) { }
        }

        public void Start(TcpClient client, Action<MqttCommand> onIncomingMessage)
        {
            _network.Start(client, onIncomingMessage);
        }
    }
}
