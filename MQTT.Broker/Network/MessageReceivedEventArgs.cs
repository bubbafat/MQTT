using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;

namespace MQTT.Broker.Network
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string clientId, MqttCommand command)
        {
            ClientId = clientId;
            Command = command;
        }

        public string ClientId { get; private set; }
        public MqttCommand Command { get; private set; }
    }
}
