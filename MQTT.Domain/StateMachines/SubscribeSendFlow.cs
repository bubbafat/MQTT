using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Types;
using System.Threading.Tasks;

namespace MQTT.Domain.StateMachines
{
    public class SubscribeSendFlow : StateMachine
    {
        public SubscribeSendFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(MqttCommand msg, Action<MqttCommand> release)
        {
            if (release == null)
            {
                release = (MqttCommand p) => { };
            }

            switch (msg.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Task.Factory.StartNew(() => { throw new ProtocolException(msg.CommandMessage); });
                case QualityOfService.AtLeastOnce:
                    return Send(msg)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.SUBACK, msg.MessageId, TimeSpan.FromSeconds(30)))
                        .ContinueWith((task) =>
                            release(msg),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.ExactlyOnce:
                    return Task.Factory.StartNew(() => { throw new ProtocolException(msg.CommandMessage); });
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }


    }
}
