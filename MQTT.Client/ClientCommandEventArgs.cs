using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;

namespace MQTT.Client
{
    public sealed class ClientCommandEventArgs : EventArgs
    {
        public ClientCommandEventArgs(ClientCommand command)
        {
            Command = command;
        }

        public ClientCommand Command { get; private set; }
    }
}
