using System;

namespace MQTT.Broker.Network
{
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public ClientDisconnectedEventArgs(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; private set; }
    }
}
