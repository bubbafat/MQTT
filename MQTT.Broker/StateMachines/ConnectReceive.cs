using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Commands;
using MQTT.Broker.Network;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Domain;

namespace MQTT.Broker.StateMachines
{
    class ConnectReceive
    {
        private readonly ICommandWriter _writer;
        private readonly ICommandReader _reader;

        public ConnectReceive(ICommandWriter writer, ICommandReader reader)
        {
            _writer = writer;
            _reader = reader;
        }

        internal NamedConnection Run(NetworkConnection connection)
        {
            MqttCommand command = _reader.Read(connection);
            if (command.CommandMessage != Types.CommandMessage.CONNECT)
            {
                throw new ProtocolException(command.CommandMessage, "Expected CONNECT");
            }

            Connect connect = (Connect)command;

            _writer.Send(connection, new ConnAck());

            return new NamedConnection(connect.ClientIdentifier, connection);
        }
    }
}
