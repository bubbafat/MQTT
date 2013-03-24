using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using MQTT.Commands;
using MQTT.Domain;
using MQTT.Domain.StateMachines;
using MQTT.Types;

namespace MQTT.Client
{
    public delegate void UnsolicitedMessageCallback(object sender, ClientCommandEventArgs e);

    public sealed class MqttClient : IDisposable
    {
        private readonly IMqttClient _client;
        private readonly MessageIdSequence _idSeq = new MessageIdSequence();
        private readonly object _lastHeaderLock = new object();
        private readonly StateMachineManager _manager;

        private bool _connAcked;
        private DateTime _lastHeard = DateTime.MinValue;
        private Timer _timer;

        public MqttClient(string clientId)
            : this(clientId, new MqttNetworkClient(new NetworkInterface(new CommandReader(), new CommandWriter())))
        {
            
        }

        public MqttClient(string clientId, IMqttClient client)
        {
            ClientId = clientId;
            _client = client;
            _client.OnMessageReceived += ClientOnMessageReceived;
            _manager = new StateMachineManager(_client);
        }

        public string ClientId { get; private set; }

        public bool IsConnected
        {
            get { return _client.IsConnected && _connAcked; }
        }

        public void Dispose()
        {
            using (_timer) { }
            using (_client) { }
        }

        public Task Connect(string server, int port)
        {
            IPAddress address =
                Dns.GetHostAddresses(server)
                   .First(a => a.AddressFamily == AddressFamily.InterNetwork);

            return Connect(new IPEndPoint(address, port));
        }

        public Task Connect(IPEndPoint endpoint)
        {
            _connAcked = false;
            _client.Connect(endpoint);

            var connect = new ConnectSendFlow(_manager);
            return connect.Start(new Connect(ClientId, 300),
                                 startCmd =>
                                     {
                                         ResetTimer();
                                         _connAcked = true;
                                     });
        }

        public void Disconnect(TimeSpan lengthBeforeForce)
        {
            _client.Send(new Disconnect()).Await();
            _client.Disconnect();
        }

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
                new[] { new Subscription(topic, qos)}, 
                completed);
        }

        public Task Subscribe(Subscription[] subs, Action<MqttCommand> completed)
        {
            var s = new Subscribe(subs, _idSeq.Next());
            var flow = new SubscribeSendFlow(_manager);
            return flow.Start(s, completed);
        }

        public void Unsubscribe(string[] topics)
        {
            _client.Send(new Unsubscribe(topics)).Await();
        }

        public event UnsolicitedMessageCallback OnUnsolicitedMessage;

        private void ClientOnMessageReceived(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            lock (_lastHeaderLock)
            {
                _lastHeard = DateTime.UtcNow;
            }

            switch (command.CommandMessage)
            {
                case CommandMessage.PUBACK:
                case CommandMessage.PUBCOMP:
                case CommandMessage.PUBREC:
                case CommandMessage.PUBREL:
                case CommandMessage.SUBACK:
                case CommandMessage.CONNACK:
                case CommandMessage.UNSUBACK:
                    _manager.Deliver(command);
                    break;
                case CommandMessage.PINGRESP:
                    // ignore (we sent it) - eventually track
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
            using (_timer) { }
            _timer = new Timer(300*1000*0.80);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lastHeaderLock)
            {
                if (IsConnected && _lastHeard < DateTime.UtcNow.AddMinutes(4))
                {
                    _client.Send(new PingReq()).Await();
                }
            }
        }
    }
}