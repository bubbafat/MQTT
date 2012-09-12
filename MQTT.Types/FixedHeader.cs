using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

            List<byte> bytes = new List<byte>();
            bytes.Add((byte)firstByte);
            bytes.AddRange(VariableLengthInteger.ToByteArray(RemainingLength));

            return bytes.ToArray();
        }

        public static FixedHeader FromSocket(System.Net.Sockets.Socket socket)
        {
            byte firstByte = socket.ReadBytes(1)[0];

            FixedHeader header = new FixedHeader();
            header.Message = (CommandMessage)((firstByte & 0xF0) >> 4);
            header.Duplicate = (firstByte & 0x8) == 0x8;
            header.QualityOfService = (QualityOfService)((firstByte & 0x6) >> 1);
            header.Retain = (firstByte & 0x1) == 0x1;

            header.RemainingLength = VariableLengthInteger.FromSocket(socket);

            return header;
        }
    }
}
