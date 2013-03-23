using System;
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

        internal Task Deliver(MqttCommand cmd)
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
                    var sub = command as Subscribe;
                    if (sub == null)
                    {
                        throw new ArgumentException("Message declared itself as Subscribe but was not of type Subscribe", "command");
                    }
                    ActiveSubscriptions.Current.Add(ClientId, sub.Subscriptions);
                    break;
                case CommandMessage.PUBLISH:
                    var pub = command as Publish;
                    if (pub == null)
                    {
                        throw new ArgumentException("Message declared itself as Publish but was not of type Publish", "command");
                    }
                    foreach (var client in ActiveSubscriptions.Current.Publish(ClientId, pub.Topic, pub.Message))
                    {
                        Manager.Send(client, new Publish(pub.Topic, pub.Message));
                    }
                    break;
                case CommandMessage.UNSUBSCRIBE:
                    var unsub = command as Unsubscribe;
                    if (unsub == null)
                    {
                        throw new ArgumentException("Message declared itself as Unsubscribe but was not of type Unsubscribe", "command");
                    }
                    ActiveSubscriptions.Current.Remove(ClientId, unsub.Topics);
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
            return Task.Factory.StartNew(machine.Start, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
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
            var writer = BrokerFactory.Get<ICommandWriter>();
            writer.Send(Connection, command);
        }

        internal IActiveConnectionManager Manager { get; set; }
    }
}
