using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;
using System.Threading;

namespace MQTT.Domain
{
    public sealed class NetworkInterface : INetworkInterface
    {
        Thread _recvThread;
        Socket _socket;
        readonly object _socketReadLock = new object();
        readonly object _socketWriteLock = new object();

        public NetworkInterface(Socket socket)
        {
            _socket = socket;
        }

        public void Disconnect()
        {
            _recvThread.Abort();
            _socket.Close();
        }

        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }

        public MqttCommand ReadCommand()
        {
            FixedHeader header;
            byte[] data = null;

            header = FixedHeader.FromSocket(_socket);

            if (header.RemainingLength > 0)
            {
                data = _socket.ReadBytes(header.RemainingLength);
            }

            return MqttCommand.Create(header, data);
        }

        public System.Threading.Tasks.Task Send(MqttCommand command)
        {
            return Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Debug.WriteLine("SEND: {0} ({1})", command.CommandMessage, command.MessageId);

                byte[] buffer = command.ToByteArray();

                int start = 0;

                lock (_socketWriteLock)
                {
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
                }
            });
        }

        public void Start(Action<MqttCommand> onIncomingMessage)
        {
            _recvThread = new Thread(() =>
                {
                    ReceiveLoop(onIncomingMessage);
                });

            _recvThread.Start();
        }

        public bool IsConnected
        {
            get
            {
                return _socket.Connected;
            }
        }

        public void Dispose()
        {
            using (_socket) { }
        }

        private void ReceiveLoop(Action<MqttCommand> recv)
        {
            while (true)
            {
                MqttCommand command = ReadCommand();

                if (recv != null)
                {
                    recv(command);
                }
            }
        }
    }
}
