using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Commands;

namespace MQTT.Commands
{
    public class PingReq : MqttCommand
    {
        public PingReq()
            : this(new FixedHeader(CommandMessage.PINGREQ))
        {
        }

        public PingReq(FixedHeader header)
            :  base (header)
        {
            if (header.RemainingLength != 0)
            {
                throw new ProtocolException(CommandMessage, "PingReq does not have any payload data");
            }
        }
    }
}
