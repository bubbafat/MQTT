using System;

namespace MQTT.Broker.Network
{
    public class ListenerExceptionEventArgs : EventArgs
    {
        public ListenerExceptionEventArgs(Exception ex)
        {
            Exception = ex;
        }

        public Exception Exception { get; private set; }
    }
}
