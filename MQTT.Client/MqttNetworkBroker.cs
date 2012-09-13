using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Types;
using System.Threading.Tasks;
using MQTT.Client.Commands;
using System.Threading;

namespace MQTT.Client
{
    public sealed class MqttNetworkBroker : IMqttBroker
    {
        Socket _socket;
        Thread _receiveThread;

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endpoint);
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.Start();
        }

        public void Disconnect()
        {
            _socket.Close();
        }

        public System.Threading.Tasks.Task Send(Commands.ClientCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
                    System.Diagnostics.Debug.WriteLine("SEND: {0} ({1})", command.CommandMessage, command.MessageId);

                    byte[] buffer = command.ToByteArray();

                    int start = 0;

                    while (start < buffer.Length)
                    {
                        int end = buffer.Length - start;
                        Task<int> sendResult = _socket.SendAsync(buffer, start, end);
                        sendResult.Wait();
                        if (sendResult.IsFaulted)
                        {
                            throw sendResult.Exception;
                        }

                        if (sendResult.IsCompleted)
                        {
                            if (sendResult.Result == 0)
                            {
                                throw new InvalidOperationException("Stream unexpectedly closed!");
                            }

                            start += sendResult.Result;
                        }
                    }
                });
        }

        public bool IsConnected
        {
            get { return _socket.Connected; }
        }

        public event MessageReceivedCallback OnMessageReceived;

        public void Dispose()
        {
            using (_socket) { }
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                FixedHeader header = FixedHeader.FromSocket(_socket);
                byte[] data = null;

                if (header.RemainingLength > 0)
                {
                    data = _socket.ReadBytes(header.RemainingLength);
                }

                MessageReceivedCallback recv = OnMessageReceived;
                if (recv != null)
                {
                    recv(this, new ClientCommandEventArgs(ClientCommand.Create(header, data)));
                }
            }
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
