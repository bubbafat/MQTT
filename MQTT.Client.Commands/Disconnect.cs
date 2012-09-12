using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    public class Disconnect : ClientCommand
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
                throw new InvalidOperationException("Disconnect does not have any payload data");
            }
        }
    }
}
