using System;
using System.IO;
using System.Threading.Tasks;

namespace MQTT.Types
{
    public static class StreamExtensions
    {
        public static Task<byte[]> ReadBytesOrFailAsync(this Stream stream, int length)
        {
            return Task.Factory.StartNew(() =>
                {
                    var result = new byte[length];
                    int remaining = length;
                    int readStart = 0;
                    while (remaining > 0)
                    {
                        var task = stream.ReadAsync(result, readStart, remaining).Await();
                        if (task.IsCompleted)
                        {
                            int actuallyRead = task.Result;
                            if (actuallyRead > 0)
                            {
                                remaining -= actuallyRead;
                                readStart += actuallyRead;
                            }
                            else
                            {
                                throw new InvalidOperationException("Unable to read the required length from the stream");
                            }
                        }
                        else
                        {
                            if (task.IsFaulted)
                            {
                                if (task.Exception != null)
                                {
                                    throw task.Exception;
                                }
                             
                                throw new InvalidOperationException(
                                    "The read operation faulted but did not return exception details.");
                            }

                            throw new OperationCanceledException("The read operation was cancelled");
                        }
                    }

                    return result;
                });
        }

        public static ushort ReadUint16(this Stream stream)
        {
            var bytes = stream.ReadBytesOrFailAsync(2).Await().Result;
            return (ushort) ((bytes[0] << 8) + bytes[1]);
        }

        public static byte[] ReadRest(this Stream stream)
        {
            return stream.ReadBytesOrFailAsync((int)(stream.Length - stream.Position)).Await().Result;
        }
    }
}
