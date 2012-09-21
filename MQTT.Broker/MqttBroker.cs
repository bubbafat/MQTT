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
using MQTT.Broker.Network;

namespace MQTT.Broker
{
    public sealed class MqttBroker : IDisposable
    {
        IConnectionManager _manager;

        public MqttBroker(IConnectionManager connectionManager)
        {
            _manager = connectionManager;
        }

        public void Listen(IPEndPoint endpoint)
        {
            _manager.Start();
        }

        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}
