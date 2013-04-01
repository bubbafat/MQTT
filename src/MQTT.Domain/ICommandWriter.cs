using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public interface ICommandWriter
    {
        Task Send(NetworkConnection connection, MqttCommand command);
    }
}
