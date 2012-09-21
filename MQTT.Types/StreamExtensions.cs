using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

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
                Task<int> read = stream.ReadAsync(result, readStart, remaining);
                read.Wait();
                
                if(read.IsFaulted)
                {
                    throw read.Exception;
                }

                if (read.IsCompleted)
                {
                    int actuallyRead = read.Result;
                    if (actuallyRead > 0)
                    {
                        remaining -= actuallyRead;
                        readStart += actuallyRead;
                    }
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
                        Task<int> read = stream.ReadAsync(result, readStart, remaining);
                        read.Wait();

                        if (read.IsFaulted)
                        {
                            throw read.Exception;
                        }

                        if (read.IsCompleted)
                        {
                            int actuallyRead = read.Result;
                            if (actuallyRead > 0)
                            {
                                remaining -= actuallyRead;
                                readStart += actuallyRead;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Unable to read the required length from the string");
                        }
                    }

                    return result;
                });
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

        public static Task<int> WriteAsync(
            this Stream stream, byte[] buffer, int offset, int size)
        {
            var tcs = new TaskCompletionSource<int>(stream);
            stream.BeginWrite(buffer, offset, size, iar =>
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
            byte[] bytes = ReadBytesOrFailAsync(stream, 2).Await<byte[]>().Result;
            return (ushort)((bytes[0] << 8) + bytes[1]);
        }

        public static byte[] ReadRest(this Stream stream)
        {
            return stream.ReadBytesOrFailAsync((int)(stream.Length - stream.Position)).Await<byte[]>().Result;
        }
    }
}
