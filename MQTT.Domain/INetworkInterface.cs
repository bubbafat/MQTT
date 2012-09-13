using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MQTT.Commands;
using System.Threading.Tasks;

namespace MQTT.Domain
{
    public interface INetworkInterface : IDisposable
    {
        void Disconnect();

        Task Send(MqttCommand command);

        bool IsConnected { get; }
    }
}
