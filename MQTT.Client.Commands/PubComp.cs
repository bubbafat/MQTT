using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    public class PubComp : ClientCommand
    {
        private byte[] data;

        public PubComp(ushort messageId)
            : this(new FixedHeader(CommandMessage.PUBCOMP), null)
        {
        }

        public PubComp(FixedHeader header, byte[] data)
            : base(header)
        {
            throw new NotImplementedException();
        }
    }
}
