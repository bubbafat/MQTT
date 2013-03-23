using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MQTT.Broker.Network
{
    sealed class TcpClientManager : IConnectionManager
    {
        TcpListener _listener;
        Thread _listeningThread;
        ManualResetEvent _stopThread;
        readonly INewConnectionManager _newConnectionManager;

        readonly object _lock = new object();

        public TcpClientManager(INewConnectionManager newConnectionManager)
        {
            _newConnectionManager = newConnectionManager;
        }

        public void Start()
        {
            lock (_lock)
            {
                Stop();

                _newConnectionManager.Start();

                var endpoint = new IPEndPoint(IPAddress.IPv6Any, 1883);
                _listener = new TcpListener(endpoint);
                // accept both ipv4 and ipv6 connections
                _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                _listener.Start();

                _stopThread = new ManualResetEvent(false);
                _listeningThread = new Thread(ListeningLoop);
                _listeningThread.Start(_stopThread);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                _newConnectionManager.Stop();

                if (_stopThread != null)
                {
                    _stopThread.Set();
                }

                if (_listener != null)
                {
                    try
                    {
                        _listener.Stop();
                    }
                    catch (SocketException)
                    {
                        // TODO don't just eat it
                    }

                    _listener = null;
                }
            }
        }

        public event ListenerException OnListenerException;

        public void Dispose()
        {
            Stop();
            using (_stopThread) { }
        }

        private void ListeningLoop(object stopEventArg)
        {
            if(stopEventArg == null)
            {
                throw new ArgumentNullException("stopEventArg");
            }

            var stopEvent = (ManualResetEvent)stopEventArg;

            var cancel = Task.Factory.StartNew(() =>
            {
                stopEvent.WaitOne();
            }, TaskCreationOptions.LongRunning);

            try
            {
                while (true)
                {
                    Task<TcpClient> conn = ReceiveAsync();

                    switch (Task.WaitAny(new[] { conn, cancel }))
                    {
                        case 0:
                            HandleConnection(conn);
                            break;
                        case 1:
                            return;
                        default:
                            throw new NotImplementedException("WTF????");
                    }

                }
            }
            catch (Exception ex)
            {
                ListenerException callback = OnListenerException;
                if (callback != null)
                {
                    callback(this, new ListenerExceptionEventArgs(ex));
                }
            }
        }

        private void HandleConnection(Task<TcpClient> conn)
        {
            switch (conn.Status)
            {
                case TaskStatus.Faulted:
                    if (conn.Exception != null)
                    {
                        throw conn.Exception;
                    }
                    throw new InvalidOperationException("New connection faulted but there was no exception provided.");
                case TaskStatus.Canceled:
                    return;
                case TaskStatus.RanToCompletion:
                    var client = conn.Result;
                    if (client.Connected)
                    {
                        _newConnectionManager.Process(client);
                    }
                    break;
                default:
                    throw new NotImplementedException(string.Format("Unexpected status: {0}", conn.Status));
            }
        }

        private Task<TcpClient> ReceiveAsync()
        {
            var tcs = new TaskCompletionSource<TcpClient>(_listener);
            _listener.BeginAcceptTcpClient(iar =>
            {
                var t = (TaskCompletionSource<TcpClient>)iar.AsyncState;
                var l = (TcpListener)t.Task.AsyncState;
                try { t.TrySetResult(l.EndAcceptTcpClient(iar)); }
                catch (Exception exc) { t.TrySetException(exc); }
            }, tcs);

            return tcs.Task;
        }
    }
}
