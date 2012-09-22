using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Domain
{
    public class CommandWriter : ICommandWriter
    {
        void ICommandWriter.Send(NetworkConnection connection, MqttCommand command)
        {
            byte[] bytes = command.ToByteArray();

            System.Diagnostics.Debug.WriteLine("SEND {0} => {1}", command, BitConverter.ToString(bytes).Replace("-", " "));

            connection.Stream.Write(bytes, 0, bytes.Length);
        }
    }
}
