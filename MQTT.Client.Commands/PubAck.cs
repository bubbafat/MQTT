using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;
using MQTT.Commands;

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
                throw new ProtocolException(this.CommandMessage, "Remaining length must be 2");
            }

            using (MemoryStream stream = new MemoryStream(data))
            {
                MessageId = MessageId.FromStream(stream);
            }
        }
    }
}
