using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    public class UnSubAck : ClientCommand
    {
        public UnSubAck()
            : this(new FixedHeader(CommandMessage.UNSUBACK), null)
        {
        }

        public UnSubAck(FixedHeader header, byte[] data)
            : base(header)
        {
            // for now ignore unsuback message ID
        }
    }
}
