using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Commands;

namespace MQTT.Commands
{
    public class PingResp : MqttCommand
    {
        public PingResp()
            : this(new FixedHeader(CommandMessage.PINGRESP))
        {
        }

        public PingResp(FixedHeader header)
            : base (header)
        {
            if (header.RemainingLength != 0)
            {
                throw new ProtocolException(CommandMessage, "PingResp does not have any payload data");
            }
        }
    }
}
