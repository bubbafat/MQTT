using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;

namespace MQTT.Broker.Network
{
    internal interface IActiveConnectionManager
    {
        void Register(NamedConnection connection);

        void Start();
        void Stop();

        void Send(string client, Publish publish);
        void Disconnect(NamedConnection namedConnection);
    }
}
