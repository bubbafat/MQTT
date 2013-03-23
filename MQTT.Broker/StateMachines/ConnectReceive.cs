using MQTT.Commands;
using MQTT.Broker.Network;
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
            var command = _reader.Read(connection);
            if (command.CommandMessage != CommandMessage.CONNECT)
            {
                throw new ProtocolException(command.CommandMessage, "Expected CONNECT");
            }

            var connect = (Connect)command;

            _writer.Send(connection, new ConnAck());

            return new NamedConnection(connect.ClientIdentifier, connection);
        }
    }
}
