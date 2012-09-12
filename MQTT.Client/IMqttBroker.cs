
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using System.Net;

namespace MQTT.Client
{
    public interface IMqttBroker : IDisposable
    {
        void Connect(IPEndPoint endpoint);
        void Disconnect();
        
        Task SendCommandAsync(ClientCommand command);
        Task<ClientCommand> ReceiveAsync();

        bool IsConnected { get; }
        DateTime LastHeard { get; }
    }
}
