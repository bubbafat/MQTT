using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public class Publish : ClientCommand
    {
        public Publish(string topic, string message)
            : this(new FixedHeader(CommandMessage.PUBLISH), null)
        {
            if (string.IsNullOrEmpty(topic))
            {
                throw new ArgumentNullException("topic");
            }

            Topic = topic;

            if (!string.IsNullOrEmpty(message))
            {
                Message = MQString.ToByteArray(message);
            }
            else
            {
                Message = new byte[0];
            }
        }

        protected override byte[] VariableHeader
        {
            get
            {
                List<byte> bytes = new List<byte>(MQString.ToByteArray(Topic));
                if (Header.QualityOfService == QualityOfService.AtLeastOnce ||
                    Header.QualityOfService == QualityOfService.ExactlyOnce)
                {
                    bytes.AddRange(MessageId.ToByteArray());
                }

                return bytes.ToArray();
            }
        }

        protected override byte[] Payload
        {
            get
            {
                return Message;
            }
        }

        public byte[] Message { get; private set; }
        public string Topic { get; private set; }

        public Publish(FixedHeader header, byte[] data)
            : base(header)
        {
            if (header.RemainingLength > 0)
            {
                using(MemoryStream stream = new MemoryStream(data))
                {
                    Topic = MQString.FromStream(stream);

                    if (Header.QualityOfService == QualityOfService.AtLeastOnce ||
                        Header.QualityOfService == QualityOfService.ExactlyOnce)
                    {
                        MessageId = MessageId.FromStream(stream);
                    }

                    if (stream.Position < stream.Length)
                    {
                        Message = stream.ReadRest();
                    }
                    else
                    {
                        Message = new byte[0];
                    }
                }
            }
        }
    }
}
