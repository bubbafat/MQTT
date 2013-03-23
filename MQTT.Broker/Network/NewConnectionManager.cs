using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MQTT.Types;
using System.Threading;
using System.Threading.Tasks;
using MQTT.Broker.StateMachines;

namespace MQTT.Broker.Network
{
    sealed class NewConnectionManager : INewConnectionManager, IDisposable
    {
        readonly BlockingQueue<NetworkConnection> _incomingConnections = new BlockingQueue<NetworkConnection>();
        readonly IActiveConnectionManager _activeConnectionManager;
        Thread _processingThread;
        ManualResetEvent _stopThread;
        readonly object _lock = new object();

        public NewConnectionManager(IActiveConnectionManager activeConnectionManager)
        {
            _activeConnectionManager = activeConnectionManager;
        }

        public void Start()
        {
            lock (_lock)
            {
                Stop();

                _activeConnectionManager.Start();

                _stopThread = new ManualResetEvent(false);
                _processingThread = new Thread(NewConnectionLoop);
                _processingThread.Start(_stopThread);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_stopThread != null)
                {
                    _stopThread.Set();
                }

                _activeConnectionManager.Stop();
            }
        }

        public void Process(TcpClient client)
        {
            _incomingConnections.Enqueue(new NetworkConnection(client));
        }

        private void NewConnectionLoop(object stopThreadArg)
        {
            if (stopThreadArg == null)
            {
                throw new ArgumentNullException("stopThreadArg");
            }

            var stopThread = (ManualResetEvent)stopThreadArg;

            var stop = Task.Factory.StartNew(() =>
                {
                    stopThread.WaitOne();
                }, TaskCreationOptions.LongRunning);

            var pendingConnects = new List<Task<NamedConnection>>();

            while (true)
            {
                Task<NetworkConnection> conn = Task.Factory.StartNew(() => _incomingConnections.Dequeue(), TaskCreationOptions.LongRunning);

                var allTasks = new List<Task> {conn, stop};
                allTasks.AddRange(pendingConnects);

                int index = Task.WaitAny(allTasks.ToArray());
                switch (index)
                {
                    case 0:
                        pendingConnects.Add(HandleNewConnection(conn));
                        break;
                    case 1:
                        return;
                    default:
                        var result = (Task<NamedConnection>)allTasks[index];
                        pendingConnects.Remove(result);
                        ConnectFinished(result);
                        break;
                }
            }
        }

        private void ConnectFinished(Task<NamedConnection> task)
        {
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    // TODO: Log the fault and continue
                    return;
                case TaskStatus.Canceled:
                    // TODO: Log the cancel and continue
                    return;
                case TaskStatus.RanToCompletion:
                    _activeConnectionManager.Register(task.Result);
                    break;
                default:
                    throw new NotImplementedException("homey don\'t play that");
            }

        }

        private Task<NamedConnection> HandleNewConnection(Task<NetworkConnection> conn)
        {

            switch (conn.Status)
            {
                case TaskStatus.Faulted:
                    if (conn.Exception != null)
                    {
                        throw conn.Exception;
                    }
                    throw new InvalidOperationException("New connection faulted but there was no exception provided.");
                case TaskStatus.RanToCompletion:
                    var connection = conn.Result;
                    return Task.Factory.StartNew(() =>
                        {
                            var connRecv = BrokerFactory.Get<ConnectReceive>();
                            return connRecv.Run(connection);
                        });
                default:
                    throw new NotImplementedException("say what?");                    
            }
        }

        public void Dispose()
        {
            Stop();
            using(_stopThread) { }
        }
    }
}
