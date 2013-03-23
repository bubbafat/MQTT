using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using MQTT.Broker.Network;

namespace MQTT.Broker.StateMachines
{
    class DisconnectReceived : StateMachine
    {
        MqttCommand _command;
        NamedConnection _connection;

        public DisconnectReceived(MqttCommand cmd, NamedConnection connection)
        {
            _command = cmd;
            _connection = connection;
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }
    }
}
