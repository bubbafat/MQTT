using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class Subscribe : MqttCommand
    {
        List<string> _topics = new List<string>();

        public Subscribe(string[] topics, MessageId messageId)
            : this(new FixedHeader(CommandMessage.SUBSCRIBE), null)
        {
            this.Header.QualityOfService = QualityOfService.AtLeastOnce;

            if (topics != null)
            {
                _topics.AddRange(topics);
            }

            MessageId = messageId;
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
                foreach (string topic in _topics)
                {
                    bytes.AddRange(MQString.ToByteArray(topic));
                    bytes.Add((byte)QualityOfService.AtLeastOnce);
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
                            _topics.Add(MQString.FromStream(stream));
                        }
                    }
                }
            }
        }
    }
}
