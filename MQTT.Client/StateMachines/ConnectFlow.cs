using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using MQTT.Types;
using System.Threading.Tasks;
using System.Threading;

namespace MQTT.Client
{
    public class ConnectFlow : StateMachine
    {
        public ConnectFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(ClientCommand command, Action<ClientCommand> onSuccess)
        {
            if (onSuccess == null)
            {
                onSuccess = (ClientCommand c) => { };
            }

            return Send(command)
                .ContinueWith((task) =>
                    WaitFor(CommandMessage.CONNACK, MessageId.Any, TimeSpan.FromSeconds(30)), 
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWith((task) =>
                    Task.Factory.StartNew(() => onSuccess(command)),
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
