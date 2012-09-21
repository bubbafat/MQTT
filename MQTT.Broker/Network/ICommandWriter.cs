using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Commands;
using System.Threading.Tasks;

namespace MQTT.Broker.Network
{
    interface ICommandWriter
    {
        Task SendAsync(NetworkConnection connection, MqttCommand command);
    }
}
