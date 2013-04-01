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
            _network.OnNetworkDisconnected += (sender, args) =>
            {
                var dis = OnNetworkDisconnected;
                if (dis != null)
                {
                    dis(this, args);
                }
            };
        }

        public Task Connect(System.Net.IPEndPoint endpoint)
        {
            return _network.Connect(endpoint);
        }

        public void Receive()
        {
            _network.Start(cmd =>
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
        public event NetworkDisconnectedCallback OnNetworkDisconnected;
        
        public void Dispose()
        {
            using (_network) { }
        }

        public void Start(Action<MqttCommand> onIncomingMessage)
        {
            _network.Start(onIncomingMessage);
        }
    }
}
