using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Broker.Network;
using MQTT.Types;
using System.Threading.Tasks;

namespace MQTT.Broker.StateMachines
{
    class SubscribeReceive : StateMachine
    {
        MqttCommand _command;
        NamedConnection _connection;

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
            SubAck ack = new SubAck(_command.MessageId);
            Subscribe subCmd = _command as Subscribe;
            foreach (Subscription sub in subCmd.Subscriptions)
            {
                ack.Grants.Add(QualityOfService.AtMostOnce);
            }

            _connection.Send(new SubAck(_command.MessageId))
                .ContinueWith((task) =>
                    _connection.Complete(_command),
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
