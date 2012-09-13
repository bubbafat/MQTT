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
    public abstract class StateMachine
    {
        StateMachineManager _manager;

        protected StateMachine(StateMachineManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            _manager = manager;
        }

        public abstract Task Start(ClientCommand command, Action<ClientCommand> onSuccess);

        protected Task<ClientCommand> WaitFor(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            return _manager.WaitForCommand(message, messageId, timeout);
        }

        protected Task Send(ClientCommand message)
        {
            return _manager.Send(message);
        }
    }
}
