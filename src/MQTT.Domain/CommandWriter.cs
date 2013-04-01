using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class CommandWriter : ICommandWriter
    {
        Task ICommandWriter.Send(NetworkConnection connection, MqttCommand command)
        {
            Debug.WriteLine("{0} : Writing command {1} id {2}", DateTime.Now.ToString("o"), command.CommandMessage, command.MessageId);

            byte[] bytes = command.ToByteArray();
            return connection.Write(bytes, 0, bytes.Length);
        }
    }
}
