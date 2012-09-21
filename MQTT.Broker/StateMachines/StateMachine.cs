using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Broker.Network;
using System.Threading;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Broker.StateMachines
{
    abstract class StateMachine
    {
        public abstract void Start();

        protected Task WaitFor(NamedConnection connection, ushort messageId, CommandMessage mustBeType)
        {
            ManualResetEvent available = new ManualResetEvent(false);

            Action<MqttCommand> ready = (cmd) =>
                {
                    if (cmd.CommandMessage != mustBeType)
                    {
                        throw new ProtocolException(
                            string.Format("ERROR: Expected {0} for message ID {1} but received {2}",
                            mustBeType, messageId, cmd.CommandMessage));
                    }

                    available.Set();
                };

            return Task.Factory.StartNew(() =>
                {
                    connection.Desire(messageId, ready);
                    available.WaitOne();
                });
        }
    }
}
