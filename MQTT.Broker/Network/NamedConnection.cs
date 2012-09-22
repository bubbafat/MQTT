using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Commands;
using MQTT.Broker.StateMachines;
using MQTT.Domain;

namespace MQTT.Broker.Network
{
    class NamedConnection
    {
        public string ClientId { get; private set; }
        public NetworkConnection Connection { get; private set; }

        readonly ConcurrentDictionary<ushort, Action<MqttCommand>> _desires = new ConcurrentDictionary<ushort, Action<MqttCommand>>();
        readonly object _lock = new object();

        public NamedConnection(string clientId, NetworkConnection connection)
        {
            ClientId = clientId;
            Connection = connection;
        }

        internal Task Deliver(Commands.MqttCommand cmd)
        {
            switch (cmd.CommandMessage)
            {
                case CommandMessage.PUBLISH:
                    return StartNew(new PublishReceive(cmd, this));
                case CommandMessage.SUBSCRIBE:
                    return StartNew(new SubscribeReceive(cmd, this));
                case CommandMessage.PINGREQ:
                    return StartNew(new PingReceived(cmd, this));
                case CommandMessage.DISCONNECT:
                    return StartNew(new DisconnectReceived(cmd, this));
                case CommandMessage.UNSUBSCRIBE:
                    return StartNew(new UnsubscribeReceived(cmd, this));
                case CommandMessage.PUBACK:
                case CommandMessage.PUBCOMP:
                case CommandMessage.PUBREC:
                case CommandMessage.PUBREL:
                    var tcsRan = new TaskCompletionSource<object>();
                    try
                    {
                        lock (_lock)
                        {
                            Action<MqttCommand> toRun;
                            if (_desires.TryRemove(cmd.MessageId.Value, out toRun))
                            {
                                toRun(cmd);
                            }
                        }

                        tcsRan.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcsRan.SetException(ex);
                    }
                    return tcsRan.Task;
                default:
                    var tcsEx = new TaskCompletionSource<object>();
                    tcsEx.SetException(new InvalidOperationException("Nope!"));
                    return tcsEx.Task;
            }
        }

        internal void Complete(MqttCommand command)
        {
            System.Diagnostics.Debug.WriteLine("FINISHED: {0} {1}", command.CommandMessage, command.MessageId);

            switch (command.CommandMessage)
            {
                case CommandMessage.SUBSCRIBE:
                    ActiveSubscriptions.Current.Add(ClientId, (command as Subscribe).Subscriptions);
                    break;
                case CommandMessage.PUBLISH:
                    foreach (string client in ActiveSubscriptions.Current.Publish(ClientId, (command as Publish).Topic, (command as Publish).Message))
                    {
                        Manager.Send(client, new Publish((command as Publish).Topic, (command as Publish).Message));
                    }
                    break;
                case CommandMessage.UNSUBSCRIBE:
                    ActiveSubscriptions.Current.Remove(ClientId, (command as Unsubscribe).Topics);
                    break;
                case CommandMessage.PINGREQ:
                    Send(new PingReq());
                    break;
                case CommandMessage.DISCONNECT:
                    ActiveSubscriptions.Current.Remove(ClientId);
                    Connection.Disconnect();
                    Manager.Disconnect(this);
                    break;
            }
        }

        private Task StartNew(StateMachine machine)
        {
            return Task.Factory.StartNew(() =>
                {
                    machine.Start();
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
        }

        internal void Desire(ushort messageId, Action<MqttCommand> callback)
        {
            lock (_lock)
            {
                _desires.AddOrUpdate(messageId, callback, (key, old) => callback);
            }
        }

        internal void Send(MqttCommand command)
        {
            ICommandWriter writer = BrokerFactory.Get<ICommandWriter>();
            writer.Send(Connection, command);
        }

        internal IActiveConnectionManager Manager { get; set; }
    }
}
