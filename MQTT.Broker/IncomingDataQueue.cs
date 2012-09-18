using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace MQTT.Broker
{
    internal static class IncomingDataQueue
    {
        static ConcurrentQueue<ActiveConnection> _ready = new ConcurrentQueue<ActiveConnection>();

        public static void Enqueue(IList<ActiveConnection> connections)
        {
            foreach(ActiveConnection conn in connections)
            {
                _ready.Enqueue(conn);
            }
        }

        public static bool TryDequeue(out ActiveConnection connection)
        {
            return _ready.TryDequeue(out connection);
        }
    }
}
