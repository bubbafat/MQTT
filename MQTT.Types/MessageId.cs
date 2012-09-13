using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public class MessageId : IComparable<MessageId>
    {
        public MessageId(ushort value)
        {
            Value = value;
        }

        public ushort Value
        {
            get;
            private set;
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

        public int CompareTo(MessageId other)
        {
            return Value.CompareTo(other.Value);
        }

        public static bool operator ==(MessageId id1, MessageId id2)
        {
            return id1.Value == id2.Value;
        }

        public static bool operator !=(MessageId id1, MessageId id2)
        {
            return id1.Value != id2.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            MessageId id = obj as MessageId;
            if (id == null)
            {
                return false;
            }

            return id.Value == Value;
        }

        public static readonly MessageId Any = new MessageId(0);
    }
}
