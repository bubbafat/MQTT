using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public class PubAck : ClientCommand
    {
        public PubAck(ushort messageId)
            : this(new FixedHeader(CommandMessage.PUBACK), null)
        {
            MessageId = new MessageId(messageId);
        }

        public PubAck(FixedHeader header, byte[] data)
            : base(header)
        {
            if (header.QualityOfService == QualityOfService.AtLeastOnce ||
                header.QualityOfService == QualityOfService.ExactlyOnce)
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    MessageId = MessageId.FromStream(stream);
                }
            }
        }
    }
}
