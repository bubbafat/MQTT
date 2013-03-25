using System;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain.StateMachines
{
    public class PingSendFlow : StateMachine
    {
        public PingSendFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(MqttCommand command, Action<MqttCommand> onSuccess)
        {
            if (onSuccess == null)
            {
                onSuccess = c => { };
            }

            return Send(command)
                .ContinueWith(task =>
                    WaitFor(CommandMessage.PINGRESP, MessageId.Any, TimeSpan.FromSeconds(30)),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                .ContinueWith(task =>
                    onSuccess(command),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }
    }
}
