using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Commands;
using System.Threading.Tasks;
using System.Threading;

namespace MQTT.Domain.StateMachines
{
    public class StateMachineManager
    {
        readonly Dictionary<CommandMessage, Dictionary<QualityOfService, Type>> _flowTable 
            = new Dictionary<CommandMessage,Dictionary<QualityOfService,Type>>();

        readonly object _flowLock = new object();

        IMqttBroker _broker;

        public StateMachineManager(IMqttBroker broker)
        {
            _broker = broker;
        }

        public void Deliver(MqttCommand command)
        {
            Desire desire;

            lock (_desireLock)
            {
                if (_desireCache.TryGetAndRemove(command.CommandMessage, command.MessageId, out desire))
                {
                    desire.Fulfilled(command);
                }
                else
                {
                    _unlovedCommands.Add(command);
                }
            }
        }

        public Task StartNew(MqttCommand command, Action<MqttCommand> onSuccess)
        {
            switch (command.CommandMessage)
            {
                case CommandMessage.PUBLISH:
                    return Task.Factory.StartNew(() =>
                        {
                            PublishReceiveFlow flow = new PublishReceiveFlow(this);
                            return flow.Start(command, onSuccess);
                        });
                default:
                    return Task.Factory.StartNew(() =>
                        {
                            throw new InvalidOperationException("Unhandled command type");
                        });
            }
        }

        internal Task<MqttCommand> WaitForCommand(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            lock (_desireLock)
            {
                MqttCommand maybeLoved = _unlovedCommands.Where(c => c.CommandMessage == message && c.MessageId == messageId).FirstOrDefault();

                if (maybeLoved != null)
                {
                    return Task<MqttCommand>.Factory.StartNew(() => maybeLoved);
                }
                else
                {
                    MqttCommand result = null;
                    ManualResetEvent wait = new ManualResetEvent(false);

                    Desire d = new Desire(message, messageId, (MqttCommand cmd) =>
                    {
                        result = cmd;
                        wait.Set();
                    });

                    _desireCache.AddAndRemoveDuplicates(d);

                    return Task<MqttCommand>.Factory.StartNew(() =>
                        {
                            wait.WaitOne(timeout);
                            return result;
                        });
                }
            }
        }

        readonly object _desireLock = new object();

        List<MqttCommand> _unlovedCommands = new List<MqttCommand>();
        DesireCache _desireCache = new DesireCache();

        internal Task Send(MqttCommand message)
        {
            return _broker.Send(message);
        }
    }
}
