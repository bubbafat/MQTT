using System;
using MQTT.Commands;
using MQTT.Types;
using System.Threading.Tasks;

namespace MQTT.Domain.StateMachines
{
    public class ConnectSendFlow : StateMachine
    {
        public ConnectSendFlow(StateMachineManager manager)
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
                    WaitFor(CommandMessage.CONNACK, MessageId.Any, TimeSpan.FromSeconds(30)), 
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                .ContinueWith(task =>
                    onSuccess(command),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }
    }
}
