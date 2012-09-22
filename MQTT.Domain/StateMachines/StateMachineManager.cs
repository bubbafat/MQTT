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
        INetworkInterface _broker;

        readonly object _desireLock = new object();

        List<MqttCommand> _unlovedCommands = new List<MqttCommand>();
        DesireCache _desireCache = new DesireCache();

        public StateMachineManager(INetworkInterface broker)
        {
            _broker = broker;
        }

        public void Deliver(MqttCommand command)
        {
            lock (_desireLock)
            {
                Desire desire;
                
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
                case CommandMessage.CONNECT:
                    ConnectReceiveFlow connFlow = new ConnectReceiveFlow(this);
                    return connFlow.Start(command, onSuccess);
                case CommandMessage.PUBLISH:
                    PublishReceiveFlow pubFlow = new PublishReceiveFlow(this);
                    return pubFlow.Start(command, onSuccess);
                case CommandMessage.SUBSCRIBE:
                    SubscribeFlow subFlow = new SubscribeFlow(this);
                    return subFlow.Start(command, onSuccess);

                default:
                    var tcs = new TaskCompletionSource<object>();
                    tcs.SetException(new InvalidOperationException("Unhandled command type"));
                    return tcs.Task;
            }
        }

        internal Task<MqttCommand> WaitForCommand(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            lock (_desireLock)
            {
                MqttCommand maybeLoved = _unlovedCommands.Where(c => c.MessageId == messageId && c.CommandMessage == message).FirstOrDefault();

                if (maybeLoved != null)
                {
                    var tcs = new TaskCompletionSource<MqttCommand>();
                    tcs.SetResult(maybeLoved);
                    return tcs.Task;
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

        internal Task Send(MqttCommand message)
        {
            return _broker.Send(message);
        }
    }
}
