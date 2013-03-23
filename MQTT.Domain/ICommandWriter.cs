using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public interface ICommandWriter
    {
        void Send(NetworkConnection connection, MqttCommand command);
    }
}
