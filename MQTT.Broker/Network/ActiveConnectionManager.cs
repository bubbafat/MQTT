using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Commands;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Domain;

namespace MQTT.Broker.Network
{
    class ActiveConnectionManager : IActiveConnectionManager
    {
        ConcurrentDictionary<string, NamedConnection> _allConnections = new ConcurrentDictionary<string, NamedConnection>();
        List<NamedConnection> _newConnections = new List<NamedConnection>();
        List<Task<CommandRead>> _runningCommands = new List<Task<CommandRead>>();

        Thread _processingThread;
        ManualResetEvent _stopThread;
        readonly object _lock = new object();
        ManualResetEvent _itemAdded = new ManualResetEvent(false);

        public ActiveConnectionManager()
        {

        }

        public void Start()
        {
            lock (_lock)
            {
                Stop();

                _stopThread = new ManualResetEvent(false);
                _processingThread = new Thread(ExistingConnectionListenerLoop);
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
            }
        }

        private void ExistingConnectionListenerLoop(object stopThreadArg)
        {
            if (stopThreadArg == null)
            {
                throw new ArgumentNullException("stopThreadArg");
            }

            ManualResetEvent stopThread = (ManualResetEvent)stopThreadArg;

            while (true)
            {
                Task stop = Task.Factory.StartNew(() =>
                {
                    stopThread.WaitOne();
                }, TaskCreationOptions.LongRunning);

                Task added = Task.Factory.StartNew(() =>
                {
                    _itemAdded.WaitOne();
                }, TaskCreationOptions.LongRunning);

                List<Task> toListen = new List<Task>();
                toListen.Add(stop);
                toListen.Add(added);

                lock (_lock)
                {

                    var faulted = _runningCommands.Where(t => t.Status == TaskStatus.Faulted).ToArray();
                    if (faulted.Length > 0)
                    {
                        try
                        {
                            Task.WaitAll(faulted);
                        }
                        catch (AggregateException)
                        {
                            // eat it - we are skipping these since they are dead to us.
                        }

                        foreach (var task in faulted)
                        {
                            _runningCommands.Remove(task);
                        }
                    }

                    toListen.AddRange(_runningCommands);
                }

                int index = Task.WaitAny(toListen.ToArray());
                switch (index)
                {
                    case 0:
                        return;
                    case 1:
                        LoadNewItems();
                        break;
                    default:
                        Task<CommandRead> cmdRead = (Task<CommandRead>)_runningCommands[index-2];
                        ProcessItem(cmdRead);
                        _runningCommands.Remove(cmdRead);
                        break;
                }
            }
        }

        private void ProcessItem(Task<CommandRead> namedConnectionTask)
        {
            switch (namedConnectionTask.Status)
            {
                case TaskStatus.Faulted:
                    System.Diagnostics.Trace.WriteLine(string.Format("ERROR: {0}", namedConnectionTask.Exception));
                    return;
                case TaskStatus.RanToCompletion:
                    namedConnectionTask.Result.Connection.Deliver(namedConnectionTask.Result.Command);
                    QueueReadCommand(namedConnectionTask.Result.Connection);
                    break;
            }
        }

        private void LoadNewItems()
        {
            lock (_lock)
            {
                foreach (NamedConnection nc in _newConnections)
                {
                    QueueReadCommand(nc);
                }

                _newConnections.Clear();

                _itemAdded.Reset();
            }
        }

        private void QueueReadCommand(NamedConnection connection)
        {
            lock (_lock)
            {
                _runningCommands.Add(Task.Factory.StartNew<CommandRead>(() =>
                        {
                            ICommandReader reader = BrokerFactory.Get<ICommandReader>();
                            MqttCommand cmd = reader.Read(connection.Connection);
                            return new CommandRead(cmd, connection);
                        }, TaskCreationOptions.LongRunning));
            }
        }

        void IActiveConnectionManager.Register(NamedConnection connection)
        {
            lock (_lock)
            {
                connection.Manager = this;

                // maybe an existing known connection
                Disconnect(connection);

                // maybe still in new connections queue
                NamedConnection existing = _newConnections.Where(c => c.ClientId == connection.ClientId).FirstOrDefault();
                if (existing != null)
                {
                    Disconnect(existing);
                }

                _newConnections.Add(connection);
                _allConnections.AddOrUpdate(connection.ClientId, connection, (id, old) => connection);

                _itemAdded.Set();
            }
        }

        class CommandRead
        {
            public CommandRead(MqttCommand command, NamedConnection connection)
            {
                Command = command;
                Connection = connection;
            }

            public MqttCommand Command { get; private set; }
            public NamedConnection Connection { get; private set; }
        }


        public void Send(string client, Publish publish)
        {
            NamedConnection conn;
            if (_allConnections.TryGetValue(client, out conn))
            {
                conn.Send(publish);
            }
        }

        public void Disconnect(NamedConnection namedConnection)
        {
            NamedConnection old;
            if (_allConnections.TryRemove(namedConnection.ClientId, out old))
            {
                old.Connection.Disconnect();
            }
        }
    }
}
