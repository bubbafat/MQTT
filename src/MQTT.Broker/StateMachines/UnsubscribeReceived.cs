﻿using System;
using MQTT.Commands;
using MQTT.Broker.Network;

namespace MQTT.Broker.StateMachines
{
    class UnsubscribeReceived : StateMachine
    {
        MqttCommand _command;
        NamedConnection _connection;

        public UnsubscribeReceived(MqttCommand cmd, NamedConnection connection)
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
