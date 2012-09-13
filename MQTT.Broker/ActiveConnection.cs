using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Domain;
using MQTT.Commands;
using MQTT.Domain.StateMachines;
using MQTT.Types;

namespace MQTT.Broker
{
    internal sealed class ActiveConnection : IDisposable
    {
        NetworkInterface _network;
        StateMachineManager _manager;
        MessageIdSequence _idSeq = new MessageIdSequence();


        public ActiveConnection(NetworkInterface network)
        {
            _network = network;
            _manager = new StateMachineManager(_network);
            _network.Start(OnIncomingMessage);
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
                    _manager.StartNew(command, (MqttCommand cmd) =>
                        {
                            notify(cmd);
                        });
                    break;
            }
        }

        private void notify(MqttCommand command)
        {
            // do nothing right now
        }
    }
}
