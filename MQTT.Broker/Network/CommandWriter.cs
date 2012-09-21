using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Broker.Network
{
    internal class CommandWriter : ICommandWriter
    {
        Task ICommandWriter.SendAsync(NetworkConnection connection, MqttCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
                    System.Diagnostics.Debug.WriteLine("SEND {0} : {1}",
                        command.CommandMessage, command.MessageId);

                    byte[] bytes = command.ToByteArray();
                    int written = 0;

                    while (written < bytes.Length)
                    {
                        Task<int> wrote = connection.Stream.WriteAsync(bytes, written, bytes.Length-written);

                        wrote.Wait();
                        
                        switch (wrote.Status)
                        {
                            case TaskStatus.Faulted:
                                throw wrote.Exception;
                            case TaskStatus.Canceled:
                                throw new OperationCanceledException();
                            case TaskStatus.RanToCompletion:
                                written += wrote.Result;
                                break;
                            default:
                                throw new InvalidOperationException("This was not nice");
                        }
                    }
                });
        }
    }
}
