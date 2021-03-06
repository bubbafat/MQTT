﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Broker.Network;
using MQTT.Types;
using System.Threading.Tasks;

namespace MQTT.Broker.StateMachines
{
    class PublishReceive : StateMachine
    {
        MqttCommand _command;
        NamedConnection _connection;

        public PublishReceive(MqttCommand cmd, NamedConnection connection)
        {
            _command = cmd;
            _connection = connection;
        }

        public override void Start()
        {
            switch (_command.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    _connection.Complete(_command);
                    break;
                case QualityOfService.AtLeastOnce:
                    _connection.Send(new PubAck(_command.MessageId));
                    _connection.Complete(_command);
                    break;
                case QualityOfService.ExactlyOnce:
                    _connection.Send(new PubRec(_command.MessageId));
                    WaitFor(_connection, _command.MessageId.Value, CommandMessage.PUBREL).Await();
                    _connection.Send(new PubComp(_command.MessageId));
                    _connection.Complete(_command);
                    break;
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
