﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MQTT.Types
{
    public static class VariableLengthInteger
    {
        public static byte[] ToByteArray(int X)
        {
            if (X < 0 || X > 268435455)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            int length = 1;
            if (X > 127) length++;
            if (X > 16383) length++;
            if (X > 2097151) length++;

            byte[] targetArray = new byte[length];

            int index = 0;
            do
            {
                int digit;
                X = Math.DivRem(X, 128, out digit);
                if (X > 0)
                {
                    digit = digit | 0x80;
                }

                targetArray[index++] = (byte)digit;
            }
            while (X > 0);

            return targetArray;
        }

        internal static int FromSocket(System.Net.Sockets.Socket socket)
        {
            int result = 0;
            int multiplier = 1;
            int digit = 0;
            int bytesRead = 0;

            do
            {
                digit = socket.ReadBytes(1)[0];
                result += (digit & 127) * multiplier;
                multiplier *= 128;
                bytesRead++;
            } while ((digit & 128) != 0 && (bytesRead < 4));

            return result;
        }
    }
}
