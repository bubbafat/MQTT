using System.Collections.Generic;
using MQTT.Types;
using System.IO;

namespace MQTT.Commands
{
    public class Unsubscribe : MqttCommand
    {
        readonly List<string> _topics = new List<string>();

        public Unsubscribe(IEnumerable<string> topics)
            : this(new FixedHeader(CommandMessage.UNSUBSCRIBE), null)
        {
            if (topics != null)
            {
                _topics.AddRange(topics);
            }
        }

        public List<string> Topics
        {
            get
            {
                return _topics;
            }
        }


        protected override byte[] Payload
        {
            get
            {
                var bytes = new List<byte>();
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
                using (var stream = new MemoryStream(data))
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
