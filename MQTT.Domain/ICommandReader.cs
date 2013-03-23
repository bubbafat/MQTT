using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public interface ICommandReader
    {
        MqttCommand Read(NetworkConnection connection);
    }
}
