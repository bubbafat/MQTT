using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public static class StreamExtensions
    {
        public static byte[] ReadBytesOrFail(this Stream stream, int length)
        {
            byte[] result = new byte[length];
            int remaining = length;
            int readStart = 0;
            while (remaining > 0)
            {
                int actuallyRead = stream.Read(result, readStart, remaining);
                if (actuallyRead > 0)
                {
                    remaining -= actuallyRead;
                    readStart += actuallyRead;
                }
                else
                {
                    throw new InvalidOperationException("Unable to read the required length from the string");
                }
            }

            return result;
        }

        public static ushort ReadUint16(this Stream stream)
        {
            byte[] bytes = ReadBytesOrFail(stream, 2);
            return (ushort)((bytes[0] << 8) + bytes[1]);
        }

        public static byte[] ReadRest(this Stream stream)
        {
            return stream.ReadBytesOrFail((int)(stream.Length - stream.Position));
        }
    }
}
