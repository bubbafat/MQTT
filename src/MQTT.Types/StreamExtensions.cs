using System;
using System.IO;
using System.Threading.Tasks;

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

            throw new InvalidOperationException("Unable to read the required length from the string");
        }

        public static Task<byte[]> ReadBytesOrFailAsync(this Stream stream, int length)
        {
            return Task.Factory.StartNew(() =>
                {
                    var result = new byte[length];
                    int remaining = length;
                    int readStart = 0;
                    while (remaining > 0)
                    {
                        int actuallyRead = stream.ReadAsync(result, readStart, remaining).Await().Result;
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
            var bytes = new byte[2];
            var result = stream.ReadAsync(bytes, 0, 2).Await();
            if (result.IsCompleted)
            {
                return (ushort)((bytes[0] << 8) + bytes[1]);                
            }

            if (result.IsCanceled)
            {
                throw new TaskCanceledException();
            }

            if (result.IsFaulted && result.Exception != null)
            {
                throw result.Exception;
            }

            throw new InvalidOperationException("Reading a ushort faulted but no exception was provided.");
        }

        public static byte[] ReadRest(this Stream stream)
        {
            return stream.ReadBytesOrFailAsync((int)(stream.Length - stream.Position)).Await().Result;
        }
    }
}
