using System;
using System.Net;
using System.Net.Sockets;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Types;
using System.Threading;

namespace MQTT.Domain
{
    public delegate void NetworkDisconnectedCallback(object sender, NetworkDisconenctedEventArgs e);

    public class NetworkDisconenctedEventArgs : EventArgs
    {
        public NetworkDisconenctedEventArgs(NetworkConnection connection, Exception exception = null)
        {
            Connection = connection;
            Exception = exception;
        }

        public NetworkConnection Connection { get; private set; }
        public Exception Exception { get; private set; }
    }


    public sealed class NetworkInterface : INetworkInterface
    {
        Thread _recvThread;
        readonly NetworkConnection _connection;
        readonly ICommandReader _reader;
        readonly ICommandWriter _writer;

        public NetworkInterface(ICommandReader reader, ICommandWriter writer)
        {
            _connection = new NetworkConnection();
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
            return _writer.Send(_connection, command);
        }

        public Task Connect(IPEndPoint endpoint)
        {
            return _connection.Connect(endpoint);
        }

        public void Start(Action<MqttCommand> onIncomingMessage)
        {
            _recvThread = new Thread(() => ReceiveLoop(onIncomingMessage));
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

        public event NetworkDisconnectedCallback OnNetworkDisconnected;

        private void NotifyOfDisconnect(NetworkConnection connection, Exception ex)
        {
            NetworkDisconnectedCallback ev = OnNetworkDisconnected;
            if (ev != null)
            {
                ev(this, new NetworkDisconenctedEventArgs(connection, ex));
            }
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

                NotifyOfDisconnect(_connection, ex);
            }
        }
    }
}
