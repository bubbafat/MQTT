using System.Net;
using System.Threading.Tasks;
using MQTT.Domain;

namespace MQTT.Client
{
    public delegate void MessageReceivedCallback(object sender, ClientCommandEventArgs e);

    public interface IMqttClient : INetworkInterface
    {
        Task Connect(IPEndPoint endpoint);
        void Receive();
        event MessageReceivedCallback OnMessageReceived;
    }
}
