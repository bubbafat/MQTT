using System;
using System.Net;
using MQTT.Broker.Network;

namespace MQTT.Broker
{
    public sealed class MqttBroker : IDisposable
    {
        readonly IConnectionManager _manager;

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
