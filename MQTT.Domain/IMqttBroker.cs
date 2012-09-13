using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using MQTT.Commands;

namespace MQTT.Domain
{
    public delegate void MessageReceivedCallback(object sender, ClientCommandEventArgs e);

    public interface IMqttBroker : IDisposable
    {
        void Connect(IPEndPoint endpoint);
        void Disconnect();

        Task Send(MqttCommand command);

        event MessageReceivedCallback OnMessageReceived;

        bool IsConnected { get; }
    }
}
