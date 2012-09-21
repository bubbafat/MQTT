using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTT.Commands;
using System.Net.Sockets;
using MQTT.Types;

namespace MQTT.Broker.Network
{
    class CommandReader : ICommandReader
    {
        Task<MqttCommand> ICommandReader.ReadAsync(NetworkConnection connection)
        {
            return Task.Factory.StartNew<MqttCommand>(() => ((ICommandReader)this).Read(connection));
        }

        MqttCommand ICommandReader.Read(NetworkConnection connection)
        {
            FixedHeader header;
            byte[] data = null;

            header = FixedHeader.FromStream(connection.Stream);

            if (header.RemainingLength > 0)
            {
                data = connection.Stream.ReadBytesOrFail(header.RemainingLength);
            }

            MqttCommand cmd = MqttCommand.Create(header, data);

            System.Diagnostics.Debug.WriteLine("RECV {0} : {1}",
                cmd.CommandMessage, cmd.MessageId);

            return cmd;
        }
    }
}
