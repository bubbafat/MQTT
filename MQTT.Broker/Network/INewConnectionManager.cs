using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MQTT.Broker.Network
{
    internal interface INewConnectionManager
    {
        void Start();
        void Stop();

        void Process(TcpClient client);
    }
}
