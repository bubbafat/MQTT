using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
