using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MQTT.Commands;
using MQTT.Domain;
using MQTT.Domain.StateMachines;
using MQTT.Types;
using System.Windows.Threading;

namespace MQTT.Client
{
    public delegate void UnsolicitedMessageCallback(object sender, ClientCommandEventArgs e);

    public sealed class Client : IDisposable
    {
        private readonly IMqttBroker _broker;
        private readonly MessageIdSequence _idSeq = new MessageIdSequence();
        private readonly object _lastHeaderLock = new object();
        private readonly StateMachineManager _manager;

        private DispatcherTimer _timer;
        private bool _connAcked;
        private DateTime _lastHeard = DateTime.MinValue;



        public Client(IMqttBroker broker)
        {
            _broker = broker;
            _broker.OnMessageReceived += _broker_OnMessageReceived;
            _manager = new StateMachineManager(_broker);
        }

        public string ClientId { get; set; }

        public bool IsConnected
        {
            get { return _broker.IsConnected && _connAcked; }
        }

        public void Dispose()
        {
            using (_broker)
            {
            }
        }

        public Task Connect(IPEndPoint endpoint)
        {
            _connAcked = false;
            _broker.Connect(endpoint);

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
            _broker.Send(new Disconnect()).Await();
            _broker.Disconnect();
        }

        public Task Publish(string topic, string message, QualityOfService qos, Action<MqttCommand> completed)
        {
            var pub = new Publish(topic, message);
            pub.Header.QualityOfService = qos;
            if (qos != QualityOfService.AtMostOnce)
            {
                pub.MessageId = _idSeq.Next();
            }

            var publish = new PublishSendFlow(_manager);
            return publish.Start(pub, completed);
        }

        public Task Subscribe(Subscription[] subs, Action<MqttCommand> completed)
        {
            var s = new Subscribe(subs, _idSeq.Next());
            var flow = new SubscribeSendFlow(_manager);
            return flow.Start(s, completed);
        }

        public void Unsubscribe(string[] topics)
        {
            _broker.Send(new Unsubscribe(topics)).Await();
        }

        public event UnsolicitedMessageCallback OnUnsolicitedMessage;

        private void _broker_OnMessageReceived(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            Debug.WriteLine("RECV: {0} ({1})", command.CommandMessage, command.MessageId);

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
            UnsolicitedMessageCallback callback = OnUnsolicitedMessage;
            if (callback != null)
            {
                callback(this, new ClientCommandEventArgs(command));
            }
        }

        private void ResetTimer()
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => {
                if (_timer == null)
                {
                    _timer = new DispatcherTimer();
                }
                else
                {
                    _timer.Stop();
                }

                _timer.Interval = TimeSpan.FromSeconds(300 * .8);
                _timer.Tick += new EventHandler(_timer_Tick);
                _timer.Start();
            });
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            lock (_lastHeaderLock)
            {
                if (IsConnected && _lastHeard < DateTime.UtcNow.AddMinutes(4))
                {
                    _broker.Send(new PingReq()).Await();
                }
            }
        }
    }
}