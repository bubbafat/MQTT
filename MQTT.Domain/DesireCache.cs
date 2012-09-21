using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Commands;

namespace MQTT.Domain
{
    internal class DesireCache
    {
        Dictionary<int, Desire> _desires = new Dictionary<int, Desire>();

        internal void AddAndRemoveDuplicates(Desire d)
        {
            int key = GetKeyFrom(d.Message, d.MessageId);
            if (_desires.ContainsKey(key))
            {
                _desires.Remove(key);
            }

            _desires.Add(key, d);
        }

        internal bool TryGetAndRemove(Types.CommandMessage commandMessage, Types.MessageId messageId, out Desire desire)
        {
            int key = GetKeyFrom(commandMessage, messageId);
            if (_desires.TryGetValue(key, out desire))
            {
                _desires.Remove(key);
                return true;
            }
            else
            {
                desire = null;
                return false;
            }
        }

        private int GetKeyFrom(CommandMessage message, MessageId id)
        {
            return (((ushort)message) << 16) + id.Value;            
        }
    }

    internal class Desire
    {
        public Desire(CommandMessage msg, MessageId id, Action<MqttCommand> fulfilled)
        {
            if (fulfilled == null)
            {
                throw new ArgumentNullException("The fulfilled action cannot be null");
            }

            Message = msg;
            MessageId = id;
            _fulfilled = fulfilled;
        }

        public CommandMessage Message { get; private set; }
        public MessageId MessageId { get; private set; }
        private readonly Action<MqttCommand> _fulfilled;

        public void Fulfilled(MqttCommand command)
        {
            _fulfilled(command);
        }
    }

}
