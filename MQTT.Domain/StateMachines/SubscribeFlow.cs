using System;
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
                release = p => { };
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
            var ack = new SubAck(msg.MessageId);
            var subCmd = msg as Subscribe;
            if (subCmd == null)
            {
                throw new InvalidOperationException("Subscribe operationg expects Subscribe command");
            }

            foreach (Subscription sub in subCmd.Subscriptions)
            {
                ack.Grants.Add(QualityOfService.AtMostOnce);
            }

            return Send(ack)
                .ContinueWith(task =>
                    Task.Factory.StartNew(() => release(msg)),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }
    }
}
