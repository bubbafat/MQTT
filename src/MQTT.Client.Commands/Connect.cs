using System.Collections.Generic;
using System.IO;
using MQTT.Types;

namespace MQTT.Commands
{
    public class Connect : MqttCommand
    {
        public Connect(string clientId, ushort keepAlive)
            : this(new FixedHeader(CommandMessage.CONNECT), null)
        {
            ClientIdentifier = clientId;
            Details.KeepAliveTimer = keepAlive;
        }

        public Connect(FixedHeader header, byte[] data)
            : base(header)
        {
            if (data != null)
            {
                using (var stream = new MemoryStream(data))
                {
                    Details = V3ConnectVariableHeader.FromStream(stream);
                    LoadPayload(stream);
                }
            }
            else
            {
                Details = new V3ConnectVariableHeader(180, new ConnectFlags());
            }
        }

        protected override byte[] VariableHeader
        {
            get { return Details.ToByteArray(); }
        }

        protected override byte[] Payload
        {
            get
            {
                var bytes = new List<byte>();
                bytes.AddRange(MqString.ToByteArray(ClientIdentifier));

                if (Details.ConnectFlags.Will)
                {
                    bytes.AddRange(MqString.ToByteArray(WillTopic));
                    bytes.AddRange(MqString.ToByteArray(WillMessage));
                }

                if (Details.ConnectFlags.UserName)
                {
                    bytes.AddRange(MqString.ToByteArray(UserName));
                }
                if (Details.ConnectFlags.Password)
                {
                    bytes.AddRange(MqString.ToByteArray(Password));
                }

                return bytes.ToArray();
            }
        }

        public V3ConnectVariableHeader Details { get; private set; }

        public string ClientIdentifier { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string WillTopic { get; set; }

        public string WillMessage { get; set; }

        private void LoadPayload(Stream data)
        {
            ClientIdentifier = MqString.FromStream(data);
            if (Details.ConnectFlags.Will)
            {
                WillTopic = MqString.FromStream(data);
                WillMessage = MqString.FromStream(data);
            }

            if (Details.ConnectFlags.UserName)
            {
                UserName = MqString.FromStream(data);
            }

            if (Details.ConnectFlags.Password)
            {
                Password = MqString.FromStream(data);
            }
        }
    }

    public class ConnectFlags
    {
        public bool UserName { get; set; }
        public bool Password { get; set; }
        public bool WillRetain { get; set; }
        public QualityOfService WillQoS { get; set; }
        public bool Will { get; set; }
        public bool CleanSession { get; set; }

        public byte[] ToByteArray()
        {
            byte result = 0;

            if (UserName) result |= 0x80;
            if (Password) result |= 0x40;
            if (WillRetain) result |= 0x20;
            result |= (byte) ((int) WillQoS << 3);
            if (Will) result |= 0x04;
            if (CleanSession) result |= 0x02;

            return new[] {result};
        }

        internal static ConnectFlags FromStream(Stream stream)
        {
            byte b = stream.ReadByteOrFail();
            var flags = new ConnectFlags
                {
                    UserName = (b & 0x80) == 0x80,
                    Password = (b & 0x40) == 0x40,
                    WillRetain = (b & 0x20) == 0x20,
                    WillQoS = (QualityOfService) ((b & 0x18) >> 3),
                    Will = (b & 0x04) == 0x04,
                    CleanSession = (b & 0x02) == 0x02
                };

            return flags;
        }
    }

    public class V3ConnectVariableHeader
    {
        private V3ConnectVariableHeader()
        {
        }

        internal V3ConnectVariableHeader(ushort keepAlive, ConnectFlags flags)
        {
            ProtocolName = "MQIsdp";
            Protocolversion = 3;
            KeepAliveTimer = keepAlive;
            ConnectFlags = flags;
        }

        public string ProtocolName { get; private set; }
        public byte Protocolversion { get; private set; }
        public ConnectFlags ConnectFlags { get; private set; }
        public ushort KeepAliveTimer { get; set; }

        public byte[] ToByteArray()
        {
            var bytes = new List<byte>();
            bytes.AddRange(MqString.ToByteArray(ProtocolName));
            bytes.Add(Protocolversion);
            bytes.AddRange(ConnectFlags.ToByteArray());

            var lsb = (byte) (KeepAliveTimer & 0x00FF);
            var msb = (byte) ((KeepAliveTimer & 0xFF00) >> 8);

            bytes.Add(msb);
            bytes.Add(lsb);

            return bytes.ToArray();
        }

        internal static V3ConnectVariableHeader FromStream(Stream stream)
        {
            var header = new V3ConnectVariableHeader
                {
                    ProtocolName = MqString.FromStream(stream),
                    Protocolversion = stream.ReadByteOrFail(),
                    ConnectFlags = ConnectFlags.FromStream(stream),
                    KeepAliveTimer = stream.ReadUint16()
                };

            return header;
        }
    }
}