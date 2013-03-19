using MQTT.Types;

namespace MQTT.Commands
{
    public class Disconnect : MqttCommand
    {
        public Disconnect()
            : this(new FixedHeader(CommandMessage.DISCONNECT))
        {
        }

        public Disconnect(FixedHeader header)
            : base(header)
        {
            if (header.RemainingLength != 0)
            {
                throw new ProtocolException(CommandMessage, "Disconnect does not have any payload data");
            }
        }
    }
}
