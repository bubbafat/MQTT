using System;
using MQTT.Commands;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MQTT.Domain
{
    public interface INetworkInterface : IDisposable
    {
        void Start(TcpClient client, Action<MqttCommand> onIncomingMessage);

        void Disconnect();

        Task Send(MqttCommand command);

        bool IsConnected { get; }
    }
}
