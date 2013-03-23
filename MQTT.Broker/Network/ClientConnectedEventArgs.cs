using System;

namespace MQTT.Broker.Network
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(string clientId)
        {
            ClientId = clientId;
        }

        public string ClientId { get; private set; }
    }
}
