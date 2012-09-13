using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;

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
                onSuccess = (MqttCommand c) => { };
            }

            return Send(new ConnAck())
                .ContinueWith((task) =>
                    Task.Factory.StartNew(() => onSuccess(command)),
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }    
    }
}
