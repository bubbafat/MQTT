using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class PubComp : MqttCommand
    {
        public PubComp(MessageId messageId)
            : base(new FixedHeader(CommandMessage.PUBCOMP))
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

        public PubComp(FixedHeader header, byte[] data)
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
