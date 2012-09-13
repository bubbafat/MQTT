using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using MQTT.Types;
using MQTT.Domain;

namespace MQTT.Client.Tests
{
    class MockMqttBroker : IMqttBroker
    {
        bool _connected = false;
        ConcurrentQueue<MqttCommand> _incoming = new ConcurrentQueue<MqttCommand>();

        public void Connect(System.Net.IPEndPoint endpoint)
        {
            _connected = true;
        }

        public void Listen(System.Net.IPEndPoint endpoint)
        {
            _connected = true;
        }

        public void Disconnect()
        {
            _connected = false;
        }

        public Task Send(MqttCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
                    EnqueueResponse(command);
                });
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
    }
}
