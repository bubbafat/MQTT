using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Commands;
using MQTT.Broker.StateMachines;

namespace MQTT.Broker.Network
{
    class NamedConnection
    {
        public string ClientId { get; private set; }
        public NetworkConnection Connection { get; private set; }

        readonly ConcurrentDictionary<ushort, Action<MqttCommand>> _inflight = new ConcurrentDictionary<ushort, Action<MqttCommand>>();
        readonly object _lock = new object();

        public NamedConnection(string clientId, NetworkConnection connection)
        {
            ClientId = clientId;
            Connection = connection;
        }

        internal void Deliver(Commands.MqttCommand cmd)
        {
            switch (cmd.CommandMessage)
            {
                case CommandMessage.PUBLISH:
                    StartNew(new PublishReceive(cmd, this));
                    break;
                case CommandMessage.SUBSCRIBE:
                    StartNew(new SubscribeReceive(cmd, this));
                    break;
                case CommandMessage.PINGREQ:
                    StartNew(new PingReceived(cmd, this));
                    break;
                case CommandMessage.DISCONNECT:
                    StartNew(new DisconnectReceived(cmd, this));
                    break;
                case CommandMessage.UNSUBSCRIBE:
                    StartNew(new UnsubscribeReceived(cmd, this));
                    break;
                case CommandMessage.PUBACK:
                case CommandMessage.PUBCOMP:
                case CommandMessage.PUBREC:
                case CommandMessage.PUBREL:
                    lock (_lock)
                    {
                        Action<MqttCommand> toRun;
                        if (_inflight.TryRemove(cmd.MessageId.Value, out toRun))
                        {
                            toRun(cmd);
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException("Nope!");
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

        private void StartNew(StateMachine machine)
        {
            machine.Start();
        }

        internal void Desire(ushort messageId, Action<MqttCommand> callback)
        {
            lock (_lock)
            {
                _inflight.AddOrUpdate(messageId, callback, (key, old) => callback);
            }
        }

        internal Task Send(MqttCommand command)
        {
            ICommandWriter writer = Factory.Get<ICommandWriter>();
            return writer.SendAsync(Connection, command);
        }

        internal IActiveConnectionManager Manager { get; set; }
    }
}
