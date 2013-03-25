using System;

namespace MQTT.Domain
{
    public class KeepAliveExpiredEventArgs : EventArgs
    {
        public KeepAliveExpiredEventArgs(DateTime lastHeard, ushort keepAliveSeconds)
        {
            ExpectedResponseTime = lastHeard;
            KeepAliveSeconds = keepAliveSeconds;
        }

        public DateTime ExpectedResponseTime { get; private set; }
        public ushort KeepAliveSeconds { get; private set; }
    }
}
