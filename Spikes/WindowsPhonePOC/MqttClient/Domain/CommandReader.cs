﻿using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class CommandReader : ICommandReader
    {
        MqttCommand ICommandReader.Read(NetworkConnection connection)
        {
            byte[] data = null;

            FixedHeader header = FixedHeader.Load(connection);

            if (header.RemainingLength > 0)
            {
                data = connection.Stream.ReadBytesOrFailAsync(header.RemainingLength).Await().Result;
            }

            MqttCommand cmd = MqttCommand.Create(header, data);

            System.Diagnostics.Debug.WriteLine("RECV {0}", cmd);

            return cmd;
        }
    }
}
