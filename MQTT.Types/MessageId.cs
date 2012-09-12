using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public class MessageId
    {
        public MessageId(ushort value)
        {
            Value = value;
        }

        public ushort Value
        {
            get;
            set;
        }

        public byte[] ToByteArray()
        {
            byte lsb = (byte)(Value & 0x00FF);
            byte msb = (byte)((Value& 0xFF00) >> 8);

            return new byte[] { msb, lsb };
        }

        public static MessageId FromStream(Stream stream)
        {
            return new MessageId(stream.ReadUint16());
        }
    }
}
