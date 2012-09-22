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
            HashSet<string> deliveryClients = new HashSet<string>();

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
                    _activeSubscriptions.RemoveAll(sub => sub.ClientId == clientId && sub.Subscription.Topic == topic);
                }
            }
        }

        internal void Remove(string clientId)
        {
            lock (_subLock)
            {
                _activeSubscriptions.RemoveAll(sub => sub.ClientId == clientId);
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
