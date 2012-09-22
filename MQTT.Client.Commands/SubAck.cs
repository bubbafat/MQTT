using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class SubAck : MqttCommand
    {
        List<QualityOfService> _grants = new List<QualityOfService>();

        public SubAck(MessageId messageId)
            : base(new FixedHeader(CommandMessage.SUBACK))
        {
            MessageId = messageId;
        }

        protected override byte[] VariableHeader
        {
            get
            {
                return MessageId.ToByteArray();
            }
        }

        protected override byte[] Payload
        {
            get
            {
                List<byte> bytes = new List<byte>();
                foreach (QualityOfService qos in Grants)
                {
                    bytes.Add((byte)qos);
                }

                return bytes.ToArray();
            }
        }

        public SubAck(FixedHeader header, byte[] data)
            : base(header)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            using (MemoryStream stream = new MemoryStream(data))
            {
                _grants = new List<QualityOfService>();

                MessageId = MessageId.FromStream(stream);

                while (stream.Position < stream.Length)
                {
                    byte qosByte = stream.ReadByteOrFail();
                    qosByte = (byte)(qosByte & 0x03); // 00000011
                    _grants.Add((QualityOfService)qosByte);
                }
            }
        }

        public List<QualityOfService> Grants
        {
            get
            {
                return _grants;
            }
        }
    }
}
