using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQTT.Broker
{
    public sealed class ActiveSubscriptions
    {
        private static ActiveSubscriptions _current = new ActiveSubscriptions();
        public static ActiveSubscriptions Current
        {
            get
            {
                return _current;
            }
        }

        internal static void Add(ActiveConnection activeConnection, List<Commands.Subscription> list)
        {
            throw new NotImplementedException();
        }

        internal static void Publish(string ClientId, string p, byte[] p_2)
        {
            throw new NotImplementedException();
        }

        internal static void Remove(ActiveConnection activeConnection, List<string> list)
        {
            throw new NotImplementedException();
        }

        internal static void Remove(ActiveConnection activeConnection)
        {
            throw new NotImplementedException();
        }
    }
}
