using System;

namespace MQTT.Broker.Network
{
    public delegate void MessageReceived(object sender, MessageReceivedEventArgs e);
    public delegate void ClientDisconnected(object sender, ClientDisconnectedEventArgs e);
    public delegate void ClientConnected(object sender, ClientConnectedEventArgs e);
    public delegate void ListenerException(object sender, ListenerExceptionEventArgs e);

    public interface IConnectionManager : IDisposable
    {
        void Start();
        void Stop();

        event ListenerException OnListenerException;
    }
}
