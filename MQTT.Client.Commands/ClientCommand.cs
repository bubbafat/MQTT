using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;
using System.IO;

namespace MQTT.Client.Commands
{
    public abstract class ClientCommand
    {
        protected ClientCommand(FixedHeader header)
        {
            MessageId = MessageId.Any;
            Header = header;
        }

        public byte[] ToByteArray()
        {
            List<byte> bytes = new List<byte>();

            byte[] payload = Payload;
            byte[] vh = VariableHeader;

            if (payload != null)
            {
                Header.RemainingLength = payload.Length;
            }

            if (vh != null)
            {
                Header.RemainingLength += vh.Length;
            }

            bytes.AddRange(Header.ToByteArray());

            if (vh != null)
            {
                bytes.AddRange(vh);
            }

            if (payload != null)
            {
                bytes.AddRange(payload);
            }

            return bytes.ToArray();
        }

        protected virtual byte[] VariableHeader
        {
            get 
            { 
                return null; 
            }
        }

        protected virtual byte[] Payload
        {
            get
            {
                return null;
            }
        }

        public FixedHeader Header
        {
            get;
            private set;
        }

        public CommandMessage CommandMessage
        {
            get
            {
                return Header.Message;
            }
        }

        public MessageId MessageId
        {
            get;
            set;
        }

        public static ClientCommand Create(FixedHeader header, byte[] data)
        {
            switch (header.Message)
            {
                case CommandMessage.CONNACK:
                    return new ConnAck(header, data);
                case CommandMessage.CONNECT:
                    return new Connect(header, data);
                case CommandMessage.DISCONNECT:
                    return new Disconnect(header);
                case CommandMessage.PINGREQ:
                    return new PingReq(header);
                case CommandMessage.PINGRESP:
                    return new PingResp(header);
                case CommandMessage.PUBACK:
                    return new PubAck(header, data);
                case CommandMessage.PUBCOMP:
                    return new PubComp(header, data);
                case CommandMessage.PUBLISH:
                    return new Publish(header, data);
                case CommandMessage.PUBREC:
                    return new PubRec(header, data);
                case CommandMessage.PUBREL:
                    return new PubRel(header, data);
                case CommandMessage.SUBACK:
                    return new SubAck(header, data);
                case CommandMessage.SUBSCRIBE:
                    return new Subscribe(header, data);
                case CommandMessage.UNSUBACK:
                    return new UnSubAck(header, data);
                case CommandMessage.UNSUBSCRIBE:
                    return new Unsubscribe(header, data);
                default:
                    throw new InvalidOperationException("Unknown command message");
            }
        }
    }
}
