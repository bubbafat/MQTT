using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace MQTT.Client.Tests
{
    class MockMqttBroker : IMqttBroker
    {
        bool _connected = false;
        ConcurrentQueue<ClientCommand> _incoming = new ConcurrentQueue<ClientCommand>();

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            _connected = true;
        }

        public void Disconnect()
        {
            _connected = false;
        }

        public Task SendCommandAsync(ClientCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
                    EnqueueResponse(command);
                });
        }

        public Task<ClientCommand> ReceiveAsync()
        {
            return Task<ClientCommand>.Factory.StartNew(() =>
                {
                    while(true)
                    {
                        ClientCommand cmd;
                        if (_incoming.TryDequeue(out cmd))
                        {
                            LastHeard = DateTime.UtcNow;
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

        public DateTime LastHeard
        {
            get;
            private set;
        }

        private void EnqueueResponse(ClientCommand command)
        {
            switch (command.CommandMessage)
            {
                case Types.CommandMessage.CONNECT:
                    _incoming.Enqueue(new ConnAck());
                    break;
                case Types.CommandMessage.DISCONNECT:
                case Types.CommandMessage.PUBCOMP:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            // nothing
        }
    }
}
