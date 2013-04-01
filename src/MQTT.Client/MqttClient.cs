using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

    public sealed class MqttClient : IDisposable
    {
        private const ushort DefaultKeepAliveSeconds = 300;
        private const int DefaultMqttPort = 1883;
        private const double DefaultKeepAliveThreshold = 0.80;

        private readonly IMqttClient _client;
        private readonly MessageIdSequence _idSeq = new MessageIdSequence();
        private readonly StateMachineManager _manager;

        private volatile ClientState _clientState;

        private Timer _timer;

        private IPEndPoint _reconnectEndpoint;
        private ushort _reconnectKeepAlive;

        public MqttClient(string clientId)
            : this(clientId, new MqttNetworkClient(new NetworkInterface(new CommandReader(), new CommandWriter())))
        {
        }

        public MqttClient(string clientId, IMqttClient client)
        {
            if (clientId.Length < 1 || clientId.Length > 23)
            {
                throw new ArgumentException("Client identifier must be between 1 and 23 charascters.");
            }

            _clientState = ClientState.Disconnected;
            ClientId = clientId;
            _client = client;
            _client.OnMessageReceived += ClientOnMessageReceived;
            _client.OnNetworkDisconnected += ClientOnOnNetworkDisconnected;
            _manager = new StateMachineManager(_client);
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

        public Task Ping()
        {
            return _manager.StartNew(new PingReq(), null);
        }

        public Task Connect(string server, 
            int port = DefaultMqttPort, 
            ushort keepAliveSeconds = DefaultKeepAliveSeconds, 
            bool cleanSession = false,
            string userName = null,
            string password = null)
        {
            IPAddress address =
                Dns.GetHostAddresses(server)
                   .First(a => a.AddressFamily == AddressFamily.InterNetwork);

            return Connect(new IPEndPoint(address, port), keepAliveSeconds);
        }

        public Task Connect(IPEndPoint endpoint, 
            ushort keepAliveSeconds = DefaultKeepAliveSeconds, 
            bool cleanSession = false,
            string userName = null,
            string password = null)
        {
            _reconnectEndpoint = endpoint;
            _reconnectKeepAlive = keepAliveSeconds;

            _client.Connect(endpoint).Await();
            _client.Receive();
            KeepAliveSeconds = keepAliveSeconds;
            _clientState = ClientState.Connecting;

            var connectFlow = new ConnectSendFlow(_manager);

            var connectCommand = new Connect(ClientId, keepAliveSeconds);
            connectCommand.Details.ConnectFlags.CleanSession = cleanSession;
            if (string.IsNullOrEmpty(userName) == false)
            {
                connectCommand.Details.ConnectFlags.UserName = true;
                connectCommand.UserName = userName;
            }
            if (string.IsNullOrEmpty(password) == false)
            {
                connectCommand.Details.ConnectFlags.Password = true;
                connectCommand.Password = password;
            }

            return connectFlow.Start(connectCommand, startCmd =>
                                     {
                                         _clientState = ClientState.Connected;
                                         ResetTimer();
                                     });
        }

        public Task Reconnect()
        {
            return Connect(_reconnectEndpoint, _reconnectKeepAlive);
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
        public event NetworkDisconnectedCallback OnNetworkDisconnected;

        private void ClientOnOnNetworkDisconnected(object sender, NetworkDisconenctedEventArgs networkDisconenctedEventArgs)
        {
            var dis = OnNetworkDisconnected;
            if (dis != null)
            {
                dis(this, networkDisconenctedEventArgs);
            }
        }

        private void ClientOnMessageReceived(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;
            Debug.WriteLine("{0} : Recevied Message {1} id {2}", DateTime.Now.ToString("o"), command.CommandMessage,
                            command.MessageId);

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

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            try
            {
                Ping().Wait();
                _timer.Start();
            }
            catch(Exception)
            {
                LocalDisconnect();
                throw;
            }
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