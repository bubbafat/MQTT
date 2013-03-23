using System;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class CommandWriter : ICommandWriter
    {
        void ICommandWriter.Send(NetworkConnection connection, MqttCommand command)
        {
            byte[] bytes = command.ToByteArray();

            System.Diagnostics.Debug.WriteLine("SEND {0} => {1}", command, BitConverter.ToString(bytes).Replace("-", " "));

            connection.Stream.Write(bytes, 0, bytes.Length);
        }
    }
}
