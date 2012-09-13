using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

using System.Threading.Tasks;
using System.Threading;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Domain
{
    public sealed class MqttNetworkBroker : IMqttBroker
    {
        NetworkInterface _network;

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            Socket socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endpoint);
            _network = new NetworkInterface(socket);
            _network.Start((MqttCommand cmd) =>
                {
                    MessageReceivedCallback recv = OnMessageReceived;
                    if (recv != null)
                    {
                        recv(this, new ClientCommandEventArgs(cmd));
                    }
                });
        }

        public void Disconnect()
        {
            _network.Disconnect();
        }

        public System.Threading.Tasks.Task Send(MqttCommand command)
        {
            return _network.Send(command);
        }

        public bool IsConnected
        {
            get
            {
                return _network.IsConnected;
            }
        }

        public event MessageReceivedCallback OnMessageReceived;

        public void Dispose()
        {
            using (_network) { }
        }
    }

    public class BlockingQueue<T>
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
