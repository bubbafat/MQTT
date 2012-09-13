using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using System.Threading;

namespace MQTT.Client
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

        public void Deliver(ClientCommand command)
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

        public Task StartNew(ClientCommand command, Action<ClientCommand> onSuccess)
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

        internal Task<ClientCommand> WaitForCommand(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            lock (_desireLock)
            {
                ClientCommand maybeLoved = _unlovedCommands.Where(c => c.CommandMessage == message && c.MessageId == messageId).FirstOrDefault();

                if (maybeLoved != null)
                {
                    return Task<ClientCommand>.Factory.StartNew(() => maybeLoved);
                }
                else
                {
                    ClientCommand result = null;
                    ManualResetEvent wait = new ManualResetEvent(false);

                    Desire d = new Desire(message, messageId, (ClientCommand cmd) =>
                    {
                        result = cmd;
                        wait.Set();
                    });

                    _desireCache.AddAndRemoveDuplicates(d);

                    return Task<ClientCommand>.Factory.StartNew(() =>
                        {
                            wait.WaitOne(timeout);
                            return result;
                        });
                }
            }
        }

        readonly object _desireLock = new object();

        List<ClientCommand> _unlovedCommands = new List<ClientCommand>();
        DesireCache _desireCache = new DesireCache();

        internal Task Send(ClientCommand message)
        {
            return _broker.Send(message);
        }
    }
}
