using System;
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
                release = p => { };
            }
            switch (msg.Header.QualityOfService)
            {
                case QualityOfService.AtLeastOnce:
                    return Send(msg)
                        .ContinueWith(task =>
                            WaitFor(CommandMessage.SUBACK, msg.MessageId, TimeSpan.FromSeconds(30)))
                        .ContinueWith(task =>
                            release(msg),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                case QualityOfService.AtMostOnce:
                case QualityOfService.ExactlyOnce:
                    var tcs = new TaskCompletionSource<MqttCommand>();
                    tcs.SetException(new ProtocolException(msg.CommandMessage));
                    return tcs.Task;
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }


    }
}
