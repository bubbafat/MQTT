using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public class Connect : ClientCommand
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
                using (MemoryStream stream = new MemoryStream(data))
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

        private void LoadPayload(Stream data)
        {
            ClientIdentifier = MQString.FromStream(data);
            if (Details.ConnectFlags.Will)
            {
                WillTopic = MQString.FromStream(data);
                WillMessage = MQString.FromStream(data);
            }

            if (Details.ConnectFlags.UserName)
            {
                UserName = MQString.FromStream(data);
            }

            if (Details.ConnectFlags.Password)
            {
                Password = MQString.FromStream(data);
            }
        }

        protected override byte[] VariableHeader
        {
            get
            {
                return Details.ToByteArray();
            }
        }

        protected override byte[] Payload
        {
            get
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(MQString.ToByteArray(ClientIdentifier));

                if (Details.ConnectFlags.Will)
                {
                    bytes.AddRange(MQString.ToByteArray(WillTopic));
                    bytes.AddRange(MQString.ToByteArray(WillMessage));
                }

                if (Details.ConnectFlags.UserName)
                {
                    bytes.AddRange(MQString.ToByteArray(UserName));
                }
                if (Details.ConnectFlags.Password)
                {
                    bytes.AddRange(MQString.ToByteArray(Password));
                }

                return bytes.ToArray();
            }
        }

        public V3ConnectVariableHeader Details
        {
            get;
            private set;
        }

        public string ClientIdentifier
        {
            get;
            set;
        }

        public string UserName
        {
            get;
            set;
        }
        
        public string Password 
        { 
            get; 
            set; 
        }

        public string WillTopic
        {
            get;
            set;
        }

        public string WillMessage
        {
            get;
            set;
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
            result |= (byte)((int)WillQoS << 3);
            if (Will) result |= 0x04;
            if (CleanSession) result |= 0x02;

            return new byte[] { result };
        }

        internal static ConnectFlags FromStream(Stream stream)
        {
            byte b = stream.ReadBytesOrFail(1)[0];
            ConnectFlags flags = new ConnectFlags();

            flags.UserName = (b & 0x80) == 0x80;
            flags.Password = (b & 0x40) == 0x40;
            flags.WillRetain = (b & 0x20) == 0x20;
            flags.WillQoS = (QualityOfService)((b & 0x18) >> 3);
            flags.Will = (b & 0x04) == 0x04;
            flags.CleanSession = (b & 0x02) == 0x02;

            return flags;
        }
    }

    public class V3ConnectVariableHeader
    {
        public string ProtocolName { get; private set; }
        public byte Protocolversion { get; private set; }
        public ConnectFlags ConnectFlags { get; private set; }
        public ushort KeepAliveTimer { get; set; }

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

        public byte[] ToByteArray()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(MQString.ToByteArray(ProtocolName));
            bytes.Add(Protocolversion);
            bytes.AddRange(ConnectFlags.ToByteArray());

            byte lsb = (byte)(KeepAliveTimer & 0x00FF);
            byte msb = (byte)((KeepAliveTimer & 0xFF00) >> 8);

            bytes.Add(msb);
            bytes.Add(lsb);

            return bytes.ToArray();
        }

        internal static V3ConnectVariableHeader FromStream(Stream stream)
        {
            V3ConnectVariableHeader header = new V3ConnectVariableHeader();
            header.ProtocolName = MQString.FromStream(stream);
            header.Protocolversion = (byte)stream.ReadByte();
            header.ConnectFlags = ConnectFlags.FromStream(stream);
            header.KeepAliveTimer = stream.ReadUint16();

            return header;
        }
    }
}
