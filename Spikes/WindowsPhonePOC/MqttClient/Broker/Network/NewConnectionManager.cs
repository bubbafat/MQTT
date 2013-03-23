using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using MQTT.Commands;
using MQTT.Types;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTT.Domain.StateMachines;
using MQTT.Broker.StateMachines;
using MQTT.Domain;

namespace MQTT.Broker.Network
{
    class NewConnectionManager : INewConnectionManager
    {
        BlockingQueue<NetworkConnection> _incomingConnections = new BlockingQueue<NetworkConnection>();
        IActiveConnectionManager _activeConnectionManager;
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

            ManualResetEvent stopThread = (ManualResetEvent)stopThreadArg;

            Task stop = Task.Factory.StartNew(() =>
                {
                    stopThread.WaitOne();
                }, TaskCreationOptions.LongRunning);

            List<Task> pendingConnects = new List<Task>();

            while (true)
            {
                Task<NetworkConnection> conn = Task.Factory.StartNew<NetworkConnection>(() =>
                    {
                        return _incomingConnections.Dequeue();
                    }, TaskCreationOptions.LongRunning);

                List<Task> allTasks = new List<Task>();
                allTasks.Add(conn);
                allTasks.Add(stop);
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
                        Task<NamedConnection> result = (Task<NamedConnection>)allTasks[index];
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
                    throw conn.Exception;
                case TaskStatus.RanToCompletion:
                    NetworkConnection connection = conn.Result;
                    return Task.Factory.StartNew<NamedConnection>(() =>
                        {
                            ConnectReceive connRecv = new ConnectReceive(new CommandWriter(), new CommandReader());
                            return connRecv.Run(connection);
                        });
                default:
                    throw new NotImplementedException("say what?");                    
            }
        }
    }
}
