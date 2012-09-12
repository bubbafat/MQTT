using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    public class PubRel : ClientCommand
    {
        private byte[] data;

        public PubRel(ushort messageId)
            : this(new FixedHeader(CommandMessage.PUBREL), null)
        {
        }

        public PubRel(FixedHeader header, byte[] data)
            : base(header)
        {
            throw new NotImplementedException();
        }
    }
}
