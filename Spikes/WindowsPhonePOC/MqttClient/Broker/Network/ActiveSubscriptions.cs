using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Domain;

namespace MQTT.Broker
{
    public sealed class ActiveSubscriptions
    {
        private static ActiveSubscriptions _current = new ActiveSubscriptions();
        
        readonly List<ActiveSubscription> _activeSubscriptions = new List<ActiveSubscription>();
        readonly object _subLock = new object();

        public static ActiveSubscriptions Current
        {
            get
            {
                return _current;
            }
        }

        internal void Add(string clientId, List<Commands.Subscription> list)
        {
            lock (_subLock)
            {
                foreach (Commands.Subscription sub in list)
                {
                    _activeSubscriptions.Add(new ActiveSubscription(clientId, sub));
                }
            }
        }

        internal IList<string> Publish(string clientId, string topic, byte[] message)
        {
            List<string> deliveryClients = new List<string>();

            lock (_subLock)
            {
                foreach (ActiveSubscription sub in _activeSubscriptions.Where(s => s.ClientId != clientId))
                {
                    if (!deliveryClients.Contains(sub.ClientId))
                    {
                        if (sub.Subscription.IncludesPath(topic))
                        {
                            deliveryClients.Add(sub.ClientId);
                        }
                    }
                }
            }

            return deliveryClients.ToList();
        }

        internal void Remove(string clientId, List<string> list)
        {
            lock (_subLock)
            {
                foreach (string topic in list)
                {
                    for(int i = _activeSubscriptions.Count-1; i >= 0; i--)
                    {
                        if(_activeSubscriptions[i].ClientId == clientId && _activeSubscriptions[i].Subscription.Topic == topic)
                        {
                            _activeSubscriptions.RemoveAt(i);
                        }
                    }
                }
            }
        }

        internal void Remove(string clientId)
        {
            lock (_subLock)
            {
                for (int i = _activeSubscriptions.Count - 1; i >= 0; i--)
                {
                    if (_activeSubscriptions[i].ClientId == clientId)
                    {
                        _activeSubscriptions.RemoveAt(i);
                    }
                }
            }
        }

        class ActiveSubscription
        {
            public ActiveSubscription(string clientId, Commands.Subscription subscription)
            {
                ClientId = clientId;
                Subscription = subscription;
            }

            public string ClientId { get; private set; }
            public Commands.Subscription Subscription { get; private set; }
        }
    }
}
