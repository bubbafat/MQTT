using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Domain;
using System.Timers;
using MQTT.Domain.StateMachines;
using MQTT.Types;
using System.Net;
using MQTT.Commands;
using System.Net.Sockets;
using System.Threading;

namespace MQTT.Broker
{
    public sealed class MqttBroker : IDisposable
    {
        ConnectionManager _connections = new ConnectionManager();
        CommandDrain _drain = new CommandDrain();

        Thread _listenThread;

        public MqttBroker()
        {
        }

        public void Listen(IPEndPoint endpoint)
        {
            _connections.Start();
            _drain.Start();

            _listenThread = new Thread(ListenLoop);
            _listenThread.Start();            
        }

        public void Dispose()
        {
            if (_listenThread != null)
            {
                _listenThread.Abort();
            }

            _connections.Stop();
            _drain.Stop();
        }

        public int ConnectionCount
        {
            get
            {
                return _connections.ConnectionCount;
            }
        }

        private void ListenLoop()
        {
            while (true)
            {
                IList<ActiveConnection> ready = _connections.Select();
                if (ready.Count > 0)
                {
                    IncomingDataQueue.Enqueue(ready);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
