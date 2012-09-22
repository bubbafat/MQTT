using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using MQTT.Domain.StateMachines;
using MQTT.Domain;
using MQTT.Commands;
using MQTT.Types;

namespace MQTT.Client
{
    public delegate void UnsolicitedMessageCallback(object sender, ClientCommandEventArgs e);

    public sealed class Client : IDisposable
    {
        IMqttBroker _broker;
        Timer _timer;
        StateMachineManager _manager;
        bool _connAcked = false;
        MessageIdSequence _idSeq = new MessageIdSequence();
        private object _lastHeaderLock = new object();
        private DateTime _lastHeard = DateTime.MinValue;

        public Client(IMqttBroker broker)
        {
            _broker = broker;
            _broker.OnMessageReceived += new MessageReceivedCallback(_broker_OnMessageReceived);
            _manager = new StateMachineManager(_broker);
        }

        public string ClientId
        {
            get;
            set;
        }

        public Task Connect(IPEndPoint endpoint)
        {
            _connAcked = false;
            _broker.Connect(endpoint);

            ConnectSendFlow connect = new ConnectSendFlow(_manager);
            return connect.Start(new Commands.Connect(ClientId, 300),
                (startCmd) =>
                {
                    ResetTimer();
                    _connAcked = true;
                });
        }

        public void Disconnect(TimeSpan lengthBeforeForce)
        {
            _broker.Send(new Commands.Disconnect()).Await();
            _broker.Disconnect();
        }

        public Task Publish(string topic, string message, QualityOfService qos, Action<MqttCommand> completed)
        {
            Publish pub = new Commands.Publish(topic, message);
            pub.Header.QualityOfService = qos;
            if (qos != QualityOfService.AtMostOnce)
            {
                pub.MessageId = _idSeq.Next();
            }

            PublishSendFlow publish = new PublishSendFlow(_manager);
            return publish.Start(pub, completed);
        }

        public Task Subscribe(Subscription[] subs, Action<MqttCommand> completed)
        {
            Subscribe s = new Commands.Subscribe(subs, _idSeq.Next());
            SubscribeSendFlow flow = new SubscribeSendFlow(_manager);
            return flow.Start(s, completed);
        }

        public void Unsubscribe(string[] topics)
        {
            _broker.Send(new Commands.Unsubscribe(topics)).Await();
        }

        public bool IsConnected
        {
            get
            {
                return _broker.IsConnected && _connAcked;
            }
        }

        public event UnsolicitedMessageCallback OnUnsolicitedMessage;

        void _broker_OnMessageReceived(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            System.Diagnostics.Debug.WriteLine("RECV: {0} ({1})", command.CommandMessage, command.MessageId);

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
                    _manager.StartNew(command, (MqttCommand cmd) =>
                        {
                            notify(cmd);
                        });
                    break;
            }
        }

        private void notify(MqttCommand command)
        {
            UnsolicitedMessageCallback callback = OnUnsolicitedMessage;
            if (callback != null)
            {
                callback(this, new ClientCommandEventArgs(command));
            }
        }

        private void ResetTimer()
        {
            using (_timer) { }
            _timer = new Timer(300 * 1000 * 0.80);
            _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            _timer.Start();
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lastHeaderLock)
            {
                if (IsConnected && _lastHeard < DateTime.UtcNow.AddMinutes(4))
                {
                    _broker.Send(new Commands.PingReq()).Await();
                }
            }
        }

        public void Dispose()
        {
            using (_timer) { }
            using (_broker) { }
        }
    }
}
