using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public class SubAck : ClientCommand
    {
        List<QualityOfService> _grants;

        public SubAck()
            : this(new FixedHeader(CommandMessage.SUBACK), null)
        {
        }

        public SubAck(FixedHeader header, byte[] data)
            : base(header)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                _grants = new List<QualityOfService>();

                MessageId = MessageId.FromStream(stream);

                while (stream.Position < stream.Length)
                {
                    byte qosByte = stream.ReadBytesOrFail(1)[0];
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
