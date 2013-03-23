using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Domain
{
    public interface ICommandWriter
    {
        void Send(NetworkConnection connection, MqttCommand command);
    }
}
