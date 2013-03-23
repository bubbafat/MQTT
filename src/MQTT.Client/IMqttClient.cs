using System.Net;
using MQTT.Domain;

namespace MQTT.Client
{
    public delegate void MessageReceivedCallback(object sender, ClientCommandEventArgs e);

    public interface IMqttClient : INetworkInterface
    {
        void Connect(IPEndPoint endpoint);
        event MessageReceivedCallback OnMessageReceived;
    }
}
