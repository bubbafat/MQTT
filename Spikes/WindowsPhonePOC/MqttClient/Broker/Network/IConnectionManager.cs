using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MQTT.Broker.Network
{
    public delegate void MessageReceived(object sender, MessageReceivedEventArgs args);
    public delegate void ClientDisconnected(object sender, ClientDisconnectedEventArgs args);
    public delegate void ClientConnected(object sender, ClientConnectedEventArgs args);
    public delegate void ListenerException(object sender, ListenerExceptionEventArgs args);

    public interface IConnectionManager : IDisposable
    {
        void Start();
        void Stop();

        event ListenerException OnListenerException;
    }
}
