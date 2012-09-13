using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class Subscription
    {
        public string Topic { get; private set; }
        public QualityOfService QoS { get; private set; }

        public Subscription(string topic, QualityOfService qos)
        {
            Topic = topic;
            QoS = qos;
        }

        public byte[] ToByteArray()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(MQString.ToByteArray(Topic));
            bytes.Add((byte)QoS);

            return bytes.ToArray();
        }
    }

    public class Subscribe : MqttCommand
    {
        List<Subscription> _topics = new List<Subscription>();

        public Subscribe(Subscription[] subscriptions, MessageId messageId)
            : this(new FixedHeader(CommandMessage.SUBSCRIBE), null)
        {
            this.Header.QualityOfService = QualityOfService.AtLeastOnce;

            if (subscriptions != null)
            {
                Subscriptions.AddRange(subscriptions);
            }

            MessageId = messageId;
        }

        public List<Subscription> Subscriptions
        {
            get
            {
                return _topics;
            }
        }

        protected override byte[] VariableHeader
        {
            get
            {
                return MessageId.ToByteArray();
            }
        }

        protected override byte[] Payload
        {
            get
            {
                List<byte> bytes = new List<byte>();
                foreach (Subscription sub in Subscriptions)
                {
                    bytes.AddRange(sub.ToByteArray());
                }

                return bytes.ToArray();
            }
        }

        public Subscribe(FixedHeader header, byte[] data)
            : base(header)
        {
            if (header.RemainingLength > 0)
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    if (Header.QualityOfService != QualityOfService.AtMostOnce)
                    {
                        MessageId = MessageId.FromStream(stream);

                        while (stream.Position < stream.Length)
                        {
                            Subscriptions.Add(new Subscription(MQString.FromStream(stream), (QualityOfService)stream.ReadBytesOrFail(1)[0]));
                        }
                    }
                }
            }
        }
    }
}
