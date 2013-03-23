using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Broker.Network;
using System.Threading.Tasks;

namespace MQTT.Broker.StateMachines
{
    class PingReceived : StateMachine
    {
        MqttCommand _command;
        NamedConnection _connection;

        public PingReceived(MqttCommand cmd, NamedConnection connection)
        {
            _command = cmd;
            _connection = connection;
        }

        public override void Start()
        {
            _connection.Send(new PingResp());
            _connection.Complete(_command);
        }
    }
}
