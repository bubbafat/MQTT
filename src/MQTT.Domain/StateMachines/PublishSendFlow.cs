using System;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain.StateMachines
{
    public class PublishSendFlow : StateMachine
    {
        public PublishSendFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(MqttCommand command, Action<MqttCommand> onSuccess)
        {
            if (onSuccess == null)
            {
                onSuccess = p => { };
            }

            switch (command.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Send(command)
                        .ContinueWith(task =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                case QualityOfService.AtLeastOnce:
                    return Send(command)
                        .ContinueWith(task =>
                            WaitFor(CommandMessage.PUBACK, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith(task =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                case QualityOfService.ExactlyOnce:
                    return Send(command)
                        .ContinueWith(task =>
                            WaitFor(CommandMessage.PUBREC, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith(task =>
                            Send(new PubRel(command.MessageId)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith(task =>
                            WaitFor(CommandMessage.PUBCOMP, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                        .ContinueWith(task =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
