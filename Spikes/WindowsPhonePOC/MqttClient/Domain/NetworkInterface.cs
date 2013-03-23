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
        NetworkConnection _connection;
        ICommandReader _reader;
        ICommandWriter _writer;

        public NetworkInterface(ICommandReader reader, ICommandWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public void Disconnect()
        {
            _recvThread.Abort();
            _connection.Disconnect();
        }

        public MqttCommand ReadCommand()
        {
            return _reader.Read(_connection);
        }

        public Task Send(MqttCommand command)
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                _writer.Send(_connection, command);
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public void Start(TcpClient client, Action<MqttCommand> onIncomingMessage)
        {
            _connection= new NetworkConnection(client);

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
                return _connection.Connected;
            }
        }

        public void Dispose()
        {
            // do nothing
        }

        private void ReceiveLoop(Action<MqttCommand> recv)
        {
            try
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
            catch (Exception ex)
            {
                if (_connection.Connected)
                {
                    _connection.Disconnect();
                }
                // swallow and let the thread die naturally
            }
        }
    }
}
