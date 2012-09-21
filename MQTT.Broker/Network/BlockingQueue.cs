using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MQTT.Broker.Network
{
    internal class BlockingQueue<T>
    {
        private readonly Queue<T> m_Queue = new Queue<T>();
        private readonly object _lock = new object();

        public void Enqueue(T item)
        {
            lock (_lock)
            {
                m_Queue.Enqueue(item);
                Monitor.Pulse(_lock);
            }
        }

        public T Dequeue()
        {
            lock (_lock)
            {
                while (m_Queue.Count == 0)
                {
                    Monitor.Wait(_lock);
                }

                return m_Queue.Dequeue();
            }
        }
    }
}
