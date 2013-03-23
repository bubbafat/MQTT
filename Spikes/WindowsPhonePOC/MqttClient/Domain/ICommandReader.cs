using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Net.Sockets;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Domain
{
    public interface ICommandReader
    {
        MqttCommand Read(NetworkConnection connection);
    }
}
