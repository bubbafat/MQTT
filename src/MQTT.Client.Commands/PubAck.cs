using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class PubAck : MqttCommand
    {
        public PubAck(MessageId messageId)
            : base(new FixedHeader(CommandMessage.PUBACK))
        {
            MessageId = messageId;
        }

        protected override byte[] VariableHeader
        {
            get
            {
                return MessageId.ToByteArray();
            }
        }

        public PubAck(FixedHeader header, byte[] data)
            : base(header)
        {
            if (header.RemainingLength != 2 && data.Length != 2)
            {
                throw new ProtocolException(CommandMessage, "Remaining length must be 2");
            }

            using (var stream = new MemoryStream(data))
            {
                MessageId = MessageId.FromStream(stream);
            }
        }
    }
}
