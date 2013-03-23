namespace MQTT.Types
{
    public enum CommandMessage
    {
        // ReSharper disable InconsistentNaming
        UNKNOWN = 0,    // Nothing has been set
        CONNECT = 1,    // Client request to connect to Server
        CONNACK = 2,    // Connect Acknowledgment
        PUBLISH = 3,    // Publish message
        PUBACK = 4,    // Publish Acknowledgment
        PUBREC = 5,    // Publish Received (assured delivery part 1)
        PUBREL = 6,    // Publish Release (assured delivery part 2)
        PUBCOMP = 7,    // Publish Complete (assured delivery part 3)
        SUBSCRIBE = 8,    // Client Subscribe request
        SUBACK = 9,    // Subscribe Acknowledgment
        UNSUBSCRIBE = 10,   // Client Unsubscribe request
        UNSUBACK = 11,   // Unsubscribe Acknowledgment
        PINGREQ = 12,   // PING Request
        PINGRESP = 13,   // PING Response
        DISCONNECT = 14,   // Client is Disconnecting
        // ReSharper restore InconsistentNaming
    }
}
