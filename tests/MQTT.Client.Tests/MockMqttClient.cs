using System;
using MQTT.Commands;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using MQTT.Domain;

namespace MQTT.Client.Tests
{
    class MockMqttClient : IMqttClient
    {
        private bool _connected;
        private bool _listening;
        readonly ConcurrentQueue<MqttCommand> _incoming = new ConcurrentQueue<MqttCommand>();

        public MockMqttClient()
        {
            SendPingResponses = true;
        }

        public Task Connect(System.Net.IPEndPoint endpoint)
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                _connected = true;
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public void Receive()
        {
            _listening = true;
        }

        public void Listen()
        {
            if (!_connected)
            {
                throw new InvalidOperationException("Listen called on non-connected client");
            }
            _listening = true;
        }

        public void Disconnect()
        {
            _connected = false;
        }

        public Task Send(MqttCommand command)
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                EnqueueResponse(command);
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public Task<MqttCommand> ReceiveUnsolicited()
        {
            return Task<MqttCommand>.Factory.StartNew(() =>
                {
                    while(true)
                    {
                        MqttCommand cmd;
                        if (_incoming.TryDequeue(out cmd))
                        {
                            return cmd;
                        }

                        Thread.Sleep(100);
                    }
                });
        }

        public bool IsConnected
        {
            get { return _connected; }
        }

        private void EnqueueResponse(MqttCommand command)
        {
            MessageReceivedCallback recv = OnMessageReceived;

            switch (command.CommandMessage)
            {
                case Types.CommandMessage.CONNECT:
                    if (recv != null)
                    {
                        recv(this, new ClientCommandEventArgs(new ConnAck()));
                    }
                    break;
                case Types.CommandMessage.PINGREQ:
                    if (SendPingResponses)
                    {
                        if (recv != null)
                        {
                            recv(this, new ClientCommandEventArgs(new PingResp()));
                        }
                    }
                    break;
                case Types.CommandMessage.DISCONNECT:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            // nothing
        }

        public event MessageReceivedCallback OnMessageReceived;
        public event NetworkDisconnectedCallback OnNetworkDisconnected;

        public void Start(Action<MqttCommand> onIncomingMessage)
        {
            // do nothing
        }

        public bool SendPingResponses { get; set; }
    }
}
