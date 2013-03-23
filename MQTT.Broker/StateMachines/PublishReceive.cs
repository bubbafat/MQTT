using System;
using MQTT.Commands;
using MQTT.Broker.Network;
using MQTT.Types;

namespace MQTT.Broker.StateMachines
{
    class PublishReceive : StateMachine
    {
        readonly MqttCommand _command;
        readonly NamedConnection _connection;

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
