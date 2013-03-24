using System;

namespace MQTT.Types
{
    public static class VariableLengthInteger
    {
        public static byte[] ToByteArray(int value)
        {
            if (value < 0 || value > 268435455)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            int length = 1;
            if (value > 127) length++;
            if (value > 16383) length++;
            if (value > 2097151) length++;

            var targetArray = new byte[length];

            int index = 0;
            do
            {
                int digit;
                value = Math.DivRem(value, 128, out digit);
                if (value > 0)
                {
                    digit = digit | 0x80;
                }

                targetArray[index++] = (byte)digit;
            }
            while (value > 0);

            return targetArray;
        }

        internal static int Load(NetworkConnection connection)
        {
            int result = 0;
            int multiplier = 1;
            int bytesRead = 0;
            int digit;

            do
            {
                digit = connection.Stream.ReadBytesOrFailAsync(1).Await().Result[0];
                result += (digit & 127) * multiplier;
                multiplier *= 128;
                bytesRead++;
            } while ((digit & 128) != 0 && (bytesRead < 4));

            return result;
        }
    }
}
