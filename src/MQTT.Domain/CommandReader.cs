using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class CommandReader : ICommandReader
    {
        MqttCommand ICommandReader.Read(NetworkConnection connection)
        {
            var header = FixedHeader.Load(connection);

            byte[] data = connection.ReadBytesOrFailAsync(header.RemainingLength).Await().Result;

            return MqttCommand.Create(header, data);
        }
    }
}
