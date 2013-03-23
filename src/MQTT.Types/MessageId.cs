using System;
using System.Globalization;
using System.IO;

namespace MQTT.Types
{
    public struct MessageId : IComparable<MessageId>
    {
        public MessageId(ushort value)
        {
            Value = value;
        }

        public readonly ushort Value;

        public byte[] ToByteArray()
        {
            var lsb = (byte)(Value & 0x00FF);
            var msb = (byte)((Value & 0xFF00) >> 8);

            return new [] { msb, lsb };
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
            return id1.Equals(id2);
        }

        public static bool operator != (MessageId id1, MessageId id2)
        {
            return !id1.Equals(id2);
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is MessageId)
            {
                var id = (MessageId) obj;
                return id.Value == Value;
            }

            return false;
        }

        public static readonly MessageId Any = new MessageId(0);
    }
}
