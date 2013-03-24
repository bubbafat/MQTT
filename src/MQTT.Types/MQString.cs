using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public static class MqString
    {
        public static string FromStream(Stream data)
        {
            var length = data.ReadUint16();
            return Encoding.UTF8.GetString(data.ReadBytesOrFailAsync(length).Await().Result);
        }

        public static byte[] ToByteArray(string str)
        {
            var lsb = (byte)(str.Length & 0x000000FF);
            var msb = (byte)((str.Length & 0x0000FF00) >> 8);

            var bytes = new List<byte>{msb, lsb};
            bytes.AddRange(Encoding.UTF8.GetBytes(str));

            return bytes.ToArray();
        }

        public static string FromBytes(byte[] bytes)
        {
            using (var s = new MemoryStream(bytes))
            {
                return FromStream(s);
            }
        }
    }
}
