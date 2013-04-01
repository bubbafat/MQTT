using System;
using System.Net;
using MQTT.Commands;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MQTT.Domain
{
    public interface INetworkInterface : IDisposable
    {
        void Start(Action<MqttCommand> onIncomingMessage);

        Task Connect(IPEndPoint endpoint);
        void Disconnect();

        Task Send(MqttCommand command);

        bool IsConnected { get; }

        event NetworkDisconnectedCallback OnNetworkDisconnected;
    }
}
