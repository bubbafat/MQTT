using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public static class MQString
    {
        public static string FromStream(Stream data)
        {
            ushort length = data.ReadUint16();
            return Encoding.UTF8.GetString(data.ReadBytesOrFailAsync(length).Await<byte[]>().Result);
        }

        public static byte[] ToByteArray(string str)
        {
            List<byte> bytes = new List<byte>(Encoding.UTF8.GetBytes(str));

            byte lsb = (byte)(str.Length & 0x000000FF);
            byte msb = (byte)((str.Length & 0x0000FF00) >> 8);

            bytes.Insert(0, lsb);
            bytes.Insert(0, msb);

            return bytes.ToArray();
        }

        public static string FromBytes(byte[] bytes)
        {
            using (MemoryStream s = new MemoryStream(bytes))
            {
                return FromStream(s);
            }
        }
    }
}
