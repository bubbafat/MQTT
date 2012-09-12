using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    public class PubRec : ClientCommand
    {
        private byte[] data;

        public PubRec()
            : this(new FixedHeader(CommandMessage.PUBREC), null)
        {
        }

        public PubRec(FixedHeader header, byte[] data)
            : base(header)
        {
            throw new NotImplementedException();
        }
    }
}
