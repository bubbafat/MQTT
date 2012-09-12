using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Types;
using System.Threading.Tasks;
using MQTT.Client.Commands;

namespace MQTT.Client
{
    public class MqttNetworkBroker : IMqttBroker
    {
        Socket _socket;

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            _socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(endpoint);
        }

        public void Disconnect()
        {
            _socket.Close();
        }

        public System.Threading.Tasks.Task SendCommandAsync(Commands.ClientCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
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

        public System.Threading.Tasks.Task<Commands.ClientCommand> ReceiveAsync()
        {
            return Task<Commands.ClientCommand>.Factory.StartNew(() =>
                {
                    FixedHeader header = FixedHeader.FromSocket(_socket);
                    byte[] data = null;

                    if(header.RemainingLength > 0)
                    {
                        data = _socket.ReadBytes(header.RemainingLength);
                    }

                    return ClientCommand.Create(header, data);
                });
        }

        public bool IsConnected
        {
            get { return _socket.Connected; }
        }

        public DateTime LastHeard
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
            using (_socket) { }
        }
    }
}
