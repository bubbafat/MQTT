using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class Unsubscribe : MqttCommand
    {
        List<string> _topics = new List<string>();

        public Unsubscribe(string[] topics)
            : this(new FixedHeader(CommandMessage.UNSUBSCRIBE), null)
        {
            if (topics != null)
            {
                _topics.AddRange(topics);
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
                }

                return bytes.ToArray();
            }
        }

        public Unsubscribe(FixedHeader header, byte[] data)
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
