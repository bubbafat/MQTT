using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MQTT.Types
{
    public static class SocketExtensions
    {
        public static Task<int> ReceiveAsync(
            this Socket socket, byte[] buffer, int offset, int size)
        {
            SocketFlags flags = SocketFlags.None;

            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginReceive(buffer, offset, size, flags, iar =>
            {
                var t = (TaskCompletionSource<int>)iar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try { t.TrySetResult(s.EndReceive(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);

            return tcs.Task;
        }

        public static Task<int> SendAsync(
            this Socket socket, byte[] buffer, int offset, int size)
        {
            SocketFlags flags = SocketFlags.None;

            System.Diagnostics.Debug.Write("TCP Send: ");
            System.Diagnostics.Debug.WriteLine(BitConverter.ToString(buffer).Replace("-", " "));

            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginSend(buffer, offset, size, flags, iar =>
            {
                var t = (TaskCompletionSource<int>)iar.AsyncState;
                var s = (Socket)t.Task.AsyncState;
                try { t.TrySetResult(s.EndReceive(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);

            return tcs.Task;
        }

        public static byte[] ReadBytes(this Socket socket, int count)
        {
            byte[] bytes = null;
            if (count > 0)
            {
                bytes = new byte[count];

                int start = 0;
                while (start < count)
                {
                    Task<int> read = socket.ReceiveAsync(bytes, start, count - start);
                    read.Wait();
                    if (read.IsFaulted)
                    {
                        throw read.Exception;
                    }

                    if (!read.IsCompleted)
                    {
                        throw new InvalidOperationException("Socket read finished by never completed");
                    }

                    start += read.Result;
                }
            }

            return bytes;
        }
    }
}
