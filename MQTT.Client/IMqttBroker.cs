
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using System.Net;
using MQTT.Types;

namespace MQTT.Client
{
    public delegate void MessageReceivedCallback(object sender, ClientCommandEventArgs e);

    public interface IMqttBroker : IDisposable
    {
        void Connect(IPEndPoint endpoint);
        void Disconnect();

        Task Send(ClientCommand command);

        event MessageReceivedCallback OnMessageReceived;

        bool IsConnected { get; }
    }
}
