using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                onSuccess = (MqttCommand p) => { };
            }

            switch (command.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.AtLeastOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBACK, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.ExactlyOnce:
                    return Send(command)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBREC, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Send(new PubRel(command.MessageId)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBCOMP, command.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            onSuccess(command),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
