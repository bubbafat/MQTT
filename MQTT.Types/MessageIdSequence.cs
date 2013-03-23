
namespace MQTT.Types
{
    public class MessageIdSequence
    {
        readonly object _lock = new object();
        ushort _last;

        public MessageId Next()
        {
            lock (_lock)
            {
                if (_last == ushort.MaxValue)
                {
                    _last = 0;
                }

                _last++;

                return new MessageId(_last);
            }
        }
    }
}
