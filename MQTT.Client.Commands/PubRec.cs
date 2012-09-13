using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public class PubRec : ClientCommand
    {
        public PubRec(MessageId messageId)
            : base(new FixedHeader(CommandMessage.PUBREC))
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

        public PubRec(FixedHeader header, byte[] data)
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
