using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MQTT.Domain
{
    public sealed class NewConnectionEventArgs : EventArgs
    {
        public NewConnectionEventArgs(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            Socket = socket;
        }

        public Socket Socket { get; private set; }
    }
}
