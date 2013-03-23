using System;
using MQTT.Commands;

namespace MQTT.Domain
{
    public sealed class ClientCommandEventArgs : EventArgs
    {
        public ClientCommandEventArgs(MqttCommand command)
        {
            Command = command;
        }

        public MqttCommand Command { get; private set; }
    }
}
