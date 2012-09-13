using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using MQTT.Client.Commands;

namespace MQTT.Client
{
    internal class DesireCache
    {
        readonly object _lock = new object();

        Dictionary<int, Desire> _desires = new Dictionary<int, Desire>();

        internal void AddAndRemoveDuplicates(Desire d)
        {
            lock (_lock)
            {
                int key = GetKeyFrom(d.Message, d.MessageId);
                if (_desires.ContainsKey(key))
                {
                    _desires.Remove(key);
                }

                _desires.Add(key, d);
            }
        }

        internal bool TryGetAndRemove(Types.CommandMessage commandMessage, Types.MessageId messageId, out Desire desire)
        {
            lock (_lock)
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
        }

        private int GetKeyFrom(CommandMessage message, MessageId id)
        {
            return (((ushort)message) << 16) + id.Value;            
        }
    }

    internal class Desire
    {
        public Desire(CommandMessage msg, MessageId id, Action<ClientCommand> fulfilled)
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
        private readonly Action<ClientCommand> _fulfilled;

        public void Fulfilled(ClientCommand command)
        {
            _fulfilled(command);
        }
    }

}
