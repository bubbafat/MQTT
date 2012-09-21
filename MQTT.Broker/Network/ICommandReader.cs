using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQTT.Broker.Network
{
    interface ICommandReader
    {
        Task<MqttCommand> ReadAsync(NetworkConnection connection);
        MqttCommand Read(NetworkConnection connection);
    }
}
