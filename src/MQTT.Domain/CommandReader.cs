using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class CommandReader : ICommandReader
    {
        MqttCommand ICommandReader.Read(NetworkConnection connection)
        {
            var header = FixedHeader.Load(connection);

            byte[] data = connection.Stream.ReadBytesOrFailAsync(header.RemainingLength).Await().Result;

            return MqttCommand.Create(header, data);
        }
    }
}
