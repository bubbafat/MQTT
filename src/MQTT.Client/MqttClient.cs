﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MQTT.Commands;
using MQTT.Domain;
using MQTT.Domain.StateMachines;
using MQTT.Types;
using Timer = System.Timers.Timer;

namespace MQTT.Client
{
    public delegate void UnsolicitedMessageCallback(object sender, ClientCommandEventArgs e);

    public delegate void KeepAliveExpiredCallback(object sender, KeepAliveExpiredEventArgs e);

    public sealed class MqttClient : IDisposable
    {
        private const ushort DefaultKeepAliveSeconds = 300;
        private const int DefaultMqttPort = 1883;
        private const double DefaultKeepAliveThreshold = 0.80;

        private readonly IMqttClient _client;
        private readonly MessageIdSequence _idSeq = new MessageIdSequence();
        private readonly StateMachineManager _manager;

        private volatile ClientState _clientState;
        private long _keepAlivePingTime = long.MinValue;

        private Timer _timer;

        public MqttClient(string clientId)
            : this(clientId, new MqttNetworkClient(new NetworkInterface(new CommandReader(), new CommandWriter())))
        {

        }

        public MqttClient(string clientId, IMqttClient client)
        {
            _clientState = ClientState.Disconnected;
            ClientId = clientId;
            _client = client;
            _client.OnMessageReceived += ClientOnMessageReceived;
            _manager = new StateMachineManager(_client);
            KeepAlivePingResponseMinimumWait = 15;
        }

        public ClientState State
        {
            get { return _clientState; }
        }

        public string ClientId { get; private set; }

        public bool IsConnected
        {
            get { return _client.IsConnected && (State & ClientState.Connected) == ClientState.Connected; }
        }

        public void Dispose()
        {
            _clientState = ClientState.Disconnected;
            using (_timer)
            {
            }
            using (_client)
            {
            }
        }

        public Task Connect(string server, int port = DefaultMqttPort, ushort keepAliveSeconds = DefaultKeepAliveSeconds)
        {
            IPAddress address =
                Dns.GetHostAddresses(server)
                   .First(a => a.AddressFamily == AddressFamily.InterNetwork);

            return Connect(new IPEndPoint(address, port), keepAliveSeconds);
        }

        public Task Connect(IPEndPoint endpoint, ushort keepAliveSeconds = DefaultKeepAliveSeconds)
        {
            _client.Connect(endpoint);
            KeepAliveSeconds = keepAliveSeconds;
            _clientState = ClientState.Connecting;

            var connect = new ConnectSendFlow(_manager);
            return connect.Start(new Connect(ClientId, keepAliveSeconds),
                                 startCmd =>
                                     {
                                         ResetTimer();
                                         _clientState = ClientState.Connected;
                                     });
        }

        private void LocalDisconnect()
        {
            _clientState = ClientState.Disconnecting;
            _timer.Stop();
            _client.Disconnect();
            _clientState = ClientState.Disconnected;
        }

        public void Disconnect(TimeSpan lengthBeforeForce)
        {
            _clientState = ClientState.Disconnecting;
            _timer.Stop();
            _client.Send(new Disconnect()).Await();
            _client.Disconnect();
            _clientState = ClientState.Disconnected;
        }

        public ushort KeepAliveSeconds { get; private set; }
        public ushort KeepAlivePingResponseMinimumWait { get; set; }

        public Task Publish(string topic, string message, QualityOfService qos, Action<MqttCommand> completed)
        {
            var pub = new Publish(topic, message)
                {
                    Header = {QualityOfService = qos}
                };

            if (qos != QualityOfService.AtMostOnce)
            {
                pub.MessageId = _idSeq.Next();
            }

            var publish = new PublishSendFlow(_manager);

            return publish.Start(pub, completed);
        }

        public Task Subscribe(string topic, QualityOfService qos)
        {
            return Subscribe(topic, qos, null);
        }

        public Task Subscribe(string topic, QualityOfService qos, Action<MqttCommand> completed)
        {
            return Subscribe(
                new[] {new Subscription(topic, qos)},
                completed);
        }

        public Task Subscribe(Subscription[] subs, Action<MqttCommand> completed)
        {
            var s = new Subscribe(subs, _idSeq.Next());
            var flow = new SubscribeSendFlow(_manager);
            return flow.Start(s, completed);
        }

        public Task Unsubscribe(string[] topics)
        {
            return _client.Send(new Unsubscribe(topics));
        }

        public event UnsolicitedMessageCallback OnUnsolicitedMessage;
        public event KeepAliveExpiredCallback OnKeepAliveExpired;

        private void ClientOnMessageReceived(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            long nextTimeout = DateTime.UtcNow.Ticks +
                               (long) ((KeepAliveSeconds*DefaultKeepAliveThreshold)*TimeSpan.TicksPerSecond);
            Interlocked.Exchange(ref _keepAlivePingTime, nextTimeout);

            switch (command.CommandMessage)
            {
                case CommandMessage.PUBACK:
                case CommandMessage.PUBCOMP:
                case CommandMessage.PUBREC:
                case CommandMessage.PUBREL:
                case CommandMessage.SUBACK:
                case CommandMessage.CONNACK:
                case CommandMessage.UNSUBACK:
                case CommandMessage.PINGRESP:
                    _manager.Deliver(command);
                    break;
                default:
                    _manager.StartNew(command, Notify);
                    break;
            }
        }

        private void Notify(MqttCommand command)
        {
            var callback = OnUnsolicitedMessage;
            if (callback != null)
            {
                callback(this, new ClientCommandEventArgs(command));
            }
        }

        private void ResetTimer()
        {
            if (_timer == null)
            {
                _timer = new Timer(KeepAliveSeconds*1000*DefaultKeepAliveThreshold);
            }
            else
            {
                _timer.Stop();
            }

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void NotifyOfTimeout(long keepAlivePingTime)
        {
            var callback = OnKeepAliveExpired;
            if (callback != null)
            {
                callback(this,
                         new KeepAliveExpiredEventArgs(
                             new DateTime(keepAlivePingTime, DateTimeKind.Utc), KeepAliveSeconds));
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();

            if (IsConnected && (_clientState & ClientState.WaitingOnPing) == 0)
            {
                _clientState |= ClientState.WaitingOnPing;
                long keepAlivePingTime = Interlocked.Read(ref _keepAlivePingTime);

                if (keepAlivePingTime < DateTime.UtcNow.Ticks)
                {
                    Task ping = _manager.StartNew(new PingReq(), 
                        command => _clientState &= ~ClientState.WaitingOnPing);

                    // the threshold between when we ping and when we disconnect
                    long ticksInKAThreshold = (KeepAliveSeconds*TimeSpan.TicksPerSecond) -
                                              (long)
                                              ((KeepAliveSeconds*TimeSpan.TicksPerSecond*DefaultKeepAliveThreshold));

                    // but let's at least be reasonable in how long we wait...
                    int pingWindowMS = (int) Math.Max((float)ticksInKAThreshold/TimeSpan.TicksPerMillisecond,
                                                      TimeSpan.FromSeconds(KeepAlivePingResponseMinimumWait).TotalMilliseconds);

                    var tasks = new[] {ping, 
                        Task.Factory.StartNew(() => Thread.Sleep(pingWindowMS))};

                    var first = Task.WaitAny(tasks);
                    Task finished = tasks[first];
                    if (finished == ping)
                    {
                        if (finished.IsCompleted)
                        {
                            _timer.Start();
                            return;
                        }
                    }

                    LocalDisconnect();
                    NotifyOfTimeout(keepAlivePingTime);
                    return;
                }
            }

            _timer.Start();
        }
    }

    [Flags]
    public enum ClientState
    {
        Disconnected = 0,
        Connecting = 1,
        ConnectFailed = 2,
        Connected = 4,
        WaitingOnPing = 8,
        Disconnecting = 16,
        PingTimeout = 32,
    }
}