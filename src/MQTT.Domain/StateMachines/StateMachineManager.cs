using System;
using System.Collections.Generic;
using System.Linq;
using MQTT.Types;
using MQTT.Commands;
using System.Threading.Tasks;
using System.Threading;

namespace MQTT.Domain.StateMachines
{
    public class StateMachineManager
    {
        readonly INetworkInterface _broker;

        readonly object _desireLock = new object();

        readonly List<MqttCommand> _unlovedCommands = new List<MqttCommand>();
        readonly DesireCache _desireCache = new DesireCache();

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
                    var connFlow = new ConnectReceiveFlow(this);
                    return connFlow.Start(command, onSuccess);
                case CommandMessage.PUBLISH:
                    var pubFlow = new PublishReceiveFlow(this);
                    return pubFlow.Start(command, onSuccess);
                case CommandMessage.SUBSCRIBE:
                    var subFlow = new SubscribeFlow(this);
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
                var maybeLoved =
                    _unlovedCommands.FirstOrDefault(c => c.MessageId == messageId && c.CommandMessage == message);

                if (maybeLoved != null)
                {
                    var tcs = new TaskCompletionSource<MqttCommand>();
                    tcs.SetResult(maybeLoved);
                    return tcs.Task;
                }

                MqttCommand result = null;
                var wait = new ManualResetEvent(false);

                var d = new Desire(message, messageId, cmd =>
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

        internal Task Send(MqttCommand message)
        {
            return _broker.Send(message);
        }
    }
}
