using System;
using MQTT.Commands;
using MQTT.Broker.Network;
using MQTT.Types;

namespace MQTT.Broker.StateMachines
{
    class SubscribeReceive : StateMachine
    {
        readonly MqttCommand _command;
        readonly NamedConnection _connection;

        public SubscribeReceive(MqttCommand cmd, NamedConnection connection)
        {
            _command = cmd;
            _connection = connection;
        }

        public override void Start()
        {
            switch (_command.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    throw new ProtocolException(_command.CommandMessage);
                case QualityOfService.AtLeastOnce:
                    ProcessSubscription();
                    break;
                case QualityOfService.ExactlyOnce:
                    throw new ProtocolException(_command.CommandMessage);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }

        private void ProcessSubscription()
        {
            var subCmd = _command as Subscribe;
            if (subCmd == null)
            {
                throw new InvalidOperationException("Command was not of type Subscribe");
            }

            var ack = new SubAck(_command.MessageId);
            
            foreach (Subscription sub in subCmd.Subscriptions)
            {
                ack.Grants.Add(QualityOfService.AtMostOnce);
            }

            _connection.Send(new SubAck(_command.MessageId));
            _connection.Complete(_command);
        }
    }
}
