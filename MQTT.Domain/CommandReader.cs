using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTT.Commands;
using System.Net.Sockets;
using MQTT.Types;

namespace MQTT.Domain
{
    public class CommandReader : ICommandReader
    {
        MqttCommand ICommandReader.Read(NetworkConnection connection)
        {
            FixedHeader header;
            byte[] data = null;

            header = FixedHeader.Load(connection);

            if (header.RemainingLength > 0)
            {
                if (connection.Available >= header.RemainingLength)
                {
                    data = connection.Stream.ReadBytesOrFail(header.RemainingLength);
                }
                else
                {
                    data = connection.Stream.ReadBytesOrFailAsync(header.RemainingLength).Await<byte[]>().Result;
                }
            }

            MqttCommand cmd = MqttCommand.Create(header, data);

            System.Diagnostics.Debug.WriteLine("RECV {0}", cmd);

            return cmd;
        }
    }
}
