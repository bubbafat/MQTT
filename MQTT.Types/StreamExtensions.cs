using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace MQTT.Types
{
    public static class StreamExtensions
    {
        public static byte ReadByteOrFail(this Stream stream)
        {
            int read = stream.ReadByte();
            if (read >= 0)
            {
                return (byte)read;
            }
            else
            {
                throw new InvalidOperationException("Unable to read the required length from the string");
            }
        }

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

        public static Task<byte[]> ReadBytesOrFailAsync(this Stream stream, int length)
        {
            return Task.Factory.StartNew<byte[]>(() =>
                {
                    byte[] result = new byte[length];
                    int remaining = length;
                    int readStart = 0;
                    while (remaining > 0)
                    {
                        int actuallyRead = stream.ReadAsync(result, readStart, remaining).Await<int>().Result;
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
                }, TaskCreationOptions.LongRunning);
        }


        public static Task<int> ReadAsync(
            this Stream stream, byte[] buffer, int offset, int size)
        {
            var tcs = new TaskCompletionSource<int>(stream);
            stream.BeginRead(buffer, offset, size, iar =>
            {
                var t = (TaskCompletionSource<int>)iar.AsyncState;
                var s = (Stream)t.Task.AsyncState;
                try { t.TrySetResult(s.EndRead(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);

            return tcs.Task;
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
