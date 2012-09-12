using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Client.Commands;
using System.Timers;

namespace MQTT.Client
{
    public class Client
    {
        string _clientId;
        IMqttBroker _broker;
        Timer _timer;

        public Client(string clientId)
        {
            _clientId = clientId;
            _broker = Factory.GetInstance<IMqttBroker>();
        }

        public Task Connect(IPEndPoint endpoint)
        {
            ResetTimer();
            _broker.Connect(endpoint);
            return _broker.SendCommandAsync(new Commands.Connect(_clientId, 300));
        }

        public void Disconnect(TimeSpan lengthBeforeForce)
        {
            _broker.SendCommandAsync(new Commands.Disconnect()).Wait(lengthBeforeForce);
            _broker.Disconnect();
        }

        public Task Publish(string topic, string message)
        {
            Publish pub = new Commands.Publish(topic, message);
            pub.Header.QualityOfService = QualityOfService.AtLeastOnce;
            return _broker.SendCommandAsync(pub);
        }

        public Task PubAck(ushort messageId)
        {
            return _broker.SendCommandAsync(new Commands.PubAck(messageId));
        }

        public Task PubRel(ushort messageId)
        {
            return _broker.SendCommandAsync(new Commands.PubRel(messageId));
        }

        public Task PubComp(ushort messageId)
        {
            return _broker.SendCommandAsync(new Commands.PubComp(messageId));
        }

        public Task Ping()
        {
            return _broker.SendCommandAsync(new Commands.PingReq());
        }

        public Task Subscribe(string[] topics)
        {
            Subscribe s = new Commands.Subscribe(topics);
            s.MessageId.Value = 50;
            return _broker.SendCommandAsync(s);
        }

        public Task Unsubscribe(string[] topics)
        {
            return _broker.SendCommandAsync(new Commands.Unsubscribe(topics));
        }

        public Task<ClientCommand> Receive()
        {
            return _broker.ReceiveAsync();
        }

        public bool IsConnected
        {
            get
            {
                return _broker.IsConnected;
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
            if (IsConnected)
            {
                Ping();
            }
        }
    }
}
