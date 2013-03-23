using MQTT.Commands;
using MQTT.Broker.Network;

namespace MQTT.Broker.StateMachines
{
    class PingReceived : StateMachine
    {
        readonly MqttCommand _command;
        readonly NamedConnection _connection;

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
