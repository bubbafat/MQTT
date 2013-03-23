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

    public interface IMqttBroker : INetworkInterface
    {
        void Connect(IPEndPoint endpoint);
        event MessageReceivedCallback OnMessageReceived;
    }
}
