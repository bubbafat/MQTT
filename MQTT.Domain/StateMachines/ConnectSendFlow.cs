using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Types;
using System.Threading.Tasks;
using System.Threading;

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
                onSuccess = (MqttCommand c) => { };
            }

            return Send(command)
                .ContinueWith((task) =>
                    WaitFor(CommandMessage.CONNACK, MessageId.Any, TimeSpan.FromSeconds(30)), 
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning)
                .ContinueWith((task) =>
                    onSuccess(command),
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.LongRunning);
        }
    }
}
