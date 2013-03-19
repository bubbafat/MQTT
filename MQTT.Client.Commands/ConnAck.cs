using MQTT.Types;

namespace MQTT.Commands
{
    public class ConnAck : MqttCommand
    {
        readonly byte[] _variableHeader = new byte[2];

        public ConnAck()
            : this(new FixedHeader(CommandMessage.CONNACK), null)
        {
        }

        public ConnAck(FixedHeader header, byte[] data)
            : base(header)
        {
            if (data != null)
            {
                if (header.RemainingLength != data.Length)
                {
                    throw new ProtocolException(CommandMessage, "The declared and actual data lengths did not match");
                }

                if (header.RemainingLength != 2)
                {
                    throw new ProtocolException(CommandMessage, "The declared data length must be 2");
                }

                Result = (ConnectionAckResult)data[1];
            }
        }

        protected override byte[] VariableHeader
        {
            get
            {
                return _variableHeader;
            }
        }

        public ConnectionAckResult Result
        {
            get
            {
                return (ConnectionAckResult)_variableHeader[1];
            }
            set
            {
                _variableHeader[1] = (byte)value;
            }
        }
    }

    public enum ConnectionAckResult
    {
        Accepted = 0,
        UnacceptableProtocol = 1,
        IdentifierRejected = 2,
        ServerUnavailable = 3,
        BadUsernameOrPassword = 4,
        NotAuthorized = 5,
    }
}
