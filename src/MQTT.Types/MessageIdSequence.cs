
namespace MQTT.Types
{
    public class MessageIdSequence
    {
        readonly object _lock = new object();
        ushort _last;

        public MessageId Next()
        {
            ushort temp;

            lock (_lock)
            {
                temp = _last++;
            }

            return new MessageId(temp);
        }
    }
}
