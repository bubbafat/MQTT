using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Domain;
using MQTT.Commands;
using MQTT.Domain.StateMachines;
using MQTT.Types;
using System.Net.Sockets;

namespace MQTT.Broker
{
    internal sealed class ActiveConnection : IDisposable
    {
        NetworkInterface _network;
        StateMachineManager _manager;
        MessageIdSequence _idSeq = new MessageIdSequence();
        ConnectionManager _connectionManager;

        public ActiveConnection(ConnectionManager manager, NetworkInterface network)
        {
            _network = network;
            _manager = new StateMachineManager(_network);
            _connectionManager = manager;
        }

        public void ReadCommand()
        {
            OnIncomingMessage(_network.ReadCommand());
        }

        internal Socket Socket
        {
            get
            {
                return _network.Socket;
            }
        }

        public void Dispose()
        {
            using (_network) { }
        }

        private void OnIncomingMessage(MqttCommand command)
        {
            switch(command.CommandMessage)
            {                
                case CommandMessage.PUBACK:
                case CommandMessage.PUBCOMP:
                case CommandMessage.PUBREC:
                case CommandMessage.PUBREL:
                case CommandMessage.SUBACK:
                case CommandMessage.CONNACK:
                case CommandMessage.UNSUBACK:
                    _manager.Deliver(command);
                    break;
                case CommandMessage.PINGRESP:
                    // ignore (we sent it) - eventually track
                    break;
                default:
                    _manager.StartNew(command, notify);
                    break;
            }
        }

        public string ClientId { get; private set; }

        private void notify(MqttCommand command)
        {
            switch (command.CommandMessage)
            {
                case CommandMessage.CONNECT:
                    ClientId = (command as Connect).ClientIdentifier;
                    _connectionManager.Connected(this);
                    break;
                case CommandMessage.SUBSCRIBE:
                    ActiveSubscriptions.Add(this, (command as Subscribe).Subscriptions);
                    _connectionManager.Done(this);
                    break;
                case CommandMessage.PUBLISH:
                    ActiveSubscriptions.Publish(ClientId, (command as Publish).Topic, (command as Publish).Message);
                    _connectionManager.Done(this);
                    break;
                case CommandMessage.UNSUBSCRIBE:
                    ActiveSubscriptions.Remove(this, (command as Unsubscribe).Topics);
                    _connectionManager.Done(this);
                    break;
                case CommandMessage.PINGREQ:
                    _network.Send(new PingReq());
                    _connectionManager.Done(this);
                    break;
                case CommandMessage.DISCONNECT:
                    ActiveSubscriptions.Remove(this);
                    _network.Disconnect();
                    _connectionManager.Disconnect(this);
                    break;
            }
        }
    }
}
