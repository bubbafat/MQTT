using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using MQTT.Types;

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

        public Task Send(ClientCommand command)
        {
            return Task.Factory.StartNew(() =>
                {
                    EnqueueResponse(command);
                });
        }

        public Task<ClientCommand> ReceiveUnsolicited()
        {
            return Task<ClientCommand>.Factory.StartNew(() =>
                {
                    while(true)
                    {
                        ClientCommand cmd;
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

        public Task<ClientCommand> WaitForCommand(CommandMessage message, MessageId messageId, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }


        public event MessageReceivedCallback OnMessageReceived;
    }
}
