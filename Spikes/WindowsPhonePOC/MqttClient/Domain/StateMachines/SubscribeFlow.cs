using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain.StateMachines
{
    public class SubscribeFlow : StateMachine
    {
        public SubscribeFlow(StateMachineManager manager)
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
                case QualityOfService.AtLeastOnce:
                    return ProcessSubscription(msg, release);
                case QualityOfService.AtMostOnce:
                case QualityOfService.ExactlyOnce:
                    var tcs = new TaskCompletionSource<MqttCommand>();
                    tcs.SetException(new ProtocolException(msg.CommandMessage));
                    return tcs.Task;
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }

        private Task ProcessSubscription(MqttCommand msg, Action<MqttCommand> release)
        {
            SubAck ack = new SubAck(msg.MessageId);
            Subscribe subCmd = msg as Subscribe;
            foreach (Subscription sub in subCmd.Subscriptions)
            {
                ack.Grants.Add(QualityOfService.AtMostOnce);
            }

            return Send(ack)
                .ContinueWith((task) =>
                    Task.Factory.StartNew(() => release(msg)),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }
    }
}
