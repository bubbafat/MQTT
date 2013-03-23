using System;
using MQTT.Commands;
using System.Threading.Tasks;

namespace MQTT.Domain.StateMachines
{
    class ConnectReceiveFlow : StateMachine
    {
        public ConnectReceiveFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(MqttCommand command, Action<MqttCommand> onSuccess)
        {
            if (onSuccess == null)
            {
                onSuccess = c => { };
            }

            return Send(new ConnAck())
                .ContinueWith(task =>
                    onSuccess(command),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }    
    }
}
