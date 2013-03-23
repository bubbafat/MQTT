using System;
using MQTT.Commands;
using MQTT.Types;
using System.Threading.Tasks;

namespace MQTT.Domain.StateMachines
{
    public abstract class StateMachine
    {
        readonly StateMachineManager _manager;

        protected StateMachine(StateMachineManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            _manager = manager;
        }

        public abstract Task Start(MqttCommand command, Action<MqttCommand> onSuccess);

        protected Task<MqttCommand> WaitFor(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            return _manager.WaitForCommand(message, messageId, timeout);
        }

        protected Task Send(MqttCommand message)
        {
            return _manager.Send(message);
        }
    }
}
