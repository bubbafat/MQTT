using System.Collections.Generic;

namespace MQTT.Types
{
    public class FixedHeader
    {
        public CommandMessage Message { get; set; }
        public bool Duplicate { get; set; }
        public QualityOfService QualityOfService { get; set; }
        public bool Retain { get; set; }
        public int RemainingLength { get; set; }

        private FixedHeader()
        {
        }

        public FixedHeader(CommandMessage message)
        {
            Message = message;
        }

        public byte[] ToByteArray()
        {
            int firstByte = 0;
            firstByte |= ((int)Message << 4);
            if (Duplicate)
            {
                firstByte |= 0x8;
            }

            firstByte |= ((int)QualityOfService << 1);
            if (Retain)
            {
                firstByte++;
            }

            var bytes = new List<byte>();
            bytes.Add((byte)firstByte);
            bytes.AddRange(VariableLengthInteger.ToByteArray(RemainingLength));

            return bytes.ToArray();
        }

        public static FixedHeader Load(NetworkConnection connection)
        {
            byte firstByte = connection.ReadBytesOrFailAsync(1).Await().Result[0];

            var header = new FixedHeader
                {
                    Message = (CommandMessage) ((firstByte & 0xF0) >> 4),
                    Duplicate = (firstByte & 0x8) == 0x8,
                    QualityOfService = (QualityOfService) ((firstByte & 0x6) >> 1),
                    Retain = (firstByte & 0x1) == 0x1,
                    RemainingLength = VariableLengthInteger.Load(connection)
                };

            return header;
        }
    }
}
